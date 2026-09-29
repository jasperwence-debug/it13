using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using App.Domain.Models.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace App.Infrastructure.Services.Email
{
    /// <summary>
    /// Production-grade SMTP Email Transport Service using System.Net.Mail.SmtpClient.
    /// Supports STARTTLS (port 587), SSL (port 465), custom ports, authentication,
    /// HTML/Plain-text multi-part bodies, and comprehensive network diagnostics.
    /// </summary>
    public class SmtpEmailService : IEmailService
    {
        private readonly SmtpSettings _settings;
        private readonly ILogger<SmtpEmailService>? _logger;

        public SmtpEmailService(IOptions<SmtpSettings> options, ILogger<SmtpEmailService>? logger = null)
        {
            _settings = options?.Value ?? new SmtpSettings();
            _logger = logger;
        }

        public SmtpEmailService(SmtpSettings settings, ILogger<SmtpEmailService>? logger = null)
        {
            _settings = settings ?? new SmtpSettings();
            _logger = logger;
        }

        public SmtpSettings GetSettings()
        {
            // Return safe clone with password masked
            return new SmtpSettings
            {
                Host = _settings.Host,
                Port = _settings.Port,
                EnableSsl = _settings.EnableSsl,
                UserName = _settings.UserName,
                Password = string.IsNullOrWhiteSpace(_settings.Password) ? "(not configured)" : "••••••••",
                FromEmail = _settings.FromEmail,
                FromName = _settings.FromName,
                TimeoutSeconds = _settings.TimeoutSeconds
            };
        }

        public async Task<EmailResult> SendEmailAsync(
            string toEmail,
            string toName,
            string subject,
            string htmlBody,
            string plainTextBody)
        {
            var cleanedEmail = toEmail?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(cleanedEmail) || !IsValidEmailAddress(cleanedEmail))
            {
                return EmailResult.Failed(cleanedEmail, "Invalid or missing recipient email address.",
                    $"The specified recipient '{cleanedEmail}' does not conform to standard email syntax (user@domain.com).");
            }

            if (!_settings.IsValid())
            {
                return EmailResult.Failed(cleanedEmail, "SMTP Server is not configured.",
                    $"Missing SMTP Host or Sender Email in configuration. Host: '{_settings.Host}', Port: {_settings.Port}, From: '{_settings.FromEmail}'.",
                    "Please configure SmtpSettings in appsettings.json or provide valid SMTP credentials.");
            }

            // Pre-flight check: if targeting a cloud SMTP server (e.g. Gmail) without a password and without pickup mode
            if (!_settings.UsePickupDirectory &&
                !string.Equals(_settings.Host, "pickup", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(_settings.Host, "localhost", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(_settings.Password))
            {
                return EmailResult.Failed(
                    cleanedEmail,
                    $"SMTP Authentication Required: Password is empty for {_settings.Host}.",
                    $"The mail server '{_settings.Host}' requires an authenticated user account, but 'Password' in appsettings.json is blank.",
                    $"How to fix:\n1. Open App.API/appsettings.json\n2. Set your SMTP App Password under 'SmtpSettings' (for Gmail, generate a 16-character App Password at myaccount.google.com/apppasswords)\n3. Alternatively, set 'UsePickupDirectory': true in appsettings.json to save .eml files directly to disk for offline testing.");
            }

            try
            {
                using var client = CreateSmtpClient();
                using var message = new MailMessage
                {
                    From = new MailAddress(_settings.FromEmail, _settings.FromName),
                    Subject = subject ?? "CleanPro Customer Care Notification",
                    Priority = MailPriority.Normal
                };

                // Add To Recipient
                if (!string.IsNullOrWhiteSpace(toName))
                {
                    message.To.Add(new MailAddress(cleanedEmail, toName));
                }
                else
                {
                    message.To.Add(new MailAddress(cleanedEmail));
                }

                // Add Multi-part Alternate Views (Plain Text + HTML for optimal deliverability)
                if (!string.IsNullOrWhiteSpace(plainTextBody))
                {
                    var plainView = AlternateView.CreateAlternateViewFromString(plainTextBody, null, MediaTypeNames.Text.Plain);
                    message.AlternateViews.Add(plainView);
                }

                if (!string.IsNullOrWhiteSpace(htmlBody))
                {
                    var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, MediaTypeNames.Text.Html);
                    message.AlternateViews.Add(htmlView);
                    message.IsBodyHtml = true;
                }
                else
                {
                    message.Body = plainTextBody;
                    message.IsBodyHtml = false;
                }

                if (_settings.UsePickupDirectory || string.Equals(_settings.Host, "pickup", StringComparison.OrdinalIgnoreCase))
                {
                    _logger?.LogInformation("Generating RFC-822 email to pickup directory for {Recipient}", cleanedEmail);
                    await client.SendMailAsync(message);
                    return EmailResult.Success(cleanedEmail, $"Win-back email generated successfully as .eml file in pickup folder: '{_settings.PickupDirectory}'.");
                }

                _logger?.LogInformation("Dispatching SMTP email via {Host}:{Port} (SSL={EnableSsl}) to {Recipient}",
                    _settings.Host, _settings.Port, _settings.EnableSsl, cleanedEmail);

                await client.SendMailAsync(message);

                _logger?.LogInformation("SMTP email delivered successfully to {Recipient}", cleanedEmail);
                return EmailResult.Success(cleanedEmail, $"Win-back email successfully transmitted to {cleanedEmail} via SMTP ({_settings.Host}:{_settings.Port}).");
            }
            catch (SmtpFailedRecipientException frex)
            {
                var error = $"Recipient mailbox rejected by SMTP server: {frex.Message}";
                _logger?.LogWarning(frex, "SMTP recipient error: {Error}", error);
                return EmailResult.Failed(cleanedEmail, error,
                    $"The server at '{_settings.Host}' could not route email to '{cleanedEmail}'.",
                    $"SMTP StatusCode: {frex.StatusCode}. Verify that {cleanedEmail} exists and can receive incoming mail.");
            }
            catch (SmtpException smtpex)
            {
                var error = $"SMTP transmission failure: {smtpex.Message}";
                _logger?.LogError(smtpex, "SMTP error: {Error}", error);

                var diag = smtpex.Message.Contains("Authentication", StringComparison.OrdinalIgnoreCase) || smtpex.StatusCode == SmtpStatusCode.MustIssueStartTlsFirst
                    ? $"Authentication issue on {_settings.Host}:{_settings.Port}. Ensure your SMTP username ('{_settings.UserName}') and App Password in appsettings.json are valid."
                    : $"SMTP StatusCode: {smtpex.StatusCode}. Host: {_settings.Host}:{_settings.Port}.";

                return EmailResult.Failed(cleanedEmail, error,
                    "Verify your SMTP settings in App.API/appsettings.json.",
                    diag);
            }
            catch (SocketException sockex)
            {
                var error = $"Network connection to SMTP server '{_settings.Host}:{_settings.Port}' failed.";
                _logger?.LogError(sockex, "Socket error during SMTP: {Error}", error);
                return EmailResult.Failed(cleanedEmail, error, sockex.ToString(),
                    $"Socket ErrorCode: {sockex.ErrorCode}. Check server address, outbound port firewall rules, or VPN connectivity.");
            }
            catch (AuthenticationException authex)
            {
                var error = $"SSL/TLS handshake authentication failed with SMTP server '{_settings.Host}'.";
                _logger?.LogError(authex, "TLS authentication error: {Error}", error);
                return EmailResult.Failed(cleanedEmail, error, authex.ToString(),
                    "Verify TLS version compatibility or validate server certificate settings.");
            }
            catch (TimeoutException toex)
            {
                var error = $"SMTP connection timed out after {_settings.TimeoutSeconds} seconds.";
                _logger?.LogError(toex, "SMTP timeout error: {Error}", error);
                return EmailResult.Failed(cleanedEmail, error, toex.ToString(),
                    $"The SMTP host '{_settings.Host}:{_settings.Port}' did not respond within the allocated timeout.");
            }
            catch (Exception ex)
            {
                var error = $"Unexpected error during SMTP email delivery: {ex.Message}";
                _logger?.LogError(ex, "Unexpected error: {Error}", error);
                return EmailResult.Failed(cleanedEmail, error, ex.ToString(),
                    $"Exception Type: {ex.GetType().FullName}.");
            }
        }

        public async Task<EmailResult> SendWinBackEmailAsync(WinBackEmailRequest request)
        {
            if (request == null)
            {
                return EmailResult.Failed(string.Empty, "Request cannot be null.");
            }

            var subject = !string.IsNullOrWhiteSpace(request.Subject)
                ? request.Subject
                : $"We Miss You! Enjoy {request.DiscountPercentage}% Off Your Next CleanPro Service 🎁";

            var (htmlBody, plainTextBody) = WinBackEmailTemplate.Render(request, _settings.FromName);

            return await SendEmailAsync(
                request.RecipientEmail,
                request.RecipientName,
                subject,
                htmlBody,
                plainTextBody);
        }

        public async Task<WinBackBatchResult> SendBatchWinBackEmailsAsync(List<WinBackEmailRequest> requests)
        {
            var result = new WinBackBatchResult
            {
                TotalRequested = requests.Count
            };

            foreach (var req in requests)
            {
                var res = await SendWinBackEmailAsync(req);
                result.Results.Add(res);
                if (res.IsSuccess)
                {
                    result.TotalSent++;
                }
                else
                {
                    result.TotalFailed++;
                }

                // Minor courteous delay between SMTP dispatches to prevent rate-limit flooding
                await Task.Delay(100);
            }

            return result;
        }

        public async Task<EmailResult> TestConnectionAsync(string recipientEmail)
        {
            var testReq = new WinBackEmailRequest
            {
                RecipientEmail = recipientEmail,
                RecipientName = "CleanPro System Admin",
                Subject = "[SMTP Test] CleanPro Retention Integration Diagnostic",
                CampaignType = "SMTP Health Check",
                PromoCode = "TEST-VERIFIED-2026",
                DiscountPercentage = 10,
                DaysInactive = 45,
                LastServiceType = "Deep Cleaning",
                CustomMessage = "This is a real SMTP transport diagnostic verification sent from the CleanPro Retention Engine."
            };

            return await SendWinBackEmailAsync(testReq);
        }

        private SmtpClient CreateSmtpClient()
        {
            if (_settings.UsePickupDirectory || string.Equals(_settings.Host, "pickup", StringComparison.OrdinalIgnoreCase))
            {
                var pickupDir = Path.IsPathRooted(_settings.PickupDirectory)
                    ? _settings.PickupDirectory
                    : Path.Combine(AppContext.BaseDirectory, _settings.PickupDirectory);

                if (!Directory.Exists(pickupDir))
                {
                    Directory.CreateDirectory(pickupDir);
                }

                return new SmtpClient
                {
                    DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                    PickupDirectoryLocation = pickupDir
                };
            }

            var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Timeout = Math.Max(5000, _settings.TimeoutSeconds * 1000)
            };

            if (!string.IsNullOrWhiteSpace(_settings.UserName) && !string.IsNullOrWhiteSpace(_settings.Password))
            {
                client.Credentials = new NetworkCredential(_settings.UserName, _settings.Password);
            }

            return client;
        }

        private static bool IsValidEmailAddress(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email && Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            }
            catch
            {
                return false;
            }
        }
    }
}
