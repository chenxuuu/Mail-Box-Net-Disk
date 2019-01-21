using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace maildisk.apis
{
    class VisualDisk
    {
        private string address;
        private string account;
        private string password;
        private string imapServer;
        private int imapPort;
        private bool imapSsl;
        private string smtpServer;
        private int smtpPort;
        private bool smtpSsl;
        
        public VisualDisk(string imapServer, int port, bool useSsl, string account, string password, string address, 
            string smtpServer,int smtpPort, bool smtpSsl)
        {
            this.imapServer = imapServer;
            this.imapPort = port;
            this.imapSsl = useSsl;
            this.address = address;
            this.account = account;
            this.password = password;
            this.smtpServer = smtpServer;
            this.smtpPort = smtpPort;
            this.smtpSsl = smtpSsl;
#if DEBUG
            Console.WriteLine($"[disk create]mail client created, on {imapServer}:{port}" +
                $", ssl:{useSsl}, {account}, {address},{smtpServer},{smtpPort},{smtpSsl}");
#endif
        }

        /// <summary>
        /// get a imap client
        /// </summary>
        /// <returns>imap client</returns>
        private ImapClient GetImapClient()
        {
            ImapClient client = new ImapClient();
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            client.Connect(imapServer, imapPort, imapSsl);
            client.Authenticate(account, password);
            return client;
        }

        /// <summary>
        /// upload a file
        /// </summary>
        /// <param name="fileName">file name on cloud disk</param>
        /// <param name="folderPath">folder on email</param>
        /// <param name="filePath">local file path</param>
        /// <returns>file upload success or not</returns>
        public bool Upload(string fileName, string folderPath, string filePath)
        {
#if DEBUG
            Console.WriteLine($"[disk upload]{fileName},{folderPath},{filePath}");
#endif
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("mail disk", address));
            message.To.Add(new MailboxAddress("mail disk", address));
            message.Subject = "[mailDisk]" + fileName;
            var body = new TextPart("plain")
            {
                Text = 
                "This mail is send by mail disk\r\n" +
                "please do not delete"
            };
            
            var attachment = new MimePart()
            {
                Content = new MimeContent(File.OpenRead(filePath), ContentEncoding.Default),
                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                ContentTransferEncoding = ContentEncoding.Base64,
                FileName = "attachment.netdiskfile"
            };
            
            var multipart = new Multipart("mixed");
            multipart.Add(body);
            multipart.Add(attachment);
            message.Body = multipart;

            using (var smtpClient = new SmtpClient())
            {
                smtpClient.ServerCertificateValidationCallback = (s, c, h, e) => true;
                smtpClient.Connect(smtpServer, smtpPort, smtpSsl);
                smtpClient.Authenticate(account, password);
                smtpClient.Send(message);
                smtpClient.Disconnect(true);
            }
#if DEBUG
            Console.WriteLine($"[disk upload]upload success");
#endif
            bool result = false;
            int retryCount = 0;

            while(!result && retryCount <= 30)
            {
#if DEBUG
                Console.WriteLine($"[disk upload]check mail {retryCount} times");
#endif
                System.Threading.Tasks.Task.Delay(5000).Wait();
                var client = GetImapClient();
                var inbox = client.Inbox;
                inbox.Open(FolderAccess.ReadWrite);
                var uids = inbox.Search(SearchQuery.NotSeen);
                foreach (var m in inbox.Fetch(uids, MessageSummaryItems.Full | MessageSummaryItems.UniqueId))
                {
#if DEBUG
                    Console.WriteLine($"[disk upload]mail not seen {m.Envelope.Subject}");
#endif
                    if (m.Envelope.Subject == "[mailDisk]" + fileName)
                    {
                        result = true;
                        inbox.AddFlags(m.UniqueId, MessageFlags.Seen, true);
                        inbox.MoveTo(m.UniqueId, client.GetFolder(folderPath));
                        break;
                    }
                }
                client.Disconnect(true);
                retryCount++;
            }
            
            return result;
        }


        /// <summary>
        /// download file from mail
        /// </summary>
        /// <param name="folderPath">folder on email</param>
        /// <param name="fileName">file name on cloud disk</param>
        /// <param name="savePath">local file path</param>
        /// <returns>file download success or not</returns>
        public bool Download(string folderPath, string fileName,string savePath)
        {
#if DEBUG
            Console.WriteLine($"[disk download]{folderPath},{fileName},{savePath}");
#endif
            var client = GetImapClient();
            var folder = client.GetFolder(folderPath);
            folder.Open(FolderAccess.ReadOnly);
            var mails = folder.Fetch(0,-1, MessageSummaryItems.UniqueId | MessageSummaryItems.Full);
            foreach(var m in mails)
            {
#if DEBUG
                Console.WriteLine($"[disk download]find mail {m.Envelope.Subject}");
#endif
                if (m.Envelope.Subject == "[mailDisk]" + fileName)
                {
#if DEBUG
                    Console.WriteLine($"[disk download]downloading file {m.Envelope.Subject}");
#endif
                    MimeMessage message = folder.GetMessage(m.UniqueId);
                    foreach (MimePart attachment in message.Attachments)
                    {
                        //下载附件
                        using (var cancel = new System.Threading.CancellationTokenSource())
                        {
                            using (var stream = File.Create(savePath))
                            {
                                attachment.Content.DecodeTo(stream, cancel.Token);
                            }
                        }
                    }
#if DEBUG
                    Console.WriteLine($"[disk download]download file done!");
#endif
                    break;
                }
            }


            return true;
        }

    }
}
