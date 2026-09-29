using CRM.winforms.Services;

namespace CRM.winforms.Helpers
{
    /// <summary>
    /// Client-side mirror of TeamScopeHelper. Uses SessionManager for role.
    /// </summary>
    public static class RoleHelper
    {
        public static bool IsSuperAdmin => SessionManager.Current.IsSuperAdmin;
        public static bool IsAdmin => SessionManager.Current.IsAdmin;
        public static bool IsManager => SessionManager.Current.IsManager;
        public static bool IsSalesStaff => SessionManager.Current.IsSalesStaff;

        public static bool SeesWholeTenant => SessionManager.Current.CanManageTeam;

        public static bool CanAssign => SessionManager.Current.CanManageTeam;

        public static bool CanDelete => SessionManager.Current.IsElevated;
    }
}