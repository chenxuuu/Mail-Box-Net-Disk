using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace maildisk.apis
{
    class Settings
    {
        private static string path = Directory.GetCurrentDirectory();

        /// <summary>
        /// check setting file exist
        /// </summary>
        /// <returns>file exist</returns>
        public static bool CheckSetings()
        {
            return File.Exists(path + "/mail.json");
        }

        /// <summary>
        /// create settings file
        /// </summary>
        public static void Set()
        {
            Console.WriteLine("Notice: If you're using QQMail, pleause create a login code on mail.qq.com\r\n");
            JObject o = new JObject();
            Console.Write("Enter the imap server address:");
            o["imap"] = Console.ReadLine();

            Console.Write("Enter the imap server port(default 993):");
            try { o["port"] = int.Parse(Console.ReadLine()); }
            catch { o["port"] = 993; }

            Console.Write("Use ssl?(y/n):");
            o["ssl"] = Console.ReadLine() == "y" ? true : false;

            Console.Write("Account:");
            o["account"] = Console.ReadLine();

            Console.Write("Password:");
            o["password"] = Console.ReadLine();

            Console.Write("E-mail address:");
            o["address"] = Console.ReadLine();

            File.WriteAllText(path + "/mail.json", o.ToString());

            Console.WriteLine("\r\ndone! enjoy!");
        }

        /// <summary>
        /// return a VisualDisk
        /// </summary>
        /// <returns>VisualDisk</returns>
        public static VisualDisk GetDisk()
        {
            if (CheckSetings())
            {
                string s = File.ReadAllText(path + "/mail.json");
                JObject jo = (JObject)JsonConvert.DeserializeObject(s);
                var disk = new VisualDisk(
                    (string)jo["imap"],
                    (int)jo["port"],
                    (bool)jo["ssl"],
                    (string)jo["account"],
                    (string)jo["password"],
                    (string)jo["address"]);
                return disk;
            }
            else
            {
                Console.WriteLine("settings not found\r\npleause use -s command to set imap settings");
                return null;
            }
        }
    }
}
