using System;

namespace App.WinForms.Core.MultiTenancy
{
    /// <summary>
    /// Immutable descriptor of a single tenant.
    /// This is the ONLY shape the rest of the app should depend on —
    /// whether it came from tenants.json (local) or the Master DB (prod).
    /// </summary>
    public sealed class TenantInfo
    {
        /// <summary>Stable identifier. Used in headers, JWTs, DB names. Lowercase, no spaces.</summary>
        public string TenantId { get; init; } = string.Empty;

        /// <summary>Human-readable display name.</summary>
        public string DisplayName { get; init; } = string.Empty;

        /// <summary>
        /// Connection string to THIS tenant's database.
        /// Never logged. Never shown in UI. Encrypted at rest in prod.
        /// </summary>
        public string ConnectionString { get; init; } = string.Empty;

        /// <summary>Optional: schema version expected by this tenant (for migration guard).</summary>
        public string? ExpectedSchemaVersion { get; init; }

        /// <summary>Optional: is this tenant active? Disabled tenants refuse login.</summary>
        public bool IsActive { get; init; } = true;

        /// <summary>Optional: free-form metadata (branding, locale, timezone, etc.).</summary>
        public string? Notes { get; init; }

        public override string ToString() => $"{TenantId} ({DisplayName})";
    }
}