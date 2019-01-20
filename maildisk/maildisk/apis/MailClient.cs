using MailKit;
using MailKit.Net.Imap;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace maildisk.apis
{
    class MailClient
    {
        private string address;
        private ImapClient client = new ImapClient();

        /// <summary>
        /// new mail client
        /// </summary>
        /// <param name="server">server address</param>
        /// <param name="port">server port</param>
        /// <param name="useSsl">server use ssl or not</param>
        /// <param name="account">your account</param>
        /// <param name="password">your password</param>
        /// <param name="address">your email address</param>
        public MailClient(string server, int port, bool useSsl, string account, string password, string address)
        {
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            client.Connect(server, port, useSsl);
            client.Authenticate(account, password);
            this.address = address;
#if DEBUG
            Console.WriteLine($"[mail create]mail client created, on {server}:{port}" +
                $", ssl:{useSsl}, {account}, {address}");
#endif
        }

        /// <summary>
        /// get all folders in this mail
        /// </summary>
        /// <param name="path">path, default is empty</param>
        /// <returns>folder list</returns>
        public IMailFolder[] GetFolders(string path = "")
        {
            ArrayList folders = new ArrayList();
            
            var personal = client.GetFolder(path);

            foreach (var folder in personal.GetSubfolders(false))
            {
                if (folder.GetSubfolders(false).Count > 0)
                {
#if DEBUG
                    Console.WriteLine($"[folder open] {path + folder.Name}");
#endif
                    folders.AddRange(GetFolders(folder.FullName));
                }
                else
                {
                    folders.Add(folder);
#if DEBUG
                    Console.WriteLine($"[folder get] {folder.FullName}");
#endif
                }
            }
            return (IMailFolder[])folders.ToArray(typeof(IMailFolder));
        }

        /// <summary>
        /// get one folder
        /// </summary>
        /// <param name="path">path, default is empty</param>
        /// <returns>folder</returns>
        public IMailFolder GetFolder(string path)
        {
            return client.GetFolder(path);
        }
    }
}
