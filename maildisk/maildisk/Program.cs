using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using System;
using maildisk.apis;
using System.IO;

namespace maildisk
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (var i in args)
                Console.WriteLine(i);

            var disk = new VisualDisk("imap.qq.com", 993, true,
                "961726194@qq.com", "",
                "lolicon@papapoi.com",
                "smtp.qq.com", 465, true);

            Console.WriteLine(disk.UploadBigFile("测试文件.mp4", 
                "其他文件夹/test",
                @"C:\Users\chenx\OneDrive\others\for_share\H\sf\a\[桜都字幕组][720P][メリー･ジェーン]小女ラムネ第4話みんなの夏休み.mp4",
                1000 * 1000 * 50));


            //var mail = new MailClient("imap.qq.com", 993, true,
            //    "lolicon@papapoi.com", "lsykvlybakgkbfda",
            //    "lolicon@papapoi.com");

            //var folder = mail.GetFolder("其他文件夹/se");
            //foreach (var m in mail.GetNotSeen())
            //{
            //    Console.WriteLine(m.Envelope.Subject);
            //}


            using (var client = new ImapClient())
            {
                //// For demo-purposes, accept all SSL certificates
                //client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                //client.Connect("imap.qq.com", 993, true);

                //client.Authenticate("lolicon@papapoi.com", "lsykvlybakgkbfda");

                //// The Inbox folder is always available on all IMAP servers...
                //var inbox = client.GetFolder("其他文件夹/se");
                //inbox.Open(FolderAccess.ReadOnly);

                //Console.WriteLine("Total messages: {0}", inbox.Count);
                //Console.WriteLine("Recent messages: {0}", inbox.Recent);
                
                //for (int i = 0; i < inbox.Count; i++)
                //{
                //    var message = inbox.GetMessage(i);
                //    Console.WriteLine("Subject: {0}", message.Subject);
                //}

                //for (int i = 0; i < inbox.Count; i += 100)
                //{
                //    foreach (var summary in inbox.Fetch(i, i + 100, MessageSummaryItems.Full | MessageSummaryItems.UniqueId))
                //    {
                //        Console.WriteLine("[summary] {0:D2}: {1}", summary.Index, summary.Envelope.Subject);
                //    }
                //}



                //client.Disconnect(true);
            }

            Console.WriteLine("end!");
            Console.ReadLine();
        }
    }
}
