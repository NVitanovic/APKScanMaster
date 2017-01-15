using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit;
using MimeKit;

namespace main
{
    public class ExceptionNotifier
    {
        public static void SendEmail(String messageBody, String to)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Exception Error", "exceptionreport@outlook.com"));
            message.To.Add(new MailboxAddress("Exception/Error ", to));
            message.Subject = "apkscan.online Error " + DateTime.Now;

            message.Body = new TextPart("plain")
            {
                Text = messageBody
            };

            using (var client = new SmtpClient())
            {
                // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                client.Connect("smtp-mail.outlook.com", 587, false);

                // Note: since we don't have an OAuth2 token, disable
                // the XOAUTH2 authentication mechanism.
                client.AuthenticationMechanisms.Remove("XOAUTH2");

                // Note: only needed if the SMTP server requires authentication
                client.Authenticate("exceptionreport@outlook.com", "apkscan.online");

                client.Send(message);
                //TODO: treba da se proveri da li se mail stvarno poslao.
                //event handler mora da se napravi http://www.mimekit.org/docs/html/E_MailKit_MailTransport_MessageSent.htm
                client.Disconnect(true);
            }
        }
    }
}
