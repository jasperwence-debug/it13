namespace App.Domain.Models.Email
{
    /// <summary>
    /// Configuration settings for the SMTP email transport.
    /// Loaded from appsettings.json "SmtpSettings" section.
    /// </summary>
    public class SmtpSettings
    {
        /// <summary>
        /// SMTP Server Hostname or IP address (e.g. "smtp.gmail.com", "smtp.office365.com", "localhost")
        /// </summary>
        public string Host { get; set; } = "smtp.gmail.com";

        /// <summary>
        /// SMTP Port: 587 (STARTTLS), 465 (SSL/TLS), or 25 (Standard)
        /// </summary>
        public int Port { get; set; } = 587;

        /// <summary>
        /// Whether to enable TLS/SSL transport encryption
        /// </summary>
        public bool EnableSsl { get; set; } = true;

        /// <summary>
        /// Authenticated SMTP username or mailbox address
        /// </summary>
        public string UserName { get; set; } = "notifications@cleanpro-crm.ph";

        /// <summary>
        /// Authenticated SMTP password or App Password
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Sender email address appearing in the From header
        /// </summary>
        public string FromEmail { get; set; } = "notifications@cleanpro-crm.ph";

        /// <summary>
        /// Friendly sender display name
        /// </summary>
        public string FromName { get; set; } = "CleanPro Operations & Customer Care";

        /// <summary>
        /// Network connection timeout in seconds (default 15)
        /// </summary>
        public int TimeoutSeconds { get; set; } = 15;

        /// <summary>
        /// When true, writes genuine RFC-822 .eml files to PickupDirectory instead of connecting over network.
        /// Allows instant testing without needing real external email credentials.
        /// </summary>
        public bool UsePickupDirectory { get; set; } = false;

        /// <summary>
        /// Destination directory for pickup .eml files when UsePickupDirectory is true.
        /// </summary>
        public string PickupDirectory { get; set; } = "./mail_pickup";

        /// <summary>
        /// Returns true if minimal required configuration fields are provided.
        /// </summary>
        public bool IsValid() =>
            UsePickupDirectory || (
                !string.IsNullOrWhiteSpace(Host) &&
                Port > 0 &&
                !string.IsNullOrWhiteSpace(FromEmail));
    }
}
