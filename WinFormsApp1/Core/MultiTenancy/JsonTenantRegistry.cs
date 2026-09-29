using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace App.WinForms.Core.MultiTenancy
{
    /// <summary>
    /// Local-development tenant registry backed by a JSON file.
    /// File is read once and cached; call Reload() if you edit it at runtime.
    ///
    /// NEVER use this in production — connection strings would sit in plaintext
    /// on disk next to the .exe. Use SqlTenantRegistry in prod.
    /// </summary>
    public sealed class JsonTenantRegistry : ITenantRegistry
    {
        private readonly string _filePath;
        private readonly object _lock = new();
        private Dictionary<string, TenantInfo>? _cache;

        public JsonTenantRegistry(string filePath)
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        }

        public Task<TenantInfo?> GetByIdAsync(string tenantId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                return Task.FromResult<TenantInfo?>(null);

            var map = EnsureLoaded();
            map.TryGetValue(tenantId.Trim().ToLowerInvariant(), out var tenant);
            return Task.FromResult(tenant);
        }

        public Task<IReadOnlyList<TenantInfo>> GetAllActiveAsync(CancellationToken ct = default)
        {
            var all = EnsureLoaded().Values
                .Where(t => t.IsActive)
                .OrderBy(t => t.TenantId)
                .ToList();
            return Task.FromResult<IReadOnlyList<TenantInfo>>(all);
        }

        public Task<IReadOnlyList<TenantInfo>> GetAllAsync(CancellationToken ct = default)
        {
            var all = EnsureLoaded().Values
                .OrderBy(t => t.TenantId)
                .ToList();
            return Task.FromResult<IReadOnlyList<TenantInfo>>(all);
        }

        /// <summary>Force re-read of tenants.json. Useful if you edit the file without restarting.</summary>
        public void Reload()
        {
            lock (_lock) { _cache = null; }
        }

        // ------------------------------------------------------------
        private Dictionary<string, TenantInfo> EnsureLoaded()
        {
            if (_cache != null) return _cache;

            lock (_lock)
            {
                if (_cache != null) return _cache;

                if (!File.Exists(_filePath))
                    throw new FileNotFoundException(
                        $"Tenant registry file not found: {_filePath}", _filePath);

                string json = File.ReadAllText(_filePath);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                var parsed = JsonSerializer.Deserialize<TenantRegistryFile>(json, options)
                             ?? throw new InvalidDataException("tenants.json is empty or malformed.");

                if (parsed.Tenants == null || parsed.Tenants.Count == 0)
                    throw new InvalidDataException("tenants.json contains no tenants.");

                var map = new Dictionary<string, TenantInfo>(StringComparer.OrdinalIgnoreCase);
                foreach (var t in parsed.Tenants)
                {
                    if (string.IsNullOrWhiteSpace(t.TenantId))
                        throw new InvalidDataException("A tenant entry has an empty TenantId.");

                    if (string.IsNullOrWhiteSpace(t.ConnectionString))
                        throw new InvalidDataException(
                            $"Tenant '{t.TenantId}' has no ConnectionString.");

                    string key = t.TenantId.Trim().ToLowerInvariant();
                    if (map.ContainsKey(key))
                        throw new InvalidDataException(
                            $"Duplicate TenantId in tenants.json: '{t.TenantId}'.");

                    map[key] = t;
                }

                _cache = map;
                return _cache;
            }
        }

        // Matches the top-level shape: { "tenants": [ ... ] }
        private sealed class TenantRegistryFile
        {
            public List<TenantInfo> Tenants { get; set; } = new();
        }
    }
}