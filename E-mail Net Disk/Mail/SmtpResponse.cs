using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E_mail_Net_Disk.Mail
{
    public class SmtpResponse
    {

        public List<KeyValuePair<SmtpCode, string>> Values { get; set; }

        
        public SmtpResponse()
        {
            Values = new List<KeyValuePair<SmtpCode, string>>();
 
        }

        public String GetMessages()
        {
            StringBuilder sb = new StringBuilder();

            foreach(var kvp in Values)
            {
                sb.Append(kvp.Value);
            }

            return sb.ToString();
        }

        public bool ContainsStatus(SmtpCode status)
        {
            if (this.Values.Count == 0)
                return false;

            return this.Values.Any(kvp => kvp.Key == status);
        }

        public bool ContainsMessage(string message)
        {
            if (this.Values.Count == 0)
                return false;

            return this.Values.Any(kvp => kvp.Value.Contains(message));
        }

        public static SmtpResponse GetResponse()
        {
            return null;
        }


       
    }
}
