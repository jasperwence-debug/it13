using System;
using System.Linq;
using App.Domain.Common;

namespace App.Domain.Entities
{
    public class Customer
    {
        public int CustomerId { get; set; }
        public string CustomerType { get; set; } = string.Empty;

        // Normalized Name Fields (max length 50 each)
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string? Suffix { get; set; }

        public string ContactInfo { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string ServiceLocation { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Traceability back to the originating Lead if this customer was converted from an inquiry.
        /// </summary>
        public int? LeadId { get; set; }

        /// <summary>
        /// Staff member assigned as the relationship account manager for this customer.
        /// </summary>
        public int? AssignedUserId { get; set; }
        public string? AssignedSalesStaff { get; set; }

        /// <summary>
        /// Computed read-only property concatenating FirstName MiddleName LastName Suffix.
        /// Used for display, never for direct input.
        /// </summary>
        public string FullName => NameNormalizer.FormatFullName(FirstName, MiddleName, LastName, Suffix);

        /// <summary>
        /// Legacy CustomerName property preserved for backward compatibility in display, queries, and filters.
        /// </summary>
        public string CustomerName
        {
            get => !string.IsNullOrWhiteSpace(FullName) ? FullName : _legacyCustomerName;
            set
            {
                _legacyCustomerName = value ?? string.Empty;
                if (string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName) && !string.IsNullOrWhiteSpace(value))
                {
                    var (f, m, l) = NameNormalizer.SplitSingleString(value);
                    FirstName = f;
                    MiddleName = m;
                    LastName = l;
                }
            }
        }
        private string _legacyCustomerName = string.Empty;

        // Domain property aliases
        public int Id
        {
            get => CustomerId;
            set => CustomerId = value;
        }

        public string Type
        {
            get => CustomerType;
            set => CustomerType = value;
        }

        public string Location
        {
            get => ServiceLocation;
            set => ServiceLocation = value;
        }

        public string ContactDetails
        {
            get => ContactInfo;
            set => ContactInfo = value;
        }

        public string Phone
        {
            get => ContactInfo;
            set => ContactInfo = value;
        }
    }
}