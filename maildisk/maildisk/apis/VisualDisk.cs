using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
            //判断是否 添加ID COMMOND命令
            if ((client.Capabilities | ImapCapabilities.Id) == client.Capabilities)
            {
                var clientImplementation = new ImapImplementation
                {
                    Name = "Foxmail",
                    Version = "9.156"
                };
                var serverImplementation = client.Identify(clientImplementation);
            }
            return client;
        }

        /// <summary>
        /// upload a file
        /// </summary>
        /// <param name="fileName">file name on cloud disk</param>
        /// <param name="folderPath">folder on email</param>
        /// <param name="filePath">local file path</param>
        /// <returns>file upload success or not</returns>
        public bool Upload(string fileName, string folderPath, Stream file)
        {
#if DEBUG
            Console.WriteLine($"[disk upload]{fileName},{folderPath},{file.Length}");
#endif
            var client = GetImapClient();
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(address));
            message.To.Add(new MailboxAddress(address));
            message.Subject = "[mailDisk]" + fileName;
            var body = new TextPart("plain")
            {
                Text = 
                "This mail is send by mail disk\r\n" +
                "please do not delete"
            };
            
            var attachment = new MimePart()
            {
                Content = new MimeContent(file, ContentEncoding.Default),
                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                ContentTransferEncoding = ContentEncoding.Base64,
                FileName = "attachment.netdiskfile"
            };
            
            var multipart = new Multipart("mixed");
            multipart.Add(body);
            multipart.Add(attachment);
            message.Body = multipart;
#if DEBUG
            Console.WriteLine("[disk upload]appending...");
#endif
            var folder = GetImapClient().GetFolder(folderPath);
            folder.Open(FolderAccess.ReadWrite);
            var uid = folder.Append(message);
            Task.Run(() => {
                Task.Delay(5000).Wait();
                if (uid != null)
                    folder.SetFlags((UniqueId)uid, MessageFlags.Seen, true);
                else
                    Console.WriteLine($"[disk upload]{fileName} not marked as seened");
            });

#if DEBUG
            Console.WriteLine($"[disk upload]upload success");
#endif
            return true;
        }

        /// <summary>
        /// download file from mail
        /// </summary>
        /// <param name="folderPath">folder on email</param>
        /// <param name="fileName">file name on cloud disk</param>
        /// <param name="savePath">local file path</param>
        /// <returns>file download success or not</returns>
        public bool Download(string fileName, string folderPath, string savePath)
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


        /// <summary>
        /// upload big files, auto split by setting's size
        /// </summary>
        /// <param name="fileName">file name on cloud disk</param>
        /// <param name="folderPath">folder on email</param>
        /// <param name="filePath">local file path</param>
        /// <param name="blockSize">max size for each mail</param>
        /// <returns>file upload success or not</returns>
        public bool UploadBigFile(string fileName, string folderPath, string filePath, int blockSize)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > blockSize)
            {
#if DEBUG
                Console.WriteLine($"[disk Upload]file need to be splited");
#endif
                var steps = (int)Math.Ceiling((double)fileInfo.Length / blockSize);
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader br = new BinaryReader(fs))
                    {
                        int couter = 1;
                        bool isReadingComplete = false;
                        while (!isReadingComplete)
                        {
                            string tempName = couter.ToString();

                            byte[] bytes = br.ReadBytes(blockSize);
                            Stream stream = new MemoryStream(bytes);

                            bool result = false;
                            while(!result)
                            {
                                try
                                {
                                    result = Upload(fileName + $"<{couter}/{steps}>", folderPath, stream);
                                }
                                catch(Exception e)
                                {
#if DEBUG
                                    Console.WriteLine($"[disk Upload]fail retry, infomation:" + e.Message);
#endif
                                }
                            }

                            isReadingComplete = (bytes.Length != blockSize);
                            if (!isReadingComplete)
                            {
                                couter += 1;
                            }
                        }
                    }
                }
                return true;
            }
            else
            {
                bool result = false;
                while (!result)
                {
                    try
                    {
                        result = Upload(fileName, folderPath, File.OpenRead(filePath));
                    }
                    catch (Exception e)
                    {
#if DEBUG
                        Console.WriteLine($"[disk Upload]fail retry, infomation:" + e.Message);
#endif
                    }
                }
                return true;
            }
        }

    }
}
