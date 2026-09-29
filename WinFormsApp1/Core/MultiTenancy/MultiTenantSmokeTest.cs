using System;
using System.Threading.Tasks;

namespace App.WinForms.Core.MultiTenancy
{
    /// <summary>
    /// Temporary harness. Call RunAsync() once from Program.Main or
    /// a debug menu item to verify the tenant foundation is wired up.
    /// Delete this file once real integration is done.
    /// </summary>
    public static class MultiTenantSmokeTest
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("=== Multi-Tenant Smoke Test ===");

            var registry = TenantRegistryFactory.Create();
            Console.WriteLine($"Registry: {registry.GetType().Name}");

            var all = await registry.GetAllAsync();
            Console.WriteLine($"Total tenants : {all.Count}");

            var active = await registry.GetAllActiveAsync();
            Console.WriteLine($"Active tenants: {active.Count}");

            foreach (var t in all)
            {
                string status = t.IsActive ? "ACTIVE  " : "DISABLED";
                Console.WriteLine($"  [{status}] {t.TenantId,-15} {t.DisplayName}");
            }

            // Lookup test
            var tenantC = await registry.GetByIdAsync("tenantC");
            Console.WriteLine($"\nLookup 'tenantC': {tenantC?.DisplayName ?? "<null>"}");

            // Case-insensitivity test
            var tenantCLower = await registry.GetByIdAsync("TENANTC");
            Console.WriteLine($"Lookup 'TENANTC': {tenantCLower?.DisplayName ?? "<null>"}");

            // Context test
            if (tenantC != null)
            {
                TenantContext.SetGlobal(tenantC);
                Console.WriteLine($"\nTenantContext.Current.TenantId = {TenantContext.Current?.TenantId}");
                Console.WriteLine($"TenantContext.RequireTenantId() = {TenantContext.RequireTenantId()}");

                TenantContext.Clear();
                Console.WriteLine($"After Clear(), HasTenant = {TenantContext.HasTenant}");
            }

            Console.WriteLine("=== Smoke test complete ===");
        }
    }
}