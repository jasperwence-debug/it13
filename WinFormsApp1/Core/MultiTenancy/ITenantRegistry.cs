using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace App.WinForms.Core.MultiTenancy
{
    /// <summary>
    /// Source of truth for "what tenants exist and how do I reach their DBs."
    ///
    /// Two implementations exist:
    ///   - JsonTenantRegistry  → local dev, reads tenants.json
    ///   - SqlTenantRegistry   → prod, reads Master DB (added later)
    ///
    /// The rest of the app must depend ONLY on this interface.
    /// </summary>
    public interface ITenantRegistry
    {
        /// <summary>Get a tenant by its stable id. Returns null if not found or inactive.</summary>
        Task<TenantInfo?> GetByIdAsync(string tenantId, CancellationToken ct = default);

        /// <summary>List all active tenants. Used by migration runner and admin tools.</summary>
        Task<IReadOnlyList<TenantInfo>> GetAllActiveAsync(CancellationToken ct = default);

        /// <summary>
        /// List ALL tenants including inactive ones.
        /// Used by admin/diagnostics — NOT by request-time resolution.
        /// </summary>
        Task<IReadOnlyList<TenantInfo>> GetAllAsync(CancellationToken ct = default);
    }
}