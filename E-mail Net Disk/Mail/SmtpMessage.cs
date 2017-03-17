using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E_mail_Net_Disk.Mail
{
    /// Represent smtp message.
    /// <summary>
    /// Represent smtp message.
    /// </summary>
    public class SmtpMessage
    {
        private List<String> _EncodeHeaderKeys = new List<String>();
        private String _From = null;
        private List<String> to = new List<String>();
        private List<String> cc = new List<String>();
        private List<String> bcc = new List<String>();
        private SmtpPriority mailPriority = SmtpPriority.Normal;
        private Encoding _HeaderEncoding = Encoding.UTF8;
        
        private List<String> headers = new List<string>();


        public Encoding Encoding { get; set; }
        
        public new String From { get; set; }

        public List<String> To
        {
            get { return this.to; }
        }

        public String Boundary { get; set; }
        /// <summary>
        /// 7 bit
        /// Base64
        /// Quoted-Printable
        /// </summary>
        public String TransferEncoding { get; set; }

        public List<String> Cc
        {
            get { return this.cc; }
        }

        public List<String> Bcc
        {
            get { return this.bcc; }
        }
        public SmtpPriority Priority { get; set; }

        public String Subject { get; set; }

        public String Body { get; set; }
        public Boolean IsHtml { get; set; }

        /// <summary>
        /// text/html
        /// text/plain
        /// </summary>
        public String ContentType { get; set; }

        public SmtpMessage()
        {
            this.Initialize();
        }

        public SmtpMessage(String mailFrom, String to, String cc, String subject, String message)
        {
            this.Initialize();
            this.From = mailFrom;
            if (String.IsNullOrEmpty(to) == false)
                this.To.Add(to);

            if (String.IsNullOrEmpty(cc) == false)
                this.Cc.Add(cc);

            this.Subject = subject;
            this.Body = message;
        }

        private void Initialize()
        {
            this.Boundary = String.Format("_MESSAGE_ID_{0}", Guid.NewGuid().ToString());
            this.TransferEncoding = "7bit";
            this.Encoding = Encoding.UTF8;

          }

public String GetBody()
{
    StringBuilder sb = new StringBuilder();

    var dateFormat = "ddd, dd MMM yyyy HH:mm:ss +0000";
    sb.AppendFormat("Date: {0}{1}", DateTime.Now.ToString(dateFormat), System.Environment.NewLine);

    if (String.IsNullOrEmpty(this.From))
        throw new Exception("From is mandatory");

    sb.AppendFormat("X-Priority: {0}{1}", ((byte)this.Priority).ToString(), System.Environment.NewLine);

    if (this.to.Count == 0)
        throw new Exception("To is mandatory");

    sb.Append("To: ");
    for (int i = 0; i < this.to.Count; i++)
    {
        var to = this.to[i];
        if (i == this.to.Count - 1)
            sb.AppendFormat("{0}{1}", to, System.Environment.NewLine);
        else 
            sb.AppendFormat("{0}{1}", to, ", ");

    }
    foreach (var to in this.To)
  
    if (this.cc.Count != 0)
    {
        sb.Append("Cc: ");
        for (int i = 0; i < this.cc.Count; i++)
        {
            var cc = this.cc[i];
            if (i == this.cc.Count - 1)
                sb.AppendFormat("{0}{1}", cc, System.Environment.NewLine);
            else
                sb.AppendFormat("{0}{1}", cc, ", ");

        }
    }

    sb.AppendFormat("MIME-Version: 1.0{0}", System.Environment.NewLine);
    sb.AppendFormat("Content-Transfer-Encoding: {0}{1}", this.TransferEncoding, System.Environment.NewLine);
    sb.AppendFormat("Content-Disposition: inline{0}", System.Environment.NewLine);
    sb.AppendFormat("Subject: {0}{1}" , this.Subject, System.Environment.NewLine);

    if (this.IsHtml)
        sb.AppendFormat("Content-Type: text/html; {0}", System.Environment.NewLine);
    else
        sb.AppendFormat("Content-Type: text/plain; charset=\"{0}\"{1}", this.Encoding.WebName, System.Environment.NewLine);

    sb.Append(System.Environment.NewLine);
    sb.Append(this.Body);
    sb.Append(System.Environment.NewLine);
    sb.Append(".");

    return sb.ToString();

}


    }
}
