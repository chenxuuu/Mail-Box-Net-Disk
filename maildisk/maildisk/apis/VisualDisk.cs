using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using MimeKit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
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
        private string lastFolder;

        public VisualDisk(string imapServer, int port, bool useSsl, string account, string password, string address)
        {
            this.imapServer = imapServer;
            this.imapPort = port;
            this.imapSsl = useSsl;
            this.address = address;
            this.account = account;
            this.password = password;
            //auto mark as seen task
            Task.Run(() => {
                while(true)
                {
                    if (lastFolder != null)
                    {
                        var client = GetImapClient();
                        var f = client.GetFolder(lastFolder);
                        f.Open(FolderAccess.ReadWrite);
                        var uids = f.Search(SearchQuery.NotSeen);
                        foreach (var u in uids)
                        {
                            f.AddFlags(u, MessageFlags.Seen, true);
#if DEBUG
                            Console.WriteLine($"[disk check]add a seen flag");
#endif
                        }
                        client.Disconnect(true);
                    }
                    Task.Delay(30 * 1000).Wait();
                }
            });
        }

        /// <summary>
        /// get a imap client
        /// </summary>
        /// <returns>imap client</returns>
        public ImapClient GetImapClient()
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
        private bool Upload(string fileName, string folderPath, Stream file)
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
                "This mail was send by mail disk\r\n" +
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
            lastFolder = folderPath;

#if DEBUG
            Console.WriteLine($"[disk upload]upload success");
