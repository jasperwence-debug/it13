using System;
using System.Collections.Generic;

namespace CRM.domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty; // Store hash, not plain text
        public string Role { get; set; } = "SalesStaff"; // Admin, Manager, SalesStaff

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        // One User can manage many Customers
        public ICollection<Customer> Customers { get; set; } = new List<Customer>();
        // One User can manage many Leads
        public ICollection<Lead> Leads { get; set; } = new List<Lead>();
    }
}