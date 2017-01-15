using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace main
{
    public class ConfigEmail
    {
        public String from_email_addr { get; set; }
        public String to_email_addr  { get; set; }
        public String from_mailbox_name  { get; set; }
        public String to_mailbox_name  { get; set; }
        public String subject  { get; set; }
        public String client_connect  { get; set; }
        public String client_port  { get; set; }
        public String client_authenticate_username  { get; set; }
        public String client_authenticate_password  { get; set; }
    }
}
