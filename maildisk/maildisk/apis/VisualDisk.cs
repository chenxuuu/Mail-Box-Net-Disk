using MailKit.Net.Imap;
using System;
using System.Collections.Generic;
using System.Text;

namespace maildisk.apis
{
    class VisualDisk
    {
        private string address;
        private ImapClient client = new ImapClient();

        public VisualDisk(string server, int port, bool useSsl, string account, string password, string address)
        {
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            client.Connect(server, port, useSsl);
            client.Authenticate(account, password);
            this.address = address;
#if DEBUG
            Console.WriteLine($"[disk create]mail client created, on {server}:{port}" +
                $", ssl:{useSsl}, {account}, {address}");
#endif
        }
    }
}
