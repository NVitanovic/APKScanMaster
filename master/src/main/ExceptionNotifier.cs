using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit;
using MimeKit;

namespace main
{
    public class EmailNotify
    {
        public static void SendEmail(Config config, String messageBody)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(config.email.from_mailbox_name, config.email.from_email_addr));
                message.To.Add(new MailboxAddress(config.email.to_mailbox_name, config.email.to_email_addr));
                message.Subject = config.email.subject + DateTime.Now;

                message.Body = new TextPart("plain")
                {
                    Text = messageBody
                };

                using (var client = new SmtpClient())
                {
                    // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    client.Connect(config.email.client_connect, Int32.Parse(config.email.client_port), false);

                    // Note: since we don't have an OAuth2 token, disable
                    // the XOAUTH2 authentication mechanism.
                    client.AuthenticationMechanisms.Remove("XOAUTH2");

                    // Note: only needed if the SMTP server requires authentication
                    client.Authenticate(config.email.client_authenticate_username, config.email.client_authenticate_password);

                    client.Send(message);

                    Console.WriteLine("Email sent.");
                    //TODO: treba da se proveri da li se mail stvarno poslao.
                    //event handler mora da se napravi http://www.mimekit.org/docs/html/E_MailKit_MailTransport_MessageSent.htm
                    client.Disconnect(true);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception while sending email");
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}
