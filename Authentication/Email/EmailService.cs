using Authentication.Models;
using IdentityServer3.Core.Services.Default;
using MimeKit;
using MimeKit.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Authentication.Email
{
	public class EmailService : IEmailService
	{
		private readonly IEmailConfiguration _emailConfiguration;

		public EmailService(IEmailConfiguration emailConfiguration)
		{
			_emailConfiguration = emailConfiguration;
		}

		public void Send(EmailMessage emailMessage)
		{
            var messageToSend = new MimeMessage
            {
                Sender = new MailboxAddress(emailMessage.FromAddresses.Name, emailMessage.FromAddresses.Address),
                Subject = emailMessage.Subject,
                Body = new TextPart(TextFormat.Html) { Text = emailMessage.Content },
            };

            messageToSend.To.Add(MailboxAddress.Parse(emailMessage.ToAddresses.Address));
			messageToSend.From.Add(MailboxAddress.Parse(emailMessage.FromAddresses.Address));

            using (var client = new MailKit.Net.Smtp.SmtpClient())
            {
				client.Connect(_emailConfiguration.Host, _emailConfiguration.Port);
				client.AuthenticationMechanisms.Remove("XOAUTH2");
				client.Authenticate(_emailConfiguration.Username, _emailConfiguration.Password);
				client.Send(messageToSend);
				client.Disconnect(true);
			}
        }
	}
}
