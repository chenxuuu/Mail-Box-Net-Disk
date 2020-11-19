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
                    try
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

                                Console.WriteLine($"[disk check]add a seen flag");

                            }
                            client.Disconnect(true);
                        }
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine($"[disk check]fetch error: {e.Message}");
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

            Console.WriteLine($"[disk upload]upload {fileName} to mail folder {folderPath}, size:{file.Length}");

            var client = GetImapClient();
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(address));
            message.To.Add(MailboxAddress.Parse(address));
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

            Console.WriteLine("[disk upload]appending...");

            var folder = GetImapClient().GetFolder(folderPath);
            folder.Open(FolderAccess.ReadWrite);
            var uid = folder.Append(message);
            lastFolder = folderPath;


            Console.WriteLine($"[disk upload]upload success");

            client.Disconnect(true);
            return true;
        }

        /// <summary>
        /// get file list with folder
        /// </summary>
        /// <param name="folderPath">folder</param>
        /// <returns>files' name</returns>
        public string[] GetFileList(string folderPath, bool deleteMissing = false)
        {
            ArrayList mails = new ArrayList();
            var client = GetImapClient();
            var folder = client.GetFolder(folderPath);
            folder.Open(FolderAccess.ReadWrite);
            Console.WriteLine($"find {folder.Count} mails in this folder");
            for(int i = 0;i < folder.Count; i += 100)
            {
                Console.WriteLine($"fatching mails {i}/{folder.Count}");
                int max = i + 100;
                if (max >= folder.Count)
                    max = folder.Count - 1;
                foreach (var m in folder.Fetch(i, max, MessageSummaryItems.Full | MessageSummaryItems.UniqueId))
                {
                    if(m.Envelope.Subject.IndexOf("[mailDisk]") == 0)
                        mails.Add(m.Envelope.Subject.Substring("[mailDisk]".Length));
                }
                if (max == folder.Count)
                    break;
            }
            Console.WriteLine($"mails in {folder.Count} fatched ok.");

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
            if(deleteMissing)
            {
                Console.WriteLine("start cleaning all missing mails");
                for (int i = 0; i < folder.Count; i += 100)
                {
                    Console.WriteLine($"fatching mails {i}/{folder.Count}");
                    int max = i + 100;
                    if (max >= folder.Count)
                        max = folder.Count - 1;
                    foreach (var m in folder.Fetch(i, max, MessageSummaryItems.Full | MessageSummaryItems.UniqueId))
                    {
                        if (m.Envelope.Subject.IndexOf("[mailDisk]") == 0)
                        {
                            string name = m.Envelope.Subject.Substring("[mailDisk]".Length);
                            Console.WriteLine($"checking file {name}");
                            MatchCollection mc = Regex.Matches(name, @"(.+?)<(\d+?)/(\d+?)>");
                            if (mc.Count > 0 && mc[0].Groups.Count == 4)
                                name = mc[0].Groups[1].ToString();
                            if (!files.Contains(name))
                            {
                                Console.WriteLine($"file {name} is not right, mark as deleted");
                                folder.AddFlags(m.UniqueId, MessageFlags.Deleted, true);
                            }
                        }
                    }
                    if (max == folder.Count)
                        break;
                }
                folder.Expunge();
            }
            client.Disconnect(true);
            return (string[])files.ToArray(typeof(string));
        }

        /// <summary>
        /// download file
        /// </summary>
        /// <param name="folderPath">mail folder</param>
        /// <param name="fileName">file save name</param>
        public void DownloadFile(string folderPath, string fileName, string local, 
            ImapClient client = null, 
            IList<IMessageSummary> all = null,
            IMailFolder folder = null)
        {
            if(File.Exists(local))
            {
                Console.WriteLine($"error! file {local} already exist!");
                return;
            }
            if(client == null)
                client = GetImapClient();
            if(all == null)
            {
                folder = client.GetFolder(folderPath);
                folder.Open(FolderAccess.ReadOnly);
                var uids = folder.Search(SearchQuery.SubjectContains($"[mailDisk]{fileName}"));

                Console.WriteLine($"find {uids.Count} matchs in this folder");
                Console.WriteLine($"fatching mails");
                all = folder.Fetch(uids, MessageSummaryItems.Full | MessageSummaryItems.UniqueId);
            }
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
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"error! file {filePath} not exist!");
                return false;
            }
            fileName = fileName.Replace("\\", "/");
            while(fileName.IndexOf("/") == 0)//remove the "/" head
            {
                fileName = fileName.Substring(1);
            }

            FileInfo fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > blockSize)
            {

                Console.WriteLine($"[disk Upload]file need to be splited");

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

        private ArrayList tempFiles = new ArrayList();
        public void RefreshFiles(string folderPath)
        {
            tempFiles.AddRange(GetFileList(folderPath));
            foreach (var s in tempFiles)
            {
                Console.WriteLine(s);
            }
        }

        /// <summary>
        /// upload a folder
        /// </summary>
        /// <param name="cloudPath">cloud folder path</param>
        /// <param name="folderPath">mail folder</param>
        /// <param name="localPath">local folder path</param>
        /// <param name="blockSize">each mail size</param>
        public void UploadFolder(string cloudPath, string folderPath, string localPath, int blockSize)
        {
            while (cloudPath.LastIndexOf("/") == cloudPath.Length - 1)//remove last "/"
            {
                cloudPath = cloudPath.Substring(0, cloudPath.Length - 1);
            }
            Console.WriteLine($"[disk upload folder]upload {localPath} to {cloudPath}");
            if (!Directory.Exists(localPath))
            {
                Console.WriteLine($"error! folder {localPath} not exist!");
                return;
            }

            foreach (var f in Directory.GetDirectories(localPath))
            {
                int l = f.LastIndexOf("/");
                if(l == -1)
                    l = f.LastIndexOf("\\");
                UploadFolder(cloudPath + f.Substring(l), folderPath, f, blockSize);
            }
            foreach(var f in Directory.GetFiles(localPath))
            {
                int l = f.LastIndexOf("/");
                if (l == -1)
                    l = f.LastIndexOf("\\");

                string fileName = cloudPath + f.Substring(l);
                fileName = fileName.Replace("\\", "/");
                while (fileName.IndexOf("/") == 0)//remove the "/" head
                {
                    fileName = fileName.Substring(1);
                }

                if (tempFiles.Contains(fileName))
                    Console.WriteLine($"[disk upload folder] file {fileName} is" +
                        $" already exist in mail disk, skip upload.");
                else
                    UploadBigFile(fileName, folderPath, f, blockSize);
            }
        }

        /// <summary>
        /// download a floder
        /// </summary>
        /// <param name="cloudPath">cloud folder path</param>
        /// <param name="folderPath">mail folder</param>
        /// <param name="localPath">local folder path</param>
        public void DownloadFolder(string cloudPath, string folderPath, string localPath)
        {
            while (cloudPath.LastIndexOf("/") == cloudPath.Length - 1)//remove last "/"
            {
                cloudPath = cloudPath.Substring(0, cloudPath.Length - 1);
            }
            cloudPath += "/";
            localPath = localPath.Replace("\\", "/");
            if (localPath.LastIndexOf("/") != localPath.Length - 1)
                localPath += "/";
            Console.WriteLine($"[disk download folder]download {cloudPath} to {localPath}");

            var client = GetImapClient();
            var folder = client.GetFolder(folderPath);
            folder.Open(FolderAccess.ReadOnly);

            Console.WriteLine($"find {folder.Count} files in this folder");
            Console.WriteLine($"fatching mails");
            var all = folder.Fetch(0,-1, MessageSummaryItems.Full | MessageSummaryItems.UniqueId);

            foreach (string f in GetFileList(folderPath))
            {
                if(f.IndexOf(cloudPath) == 0)//match folder
                {
                    string localFile = localPath + f.Substring(cloudPath.Length);
                    localPath = localPath.Replace("\\", "/");
                    int l = localFile.LastIndexOf("/");
                    Directory.CreateDirectory(localFile.Substring(0, l));
                    DownloadFile(folderPath, f, localFile, client, all, folder);
                }
            }
        }
    }
}