#endif
            client.Disconnect(true);
            return true;
        }

        /// <summary>
        /// get file list with folder
        /// </summary>
        /// <param name="folderPath">folder</param>
        /// <returns>files' name</returns>
        public string[] GetFileList(string folderPath)
        {
            ArrayList mails = new ArrayList();
            var client = GetImapClient();
            var folder = client.GetFolder(folderPath);
            folder.Open(FolderAccess.ReadOnly);
            Console.WriteLine($"find {folder.Count} mails in this folder");
            for(int i = 0;i < folder.Count; i += 100)
            {
                Console.WriteLine($"fatching mails {i}/{folder.Count}");
                foreach (var m in folder.Fetch(i, i + 100, MessageSummaryItems.Full | MessageSummaryItems.UniqueId))
                {
                    if(m.Envelope.Subject.IndexOf("[mailDisk]") == 0)
                        mails.Add(m.Envelope.Subject.Substring("[mailDisk]".Length));
                }
            }
            
            ArrayList files = new ArrayList();
            foreach(string f in mails)
            {
                if (f.IndexOf("<")>=0)
                {
                    MatchCollection mc = Regex.Matches(f, @"(.+?)<1/(\d+?)>");
                    if (mc.Count > 0 && mc[0].Groups.Count == 3)
                    {
                        Console.WriteLine($"find file {mc[0].Groups[1]} with {mc[0].Groups[2]} parts,checking...");
                        bool result = true;//check is it have all files
                        int sum = int.Parse(mc[0].Groups[2].ToString());
                        for(int i=1;i<=sum;i++)
                        {
                            if(!mails.Contains($"{mc[0].Groups[1]}<{i}/{sum}>"))
                            {
                                result = false;
                                break;
                            }
                        }
                        if(result)
                        {
                            files.Add(mc[0].Groups[1].ToString());
                            Console.WriteLine($"file {mc[0].Groups[1]} check ok");
                        }
                        else
                            Console.WriteLine($"file {mc[0].Groups[1]}'s parts are missing");
                    }
                        
                }
                else
                {
                    files.Add(f);
                }
            }
            client.Disconnect(true);
            Console.WriteLine($"\r\n\r\ndone! list of files:");
            return (string[])files.ToArray(typeof(string));
        }

        /// <summary>
        /// download file
        /// </summary>
        /// <param name="folderPath">mail folder</param>
        /// <param name="fileName">file save name</param>
        public void DownloadFile(string folderPath, string fileName, string local)
        {
            if(File.Exists(local))
            {
                Console.WriteLine($"error! file {local} already exist!");
                return;
            }
            var client = GetImapClient();
            var folder = client.GetFolder(folderPath);
            folder.Open(FolderAccess.ReadOnly);
            var uids = folder.Search(SearchQuery.SubjectContains($"[mailDisk]{fileName}"));
            
            ArrayList fileNames = new ArrayList();
            Console.WriteLine($"find {uids.Count} matchs in this folder");
            Console.WriteLine($"fatching mails");
            var all = folder.Fetch(uids, MessageSummaryItems.Full | MessageSummaryItems.UniqueId);
            bool singleFile = true;
            int fileSum = 0;
            bool hasFile = false;
            foreach(var m in all)
            {
                string subject = m.Envelope.Subject.Substring("[mailDisk]".Length);
                if (subject.IndexOf(fileName) == 0 && (subject.Length == fileName.Length || subject.Substring(fileName.Length, 1) == "<"))
                {
                    if(subject.Length == fileName.Length)
                    {
                        hasFile = true;
                        break;
                    }
                    MatchCollection mc = Regex.Matches(subject, @"<1/(\d+?)>");
                    if (mc.Count > 0 && mc[0].Groups.Count == 2)
                    {
                        fileSum = int.Parse(mc[0].Groups[1].ToString());
                        singleFile = false;
                        hasFile = true;
                        break;
                    }
                }
            }

            if(!hasFile)
            {
                Console.WriteLine($"error! file not exist!");
                return;
            }

            if(singleFile)
            {
                foreach (var m in all)
                {
                    if(m.Envelope.Subject.IndexOf($"[mailDisk]{fileName}") == 0)
                    {
                        while(true)
                        {
                            try
                            {
                                using (var output = File.Create(local))
                                {
                                    var r = Download(folder, m.UniqueId);
                                    r.Position = 0;
                                    r.CopyTo(output);
                                    break;
                                }
                            }
                            catch(Exception e)
                            {
                                Console.WriteLine($"[disk Download]fail, retry, infomation:" + e.Message);
                            }
                        }
                    }
                }
            }
            else
            {
                ArrayList mails = new ArrayList();
                foreach (var m in all)
                {
                    string subject = m.Envelope.Subject.Substring("[mailDisk]".Length);
                    mails.Add(subject);
                }
                Console.WriteLine($"find file {fileName} with {fileSum} parts,checking...");
                bool result = true;//check is it have all files
                for (int i = 1; i <= fileSum; i++)
                {
                    if (!mails.Contains($"{fileName}<{i}/{fileSum}>"))
                    {
                        result = false;
                        break;
                    }
                }
                if (result)
                {
                    Console.WriteLine($"file {fileName} check ok, begin download...");
                    using (var output = File.Create(local))
                    {
                        for (int i = 1; i <= fileSum; i++)
                        {
                            foreach (var m in all)
                            {
                                if (m.Envelope.Subject.IndexOf($"[mailDisk]{fileName}<{i}/{fileSum}>") == 0)
                                {
                                    while (true)
                                    {
                                        try
                                        {
                                            Console.WriteLine($"downloading {fileName}<{i}/{fileSum}> ...");
                                            var r = Download(folder, m.UniqueId);
                                            r.Position = 0;
                                            r.CopyTo(output);
                                            break;
                                        }
                                        catch (Exception e)
                                        {
                                            Console.WriteLine($"[disk Download]fail, retry, infomation:" + e.Message);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"file {fileName}'s parts are missing, download fail");
                    return;
                } 
            }
            Console.WriteLine($"file {fileName} download success!");
        }

        /// <summary>
        /// download a mail's attachments
        /// </summary>
        /// <param name="folder">file folder</param>
        /// <param name="id">mail uid</param>
        /// <returns>file's stream</returns>
        private Stream Download(IMailFolder folder, UniqueId id)
        {
            Stream stream = new MemoryStream();
            MimeMessage message = folder.GetMessage(id);
            foreach (MimePart attachment in message.Attachments)
            {
                //下载附件
                using (var cancel = new CancellationTokenSource())
                {
                    attachment.Content.DecodeTo(stream, cancel.Token);
                    return stream;
                }
            }
            return stream;
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
                                    Console.WriteLine($"[disk Upload]fail, retry, infomation:" + e.Message);
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
                        Console.WriteLine($"[disk Upload]fail, retry, infomation:" + e.Message);
                    }
                }
                return true;
            }
        }


        /// <summary>
        /// get all folders in this mail
        /// </summary>
        /// <param name="path">path, default is empty</param>
        /// <returns>folder list</returns>
        public IMailFolder[] GetFolders(string path = "")
        {
            ArrayList folders = new ArrayList();

            var personal = GetImapClient().GetFolder(path);

            foreach (var folder in personal.GetSubfolders(false))
            {
                if (folder.GetSubfolders(false).Count > 0)
                {
                    folders.Add(folder);
                    folders.AddRange(GetFolders(folder.FullName));
                }
                else
                {
                    folders.Add(folder);
                }
            }
            return (IMailFolder[])folders.ToArray(typeof(IMailFolder));
        }


        /// <summary>
        /// create a folder
        /// </summary>
        /// <param name="path">folder name and path</param>
        /// <returns>result</returns>
        public void CreatFolder(string path)
        {
            var client = GetImapClient();
            if(path.IndexOf("/") < 0)
            {
                var root = client.GetFolder("");
                root.Create(path, true);
            }
            else
            {
                int l = path.LastIndexOf("/");
                var folder = client.GetFolder(path.Substring(0,l));
                folder.Create(path.Substring(l+1), true);
            }
            Console.WriteLine($"[disk folder]folder {path} is created.");
        }
    }
}
