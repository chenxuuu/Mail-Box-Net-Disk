using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace E_mail_Net_Disk
{
    class settings
    {
        public static string ImapURL { get; set; }
        public static string SmtpURL { get; set; }
        public static int PortNumber { get; set; }
        public static string UserName { get; set; }
        public static string Password { get; set; }
        public static bool IsSSL { get; set; }

        public static async Task SaveSettings()
        {
            StorageFolder folder;
            folder = ApplicationData.Current.RoamingFolder; //获取应用目录的文件夹

            var file_demonstration = await folder.CreateFileAsync("settings", CreationCollisionOption.ReplaceExisting);
            //创建文件

            using (Stream file = await file_demonstration.OpenStreamForWriteAsync())
            {
                using (StreamWriter write = new StreamWriter(file))
                {
                    write.Write(string.Format("{0};{1};{2};{3};{4};{5}",
                                                ImapURL,
                                                SmtpURL,
                                                PortNumber.ToString(),
                                                UserName,
                                                Password,
                                                IsSSL.ToString()
                                               ));
                }
            }
        }

        public static async Task GetSettings()
        {
            StorageFolder folder;
            folder = ApplicationData.Current.RoamingFolder; //获取应用目录的文件夹

            var file_demonstration = await folder.CreateFileAsync("settings", CreationCollisionOption.OpenIfExists);
            //创建文件

            string s;
            using (Stream file = await file_demonstration.OpenStreamForReadAsync())
            {
                using (StreamReader read = new StreamReader(file))
                {
                    s = read.ReadToEnd();
                }
            }

            //DebugTextBlock.Text = s;

            if (s.IndexOf(";") >= 1 && s.IndexOf(";") != s.Length - 1)
            {
                string[] str2;
                int count_temp = 0;
                str2 = s.Split(';');
                foreach (string i in str2)
                {
                    if (count_temp == 0)
                    {
                        ImapURL = i.ToString();
                        count_temp++;
                    }
                    else if (count_temp == 1)
                    {
                        SmtpURL = i.ToString();
                        count_temp++;
                    }
                    else if (count_temp == 2)
                    {
                        PortNumber = int.Parse(i.ToString());
                        count_temp++;
                    }
                    else if (count_temp == 3)
                    {
                        UserName = i.ToString();
                        count_temp++;
                    }
                    else if (count_temp == 4)
                    {
                        Password = i.ToString();
                        count_temp++;
                    }
                    else if (count_temp == 5)
                    {
                        if (i.ToString() == "True")
                        {
                            IsSSL = true;
                        }
                        else
                        {
                            IsSSL = false;
                        }
                        count_temp++;
                    }
                }
            }
        }
    }
}
