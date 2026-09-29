using System.Collections.Generic;
using System.Threading.Tasks;
using App.Domain.Models.Email;

namespace App.Infrastructure.Services.Email
{
    /// <summary>
    /// Contract for SMTP email dispatch and retention win-back messaging.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends a generic email via SMTP with HTML and plain text bodies.
        /// </summary>
        Task<EmailResult> SendEmailAsync(string toEmail, string toName, string subject, string htmlBody, string plainTextBody);

        /// <summary>
        /// Renders and sends a personalized win-back re-engagement email to a specific customer.
        /// </summary>
        Task<EmailResult> SendWinBackEmailAsync(WinBackEmailRequest request);

        /// <summary>
        /// Dispatches win-back emails to a batch of customer requests.
        /// </summary>
        Task<WinBackBatchResult> SendBatchWinBackEmailsAsync(List<WinBackEmailRequest> requests);

        /// <summary>
        /// Gets current active SMTP configuration (with credentials masked).
        /// </summary>
        SmtpSettings GetSettings();

        /// <summary>
        /// Tests the SMTP connection by verifying credentials and connectivity.
        /// </summary>
        Task<EmailResult> TestConnectionAsync(string recipientEmail);
    }
}
