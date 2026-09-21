using System;

namespace App.Domain.Entities
{
    public class Customer
    {
        public int CustomerId { get; set; }
        public string CustomerType { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ContactInfo { get; set; } = string.Empty;
        public string ServiceLocation { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Domain property aliases
        public string FullName
        {
            get => CustomerName;
            set => CustomerName = value;
        }

        public string ContactDetails
        {
            get => ContactInfo;
            set => ContactInfo = value;
        }
    }
}