using System;
using System.Threading;

namespace App.WinForms.Core.MultiTenancy
{
    /// <summary>
    /// Ambient runtime holder for "which tenant is THIS operation running under."
    ///
    /// Backed by AsyncLocal so it flows safely across await boundaries
    /// without leaking between concurrent operations on the same thread.
    ///
    /// Usage:
    ///     using (TenantContext.Set(tenant))
    ///     {
    ///         // inside this block, TenantContext.Current == tenant
    ///     }
    ///
    /// Or, on the UI thread at login time:
    ///     TenantContext.SetGlobal(tenant);   // applies to the whole WinForms session
    /// </summary>
    public interface ITenantContext
    {
        TenantInfo? Current { get; }
        bool HasTenant { get; }
        string RequireTenantId();
    }

    public static class TenantContext
    {
        private static readonly AsyncLocal<TenantInfo?> _ambient = new();

        /// <summary>
        /// Set for the current async flow. Disposing restores the previous value.
        /// Use this on the server for per-request scoping.
        /// </summary>
        public static IDisposable Set(TenantInfo tenant)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));
            var previous = _ambient.Value;
            _ambient.Value = tenant;
            return new Restore(previous);
        }

        /// <summary>
        /// Set for the entire WinForms session. There is typically one user,
        /// one tenant per running app instance — this is the common case.
        /// </summary>
        public static void SetGlobal(TenantInfo tenant)
        {
            _ambient.Value = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        /// <summary>Clear the current tenant (e.g., on logout).</summary>
        public static void Clear() => _ambient.Value = null;

        /// <summary>The tenant bound to the current operation, or null.</summary>
        public static TenantInfo? Current => _ambient.Value;

        public static bool HasTenant => _ambient.Value != null;

        /// <summary>Throws if no tenant is set. Use this to fail loudly instead of silently hitting the wrong DB.</summary>
        public static string RequireTenantId()
        {
            var t = _ambient.Value
                ?? throw new InvalidOperationException(
                    "No tenant is bound to the current operation. " +
                    "Did you forget to call TenantContext.SetGlobal() after login?");
            return t.TenantId;
        }

        // ------------------------------------------------------------
        private sealed class Restore : IDisposable
        {
            private readonly TenantInfo? _previous;
            public Restore(TenantInfo? previous) { _previous = previous; }
            public void Dispose() => _ambient.Value = _previous;
        }
    }
}