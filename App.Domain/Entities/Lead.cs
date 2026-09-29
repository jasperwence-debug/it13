using System;
using System.Linq;
using App.Domain.Common;

namespace App.Domain.Entities
{
    public class Lead
    {
        public int LeadId { get; set; }

        // Normalized Name Fields (max length 50 each)
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string? Suffix { get; set; }

        /// <summary>
        /// Computed read-only property concatenating FirstName MiddleName LastName Suffix.
        /// Used for display, never for direct input.
        /// </summary>
        public string FullName => NameNormalizer.FormatFullName(FirstName, MiddleName, LastName, Suffix);

        /// <summary>
        /// Legacy LeadName property preserved for backward compatibility in display, queries, and filters.
        /// </summary>
        public string LeadName
        {
            get => !string.IsNullOrWhiteSpace(FullName) ? FullName : _legacyLeadName;
            set
            {
                _legacyLeadName = value ?? string.Empty;
                if (string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName) && !string.IsNullOrWhiteSpace(value))
                {
                    var (f, m, l) = NameNormalizer.SplitSingleString(value);
                    FirstName = f;
                    MiddleName = m;
                    LastName = l;
                }
            }
        }
        private string _legacyLeadName = string.Empty;

        public string ContactInfo { get; set; } = string.Empty;
        public string LeadSource { get; set; } = string.Empty;
        public string ServiceOfInterest { get; set; } = string.Empty;
        public string? InquiryDetails { get; set; }

        /// <summary>
        /// Lifecycle status: New, Contacted, Quoted, Won, Lost, Converted
        /// </summary>
        public string Status { get; set; } = "New";

        public decimal? QuotedPrice { get; set; }
        public string? ServiceAddress { get; set; }
        public string? LostReason { get; set; }

        /// <summary>
        /// Traceability link to Customer created/linked during conversion.
        /// </summary>
        public int? ConvertedCustomerId { get; set; }

        /// <summary>
        /// Timestamp when the lead was converted.
        /// </summary>
        public DateTime? ConvertedAt { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Staff member assigned to nurture and close this lead.
        /// </summary>
        public int? AssignedUserId { get; set; }
        public string? AssignedSalesStaff { get; set; }
    }
}