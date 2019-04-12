using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ASC.Web.Configuration;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Util;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;


namespace ASC.Web.Services
{
    // This class is used by the application to send Email and SMS
    // when you turn on two-factor authentication in ASP.NET Identity.
    // For more details see this link https://go.microsoft.com/fwlink/?LinkID=532713
    public class AuthMessageSender : IEmailSender, ISmsSender
    {
        private IOptions<ApplicationSettings> _settings;
        public AuthMessageSender(IOptions<ApplicationSettings> settings)
        {
            _settings = settings;
        }
        public async Task SendEmailAsync(string email, string subject, string message)
        {
            //var secrets = new ClientSecrets
            //{
            //    ClientId = Environment.GetEnvironmentVariable("myproject1987"),
            //    ClientSecret = Environment.GetEnvironmentVariable("AIzaSyB3YgvRJGgQ-782yCnbWrBnaNYGJzR8Xrc")
            //};
            //var googleCredentials = await GoogleWebAuthorizationBroker.AuthorizeAsync(secrets, new[] { GmailService.Scope.MailGoogleCom }, email, CancellationToken.None);
            //if (googleCredentials.Token.IsExpired(SystemClock.Default))
            //{
            //    await googleCredentials.RefreshTokenAsync(CancellationToken.None);
            //}


            var emailMessage = new MimeMessage();
            //moja mail adresa
            emailMessage.From.Add(new MailboxAddress(_settings.Value.SMTPAccount));
            //mail za slanje
            emailMessage.To.Add(new MailboxAddress(email));
            //naslov na mailu
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart("plain") { Text = message };

            using (var client = new SmtpClient())
            {
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                await client.ConnectAsync(_settings.Value.SMTPServer, _settings.Value.SMTPPort,
                    SecureSocketOptions.Auto);
                client.AuthenticationMechanisms.Remove("XOAUTH2");
                await client.AuthenticateAsync(_settings.Value.SMTPAccount, _settings.Value.SMTPPassword);
                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);
            }


        }

        public Task SendSmsAsync(string number, string message)
        {
            // Plug in your SMS service here to send a text message.
            return Task.FromResult(0);
        }
    }
}
