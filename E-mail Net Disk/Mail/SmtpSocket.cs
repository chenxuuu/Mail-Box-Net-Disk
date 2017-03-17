using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace E_mail_Net_Disk.Mail
{
    public class SmtpSocket
    {
        private HostName hostName;
        private StreamSocket socket;
        private DataReader reader;
        private DataWriter writer;
        private int port;
        private bool isSsl;
        private string userName;
        private string password;
  
        // bufferlength to read
        private const int bufferLength = 1024;

        public SmtpSocket(string server, int port, bool isSsl)
        {
            this.hostName = new HostName(server);
            this.socket = new StreamSocket();
            this.port = port;
            this.isSsl = isSsl;
        }
        public SmtpSocket(string server, int port, bool isSsl, string userName, string password)
        {
            this.hostName = new HostName(server);
            this.socket = new StreamSocket();
            this.port = port;
            this.isSsl = isSsl;
            this.userName = userName;
            this.password = password;

        }

        public async Task<SmtpResponse> EstablishConnection()
        {
            try
            {
                this.reader = new DataReader(socket.InputStream);
                this.reader.InputStreamOptions = InputStreamOptions.Partial;

                this.writer = new DataWriter(socket.OutputStream);

                if (this.isSsl)
                    await socket.ConnectAsync(this.hostName, this.port.ToString(), SocketProtectionLevel.Ssl);
                else
                    await socket.ConnectAsync(this.hostName, this.port.ToString(), SocketProtectionLevel.PlainSocket);

                return await this.GetResponse("Connect");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }

        }
        public async Task UpgradeToSslAsync()
        {
            try
            {
                await socket.UpgradeToSslAsync(SocketProtectionLevel.Ssl, this.hostName);

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }

        }

        private async Task<SmtpResponse> GetResponse(string from)
        {
            SmtpResponse response = new SmtpResponse();
            bool isStartingLine = true;
            StringBuilder stringBuilder = null;
            int charLen = 3;
            Boolean endOfStream = false;
            SmtpCode code = SmtpCode.None;
            string codeStr = string.Empty;

            try
            {
                stringBuilder = new StringBuilder();

                while (!endOfStream)
                {

                    // There is a Strange beahvior when the bufferLength is exactly the same size as the inputStream
                    await reader.LoadAsync(bufferLength);

                    charLen = Math.Min((int)reader.UnconsumedBufferLength, bufferLength);

                    if (charLen == 0)
                    {
                        endOfStream = true;
                        break;
                    }

                    // If charLen < bufferLength, it's end of stream
                    if (charLen < bufferLength)
                        endOfStream = true;

                    // get the current position
                    int charPos = 0;

                    // Read the buffer
                    byte[] buffer = new byte[charLen];
                    reader.ReadBytes(buffer);

                    do
                    {
                        // get the character
                        char chr = (char)buffer[charPos];

                        // if it's starting point, we can read the first 3 chars.
                        if (isStartingLine)
                        {

                            codeStr += chr;

                            // Get the code
                            if (codeStr.Length == 3)
                            {
                                int codeInt;
                                if (int.TryParse(codeStr, out codeInt))
                                    code = (SmtpCode)codeInt;

                                // next 
                                isStartingLine = false;
                            }

                        }
                        else if (chr == '\r' || chr == '\n')
                        {
                            // Advance 1 byte to get the '\n' if not at the end of the buffer
                            if (chr == '\r' && charPos < (charLen - 1))
                            {
                                charPos++;
                                chr = (char)buffer[charPos];
                            }
                            if (chr == '\n')
                            {
                                KeyValuePair<SmtpCode, String> r = new KeyValuePair<SmtpCode, string>
                                    (code, stringBuilder.ToString());

                                response.Values.Add(r);

                                Debug.WriteLine("{0}{1}", ((int)code).ToString(), stringBuilder.ToString());

                                stringBuilder = new StringBuilder();
                                code = SmtpCode.None;
                                codeStr = string.Empty;
                                isStartingLine = true;
                            }
                        }
                        else
                        {
                            stringBuilder.Append(chr);
                        }

                        charPos++;

                    } while (charPos < charLen);

                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }

            return response;

        }

        private async Task<List<String>> GetResponse2()
        {
            List<String> lines = new List<string>();
            using (MemoryStream ms = await GetResponseStream())
            {
                using (StreamReader sr = new StreamReader(ms))
                {
                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine();

                        if (String.IsNullOrEmpty(line))
                            break;

                        lines.Add(line);
                    }
                }
            }


            return lines;
        }


    private async Task<MemoryStream> GetResponseStream()
    {
        MemoryStream ms = new MemoryStream();

        while (true)
        {
            await reader.LoadAsync(bufferLength);

            if (reader.UnconsumedBufferLength == 0) { break; }

            Int32 index = 0;
            while (reader.UnconsumedBufferLength > 0)
            {
                ms.WriteByte(reader.ReadByte());
                index = index + 1;
            }

            if (index == 0 || index < bufferLength)
                break;
        }

        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }

        public async Task<SmtpResponse> Send(String command)
        {
            Debug.WriteLine(command);
            return await this.Send(Encoding.UTF8.GetBytes(command + System.Environment.NewLine), command);
        }

        public async Task<SmtpResponse> Send(Byte[] bytes, string command)
        {
            try
            {
                writer.WriteBytes(bytes);
                await writer.StoreAsync();
                return await this.GetResponse(command);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }

        }

        public virtual void Close()
        {
            if (this.socket != null)
            {
                this.socket.Dispose();
            }
            if (this.reader != null)
            {
                this.reader.Dispose();
            }
            if (this.writer != null)
            {
                this.writer.Dispose();
            }
            this.socket = null;
            this.reader = null;
            this.writer = null;
        }

       
    }
}
