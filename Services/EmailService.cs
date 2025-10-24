using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace ZoraVault.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }

    public class EmailService: IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var server = _configuration["SMTP_SERVER"];
            var port = int.Parse(_configuration["SMTP_PORT"] ?? "465");
            var senderEmail = _configuration["SMTP_SENDER_EMAIL"];
            var senderName = _configuration["SMTP_SENDER_NAME"];
            var username = _configuration["SMTP_USERNAME"];
            var password = _configuration["SMTP_PASSWORD"];
            
            if (string.IsNullOrWhiteSpace(senderEmail))
                throw new ArgumentNullException(nameof(senderEmail), "SMTP_SENDER_EMAIL configuration value is required");

            using (var client = new SmtpClient(server, port))
            {
                client.Credentials = new NetworkCredential(username, password);
                client.EnableSsl = true;

                var message = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                message.To.Add(toEmail);

                await client.SendMailAsync(message);
            }
        }
    }
}
