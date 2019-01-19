using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using System;

namespace maildisk
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (var i in args)
                Console.WriteLine(i);

            using (var client = new ImapClient())
            {
                // For demo-purposes, accept all SSL certificates
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                client.Connect("imap.qq.com", 993, true);

                client.Authenticate("lolicon@papapoi.com", "lsykvlybakgkbfda");


                var personal = client.GetFolder(client.PersonalNamespaces[0]);
                foreach (var folder in personal.GetSubfolders(false))
                {
                    Console.WriteLine("[folder] {0}", folder.Name);
                    foreach (var f in folder.GetSubfolders(false))
                        Console.WriteLine("[folder] {0}", folder.Name + "/" + f.Name);
                }

                
                // The Inbox folder is always available on all IMAP servers...
                var inbox = client.GetFolder("其他文件夹/facebook");
                inbox.Open(FolderAccess.ReadOnly);

                Console.WriteLine("Total messages: {0}", inbox.Count);
                Console.WriteLine("Recent messages: {0}", inbox.Recent);
                
                for(int i = 0;i<inbox.Count;i+=100)
                {
                    foreach (var summary in inbox.Fetch(i, i + 100, MessageSummaryItems.Envelope | MessageSummaryItems.UniqueId))
                    {
                        Console.WriteLine("[summary] {0:D2}: {1}", summary.Index, summary.Envelope.Subject);
                    }
                }
                


                client.Disconnect(true);
            }

            Console.WriteLine("end!");
            Console.ReadLine();
        }
    }
}
