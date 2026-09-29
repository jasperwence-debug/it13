using CRM.domain.Constants;

namespace CRM.infrastructure.Services
{
    /// <summary>
    /// Centralizes "who is a Manager allowed to see?"
    /// 
    /// Current rules (v1):
    ///   - SuperAdmin / Admin → no scoping (whole tenant)
    ///   - Manager             → no scoping (whole tenant, becomes team once we add ManagerId)
    ///   - SalesStaff          → scoped to own UserId
    ///   - Unknown             → scoped to own UserId (safe default)
    /// </summary>
    public static class TeamScopeHelper
    {
        /// <summary>
        /// Returns true if the caller should see ALL records in the tenant.
        /// </summary>
        public static bool SeesWholeTenant(string? role)
            => string.Equals(role, Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, Roles.Admin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, Roles.Manager, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Returns true if the caller should be scoped to their own records only.
        /// </summary>
        public static bool SeesOnlyOwn(string? role)
            => !SeesWholeTenant(role);

        /// <summary>
        /// True when the caller can assign/reassign records to other users.
        /// </summary>
        public static bool CanAssign(string? role)
            => string.Equals(role, Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, Roles.Admin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, Roles.Manager, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// True when the caller can delete records.
        /// </summary>
        public static bool CanDelete(string? role)
            => string.Equals(role, Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, Roles.Admin, StringComparison.OrdinalIgnoreCase);
    }
}