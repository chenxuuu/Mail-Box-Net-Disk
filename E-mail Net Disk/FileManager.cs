using E_mail_Net_Disk.Models;
using MailKit.Net.Smtp;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace E_mail_Net_Disk
{
    class FileManager
    {

        public static async Task UploadFiles(StorageFile file)
        {
            string hash= file.GetHashCode().ToString();
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("File Uploader", settings.UserName));
            message.To.Add(new MailboxAddress(settings.UserName, settings.UserName));
            message.Subject = "mailbox net disk file:" + hash + "File name:" + file.Name;

            StorageFolder folder = ApplicationData.Current.TemporaryFolder;
            StorageFile newfile = await file.CopyAsync(folder, file.Name, NameCollisionOption.GenerateUniqueName);

            var body = new TextPart("plain")
            {
                Text = @"WORNING:This e-mail was sent by mailbox net disk,don't delete this.
" + hash
            };

            // create an image attachment for the file located at path
            var attachment = new MimePart()
            {
                ContentObject = new ContentObject(File.OpenRead(newfile.Path), ContentEncoding.Default),
                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                ContentTransferEncoding = ContentEncoding.Base64,
                FileName = newfile.Name
            };

            // now create the multipart/mixed container to hold the message text and the
            // image attachment
            var multipart = new Multipart("mixed");
            multipart.Add(body);
            multipart.Add(attachment);

            // now set the multipart/mixed as the message body
            message.Body = multipart;

            using (var client = new SmtpClient())
            {
                client.Connect(settings.SmtpURL, settings.PortNumber, settings.IsSSL);

                // Note: since we don't have an OAuth2 token, disable
                // the XOAUTH2 authentication mechanism.
                client.AuthenticationMechanisms.Remove("XOAUTH2");

                // Note: only needed if the SMTP server requires authentication
                client.Authenticate(settings.UserName, settings.Password);

                client.Send(message);
                client.Disconnect(true);
            }
        }

        public static async Task SaveSettings(ObservableCollection<NetFileItem> netfiles)
        {
            StorageFolder folder;
            folder = ApplicationData.Current.RoamingFolder; //获取应用目录的文件夹

            var file_demonstration = await folder.CreateFileAsync("FilesCache", CreationCollisionOption.ReplaceExisting);
            //创建文件

            string FilesCacheText="";

            foreach (var i in netfiles)
            {
                FilesCacheText += string.Format("{0}|{1}|{2}|{3}|{4}*",
                                                i.FileName,
                                                i.FileHash,
                                                i.FileSize,
                                                i.FileDateCreated,
                                                i.FileSum.ToString()
                                                );
            }

            using (Stream file = await file_demonstration.OpenStreamForWriteAsync())
            {
                using (StreamWriter write = new StreamWriter(file))
                {
                    write.Write(FilesCacheText);
                }
            }
            //await ShowMessageDialog(file_demonstration.Path, "FilesCacheText");
        }

        private static async Task ShowMessageDialog(string s, string title)
        {
            var msgDialog = new Windows.UI.Popups.MessageDialog(s) { Title = title };
            msgDialog.Commands.Add(new Windows.UI.Popups.UICommand("确定"));
            await msgDialog.ShowAsync();
        }

        public static async Task GetFilesLocal(ObservableCollection<NetFileItem> netfiles)
        {
            StorageFolder folder;
            folder = ApplicationData.Current.RoamingFolder; //获取应用目录的文件夹
            
            var file_demonstration = await folder.CreateFileAsync("FilesCache", CreationCollisionOption.OpenIfExists);
            //创建文件

            string s;
            using (Stream file = await file_demonstration.OpenStreamForReadAsync())
            {
                using (StreamReader read = new StreamReader(file))
                {
                    s = read.ReadToEnd();
                }
            }

            netfiles.Clear();
            //ObservableCollection<NetFileItem> netfiles = new ObservableCollection<NetFileItem>();
            string filename = " ", filehash = " ", filesize = " ", filedata = " ";
            int filesum = 0;

            //DebugTextBlock.Text = s;

            if (s.IndexOf("*") >= 1)
            {
                string[] FilesString;
                FilesString = s.Split('*');
                foreach (string FileString in FilesString)
                {
                    if (FileString == "")
                        break;
                    string[] Files;
                    int count_temp = 0;
                    Files = FileString.Split('|');
                    foreach (string i in Files)
                    {
                        if (count_temp == 0)
                        {
                            filename = i;
                            count_temp++;
                        }
                        else if (count_temp == 1)
                        {
                            filehash = i;
                            count_temp++;
                        }
                        else if (count_temp == 2)
                        {
                            filesize = i;
                            count_temp++;
                        }
                        else if (count_temp == 3)
                        {
                            filedata = i;
                            count_temp++;
                        }
                        else if (count_temp == 4)
                        {
                            filesum = int.Parse(i);
                            count_temp++;
                        }
                    }
                    netfiles.Add(new NetFileItem {  FileName = filename,
                                                    FileHash = filehash,
                                                    FileSize = filesize,
                                                    FileDateCreated = filedata,
                                                    FileSum = (uint)filesum });
                }
            }
            //return netfiles;
        }

        public static string FileSizeConvert(ulong size)
        {
            if (size < 1024)
            {
                return size.ToString() + "B";
            }
            else if (size / 1024 < 1024)
            {
                return ((float)size / 1024).ToString("0.00") + "KB";
            }
            else if (size / 1024 / 1024 < 1024)
            {
                return ((float)size / 1024 / 1024).ToString("0.00") + "MB";
            }
            else
            {
                return ((float)size / 1024 / 1024 / 1024).ToString("0.00") + "GB";
            }
        }
    }
}
