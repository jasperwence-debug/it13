namespace App.Domain.Enums
{
    /// <summary>
    /// Lifecycle status definitions for work orders and service bookings.
    /// </summary>
    public enum BookingStatus
    {
        Requested,
        Scheduled,
        InProgress,
        Completed,
        Cancelled,
        Rescheduled
    }

    /// <summary>
    /// Utility methods for BookingStatus parsing, normalization, and legacy compatibility.
    /// </summary>
    public static class BookingStatusHelper
    {
        /// <summary>
        /// Normalizes raw string status from the database or API.
        /// Maps legacy "Pending" rows directly to BookingStatus.Scheduled.
        /// </summary>
        public static BookingStatus FromString(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return BookingStatus.Requested;

            var trimmed = status.Trim();

            // Legacy backward-compatibility mapping
            if (string.Equals(trimmed, "Pending", System.StringComparison.OrdinalIgnoreCase))
                return BookingStatus.Scheduled;

            if (System.Enum.TryParse<BookingStatus>(trimmed, true, out var result))
                return result;

            // Handle space variations: "In Progress" -> InProgress
            if (string.Equals(trimmed, "In Progress", System.StringComparison.OrdinalIgnoreCase))
                return BookingStatus.InProgress;

            return BookingStatus.Requested;
        }

        /// <summary>
        /// Formats enum to user-friendly display text.
        /// </summary>
        public static string ToDisplayText(this BookingStatus status) => status switch
        {
            BookingStatus.InProgress => "In Progress",
            _ => status.ToString()
        };
    }
}
