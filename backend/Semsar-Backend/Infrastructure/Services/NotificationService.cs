using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Application.Interfaces;
using Polly;
using Polly.Retry;

namespace Infrastructure.Services
{
    public class ResilientNotificationService : INotificationService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<ResilientNotificationService> _logger;
        private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
        private readonly AsyncRetryPolicy _retryPolicy;

        public ResilientNotificationService(IConfiguration config, ILogger<ResilientNotificationService> logger)
        {
            _config = config;
            _logger = logger;

            _retryPolicy = Policy
                .Handle<Exception>(ex => ex is SmtpException || ex is TimeoutException || ex is WebException)
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(exception, "Email sending failed. Retrying ({RetryCount}/3) after {RetrySeconds}s", retryCount, timeSpan.TotalSeconds);
                    });
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(to) || !EmailRegex.IsMatch(to))
                throw new ArgumentException("Invalid email address", nameof(to));

            var host = _config["Smtp:Host"];
            if (string.IsNullOrWhiteSpace(host))
            {
                _logger.LogWarning("SMTP host not configured - skipping email to {To}", to);
                return;
            }

            if (!int.TryParse(_config["Smtp:Port"], out int port))
                port = 587;

            var from = _config["Smtp:From"] ?? "noreply@example.com";
            var user = _config["Smtp:User"];
            var pass = _config["Smtp:Pass"];

            await _retryPolicy.ExecuteAsync(async () =>
            {
                // Only create credentials if both user and pass are configured
                NetworkCredential? credentials = null;
                if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(pass))
                {
                    credentials = new NetworkCredential(user, pass);
                }

                using var client = new SmtpClient(host, port)
                {
                    Credentials = credentials,
                    EnableSsl = true,
                    Timeout = 10000 // 10 second timeout
                };

                using var mail = new MailMessage(from, to, subject ?? string.Empty, body ?? string.Empty)
                {
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8
                };
                await client.SendMailAsync(mail);
                _logger.LogInformation("Email sent to {To}", to);
                return true;
            });
        }

        public string GenerateWhatsAppLink(string phone, string message)
        {
            if (string.IsNullOrEmpty(phone)) throw new ArgumentNullException(nameof(phone));
            if (string.IsNullOrEmpty(message)) message = "";

            // Sanitize phone number - remove non-numeric characters except +
            var sanitizedPhone = Regex.Replace(phone, @"[^\d+]", "");
            return $"https://api.whatsapp.com/send?phone={sanitizedPhone}&text={Uri.EscapeDataString(message)}";
        }
    }
}
