using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace E_mail_Net_Disk.Mail
{
    public class SmtpClient
    {
        public String Server { get; set; }
        public int Port { get; set; }
        public String UserName { get; set; }
        public String Password { get; set; }
        public Boolean IsSsl { get; set; }
        public Boolean IsConnected { get; set; }
        public Boolean IsAuthenticated { get; set; }
        public SmtpSocket smtpSocket { get; set; }

        public SmtpClient(String server, int port)
        {
            this.Server = server;
            this.Port = port;
        }
        public SmtpClient(String server, int port, Boolean isSsl)
            : this(server, port)
        {
            this.IsSsl = isSsl;
        }
        public SmtpClient(String server, int port, string userName, string password, Boolean isSsl)
            : this(server, port, isSsl)
        {
            this.UserName = userName;
            this.Password = password;
        }

        /// <summary>
        /// Connect to server
        /// </summary>
        public async Task<Boolean> Connect()
        {
            try
            {
                if (this.IsConnected)
                {
                    this.smtpSocket.Close();
                    this.IsConnected = false;
                }

                this.smtpSocket = new SmtpSocket(this.Server, this.Port, this.IsSsl, this.UserName, this.Password);
                var response = await this.smtpSocket.EstablishConnection();

                if (response.ContainsStatus(SmtpCode.ServiceReady))
                {
                    this.IsConnected = true;
                    return true;
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;

            }

            return false;
        }


        public async Task<Boolean> Authenticate()
        {
            if (!this.IsConnected)
                throw new Exception("Not connected");

            // get the type of auth
            var rs = await this.smtpSocket.Send("EHLO " + this.Server);

            if (rs.ContainsMessage("STARTTLS"))
            {
                var rsStartTls = await smtpSocket.Send("STARTTLS");

                if (rsStartTls.ContainsStatus(SmtpCode.ServiceReady))
                {
                    await this.smtpSocket.UpgradeToSslAsync();
                    return await Authenticate();
                }
            }

            if (rs.ContainsMessage("AUTH"))
            {
                if (rs.ContainsMessage("LOGIN"))
                    this.IsAuthenticated = await this.AuthenticateByLogin();
                else if (rs.ContainsMessage("PLAIN"))
                    this.IsAuthenticated = await this.AuthenticateByPlain();
            }
            else
            {
                await this.smtpSocket.Send("EHLO " + this.Server);
                this.IsAuthenticated = true;
            }

            return this.IsAuthenticated;

        }

        public async Task<Boolean> AuthenticateByLogin()
        {
            if (!this.IsConnected)
                return false;

            var rs = await this.smtpSocket.Send("Auth Login");

            if (!rs.ContainsStatus(SmtpCode.WaitingForAuthentication))
                return false;

            var rsU = await this.smtpSocket.Send(Convert.ToBase64String(Encoding.UTF8.GetBytes(this.UserName)));

            if (!rsU.ContainsStatus(SmtpCode.WaitingForAuthentication))
                return false;

            var rsP = await this.smtpSocket.Send(Convert.ToBase64String(Encoding.UTF8.GetBytes(this.Password)));

            if (!rsP.ContainsStatus(SmtpCode.AuthenticationSuccessful))
                return false;

            return true;

        }

        public async Task<Boolean> AuthenticateByPlain()
        {
            if (!this.IsConnected)
                return false;

            var rs = await this.smtpSocket.Send("Auth Plain");

            if (!rs.ContainsStatus(SmtpCode.WaitingForAuthentication))
                return false;

            var lineAuthentication = String.Format("{0}\0{0}\0{1}", this.UserName, this.Password);

            var rsA = await this.smtpSocket.Send(Convert.ToBase64String(Encoding.UTF8.GetBytes(lineAuthentication)));

            if (!rsA.ContainsStatus(SmtpCode.AuthenticationSuccessful))
                return false;

            return true;

        }




        public async Task<Boolean> SendMail(SmtpMessage message)
        {

            if (!this.IsConnected)
                await this.Connect();

            if (!this.IsConnected)
                throw new Exception("Can't connect");

            if (!this.IsAuthenticated)
                await this.Authenticate();

            var rs = await this.smtpSocket.Send(String.Format("Mail From:<{0}>", message.From));

            if (!rs.ContainsStatus(SmtpCode.RequestedMailActionCompleted))
                return false;

            foreach (var to in message.To)
            {
                var toRs = await this.smtpSocket.Send(String.Format("Rcpt To:<{0}>", to));

                if (!toRs.ContainsStatus(SmtpCode.RequestedMailActionCompleted))
                    break;
            }

            var rsD = await this.smtpSocket.Send(String.Format("Data"));

            if (!rsD.ContainsStatus(SmtpCode.StartMailInput))
                return false;

            var rsM = await this.smtpSocket.Send(message.GetBody());

            if (!rsM.ContainsStatus(SmtpCode.RequestedMailActionCompleted))
                return false;

            var rsQ = await this.smtpSocket.Send("Quit");

            if (!rsQ.ContainsStatus(SmtpCode.ServiceClosingTransmissionChannel))
                return false;

            
            return true;
        }


        ///// <summary>
        ///// get a response from server
        ///// </summary>
        //private async Task<List<SmtpLine>> GetResponse()
        //{
        //    List<SmtpLine> lines = new List<SmtpLine>();
        //    using (MemoryStream ms = await GetResponseStream())
        //    {
        //        using (StreamReader sr = new StreamReader(ms))
        //        {
        //            while (!sr.EndOfStream)
        //            {
        //                var line = sr.ReadLine();

        //                if (String.IsNullOrEmpty(line))
        //                    break;

        //                lines.Add(new SmtpLine(line));
        //            }
        //        }
        //    }


        //    return lines;
        //}



        //private async Task<MemoryStream> GetResponseStream()
        //{
        //    MemoryStream ms = new MemoryStream();

        //    while (true)
        //    {
        //        await reader.LoadAsync(bufferLength);

        //        if (reader.UnconsumedBufferLength == 0) { break; }

        //        Int32 index = 0;
        //        while (reader.UnconsumedBufferLength > 0)
        //        {
        //            ms.WriteByte(reader.ReadByte());
        //            index = index + 1;
        //        }

        //        if (index == 0 || index < bufferLength)
        //            break;
        //    }

        //    ms.Seek(0, SeekOrigin.Begin);
        //    return ms;
        //}
    }
}
