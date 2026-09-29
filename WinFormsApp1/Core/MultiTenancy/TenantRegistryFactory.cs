using System;
using System.IO;

namespace App.WinForms.Core.MultiTenancy
{
    /// <summary>
    /// Chooses the registry backend based on a single config flag.
    ///
    /// Reads from:
    ///   1. Environment variable  APP_TENANT_MODE  ("json" | "sql")
    ///   2. Falls back to "json" for local dev
    ///
    /// When you move to MonsterASP, set APP_TENANT_MODE=sql and
    /// supply a Master DB connection string — no code changes.
    /// </summary>
    public static class TenantRegistryFactory
    {
        public static ITenantRegistry Create()
        {
            string mode = (Environment.GetEnvironmentVariable("APP_TENANT_MODE") ?? "json")
                          .Trim().ToLowerInvariant();

            switch (mode)
            {
                case "json":
                    string path = Path.Combine(AppContext.BaseDirectory, "tenants.json");
                    return new JsonTenantRegistry(path);

                case "sql":
                    // Not yet implemented — placeholder for Step X.
                    // When implemented, this reads from Master DB.
                    throw new NotImplementedException(
                        "SqlTenantRegistry is not implemented yet. " +
                        "Set APP_TENANT_MODE=json for local development.");

                default:
                    throw new InvalidOperationException(
                        $"Unknown APP_TENANT_MODE='{mode}'. Use 'json' or 'sql'.");
            }
        }
    }
}