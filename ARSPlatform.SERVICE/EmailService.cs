using ARSPlatform.SERVICE.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Threading.Tasks;

namespace ARSPlatform.SERVICE
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(_emailSettings.Password) || _emailSettings.Password == "REPLACE_WITH_EMAIL_PASSWORD")
            {
                throw new InvalidOperationException("EmailSettings:Password is not configured. Please set EMAIL_PASSWORD in environment variables.");
            }

            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
            emailMessage.To.Add(new MailboxAddress("", toEmail));
            emailMessage.Subject = subject;
            
            var bodyBuilder = new BodyBuilder { HtmlBody = body };
            emailMessage.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            
            // Bypass SSL certificate validation for containerized/cloud environments
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;

            var secureOption = _emailSettings.Port == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;
            
            // Connect to SMTP Server
            await client.ConnectAsync(_emailSettings.Server, _emailSettings.Port, secureOption);
            
            // Authenticate with Credentials
            await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
            
            // Send email
            await client.SendAsync(emailMessage);
            
            // Disconnect
            await client.DisconnectAsync(true);
        }
    }
}
