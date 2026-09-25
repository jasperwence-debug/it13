using System;
using App.Domain.Entities;
using App.WinForms.Core;

namespace App.WinForms
{
    /// <summary>
    /// Manages the authenticated user session state across the WinForms application.
    /// Provides null-safe session checks and role evaluation using strongly-typed role constants.
    /// </summary>
    public static class SessionManager
    {
        public static User? CurrentUser { get; private set; }

        public static bool IsAuthenticated => CurrentUser != null;

        public static string CurrentRole => CurrentUser?.Role ?? string.Empty;

        public static void Login(User user)
        {
            CurrentUser = user ?? throw new ArgumentNullException(nameof(user));
        }

        public static void Logout()
        {
            CurrentUser = null;
        }

        public static bool IsInRole(string role)
        {
            return string.Equals(CurrentUser?.Role, role, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsSuperAdmin => IsInRole(Roles.SuperAdmin);
        public static bool IsAdmin => IsInRole(Roles.Admin);
        public static bool IsManager => IsInRole(Roles.Manager);
        public static bool IsSalesStaff => IsInRole(Roles.SalesStaff);

        // ============================================================
        // SaaS Multi-Tenant Support & Maintenance Mode (Super Admin)
        // ============================================================
        public static bool IsMaintenanceMode { get; private set; }
        public static Company? MaintenanceTargetCompany { get; private set; }

        public static void EnterMaintenanceMode(Company company)
        {
            if (!IsSuperAdmin)
                throw new InvalidOperationException("Only Super Administrators can activate tenant maintenance access.");

            IsMaintenanceMode = true;
            MaintenanceTargetCompany = company ?? throw new ArgumentNullException(nameof(company));
        }

        public static void ExitMaintenanceMode()
        {
            IsMaintenanceMode = false;
            MaintenanceTargetCompany = null;
        }
    }
}
