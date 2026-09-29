using System;

namespace App.Domain.Models.Email
{
    /// <summary>
    /// Outcome of an SMTP email delivery attempt.
    /// </summary>
    public class EmailResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string RecipientEmail { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? ErrorDetails { get; set; }
        public string? Diagnostics { get; set; }

        public static EmailResult Success(string recipientEmail, string message = "Email dispatched successfully via SMTP.")
        {
            return new EmailResult
            {
                IsSuccess = true,
                RecipientEmail = recipientEmail,
                Message = message,
                Timestamp = DateTime.UtcNow
            };
        }

        public static EmailResult Failed(string recipientEmail, string error, string? details = null, string? diagnostics = null)
        {
            return new EmailResult
            {
                IsSuccess = false,
                RecipientEmail = recipientEmail,
                Message = error,
                ErrorDetails = details,
                Diagnostics = diagnostics,
                Timestamp = DateTime.UtcNow
            };
        }
    }
}
