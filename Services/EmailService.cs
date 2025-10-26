using Microsoft.AspNetCore.Identity.UI.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using ZoraVault.Helpers;
using ZoraVault.Models.Internal;

namespace ZoraVault.Services
{
    /// <summary>
    /// Interface defining the contract for email delivery operations.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends an email message asynchronously.
        /// </summary>
        /// <param name="toEmail">The recipient's email address.</param>
        /// <param name="subject">The subject of the email.</param>
        /// <param name="body">The HTML or plain-text body content.</param>
        Task SendEmailAsync(string toEmail, string subject, string body);
    }

    /// <summary>
    /// EmailService is responsible for handling all outgoing emails
    /// such as verification links, notifications, and system messages.
    /// </summary>
    public class EmailService: IEmailService
    {
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmailService"/> class.
        /// </summary>
        /// <param name="configuration">
        /// The configuration instance used to access SMTP and security settings.
        /// </param>
        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Sends an email using SMTP credentials defined in configuration.
        /// </summary>
        /// <param name="toEmail">The destination email address.</param>
        /// <param name="subject">The subject of the email.</param>
        /// <param name="body">The message body content (HTML supported).</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if required SMTP configuration values are missing.
        /// </exception>
        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            // Load SMTP configuration values
            var server = _configuration["SMTP_SERVER"];
            var port = int.Parse(_configuration["SMTP_PORT"] ?? "465");
            var senderEmail = _configuration["SMTP_SENDER_EMAIL"];
            var senderName = _configuration["SMTP_SENDER_NAME"];
            var username = _configuration["SMTP_USERNAME"];
            var password = _configuration["SMTP_PASSWORD"];
            
            if (string.IsNullOrWhiteSpace(senderEmail))
                throw new ArgumentNullException(nameof(senderEmail), "SMTP_SENDER_EMAIL configuration value is required");

            // Configure and send the email
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

        /// <summary>
        /// Creates a mobile deep link for email verification, embedding a secure JWT token.
        /// </summary>
        /// <param name="user">The public user information used to generate the token.</param>
        /// <returns>A complete deep link that the mobile app can handle to verify the email.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if EMAIL_TOKEN_SECRET or MOBILE_EMAIL_URL is missing from configuration.
        /// </exception>
        public string CreateEmailVerificationLink(PublicUser user)
        {
            var ets = _configuration["EMAIL_TOKEN_SECRET"];
            var deepLink = _configuration["MOBILE_EMAIL_URL"];
            if (string.IsNullOrWhiteSpace(ets) || string.IsNullOrWhiteSpace(deepLink))
                throw new ArgumentNullException("EMAIL_TOKEN_SECRET AND MOBILE_EMAIL_URL configuration values are required");

            // Generate a short-lived JWT (5-minute validity)
            string token = SecurityHelpers.GenerateJWT(user.Id, ets, 5);
            string verificationLink = $"{deepLink}?token={WebUtility.UrlEncode(token)}";

            return verificationLink;
        }

        /// <summary>
        /// Validates and decodes an email verification JWT.
        /// </summary>
        /// <param name="token">The email verification JWT token sent by the client.</param>
        /// <returns>The <see cref="Guid"/> of the verified user.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if EMAIL_TOKEN_SECRET is not defined.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown if the token is invalid, expired, or missing a valid user ID.
        /// </exception>
        public Guid VerifyEmailJWT(string token)
        {
            var ets = _configuration["EMAIL_TOKEN_SECRET"];
            if (string.IsNullOrWhiteSpace(ets))
                throw new ArgumentNullException("EMAIL_TOKEN_SECRET configuration value is required");

            // Validate and decode the JWT
            var claims = SecurityHelpers.ValidateJWT(token, ets)
                ?? throw new UnauthorizedAccessException("Invalid or expired token");

            // Extract user ID (sub claim)
            string userIdStr = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? throw new UnauthorizedAccessException("Invalid token: missing user ID");

            if (!Guid.TryParse(userIdStr, out Guid userId))
                throw new UnauthorizedAccessException("Invalid token: user ID is not a valid GUID");

            return userId;
        }
    }
}
