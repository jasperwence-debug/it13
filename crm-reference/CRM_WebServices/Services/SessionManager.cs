namespace CRM.winforms.Services
{
    public class SessionManager
    {
        public static SessionManager Current { get; } = new();

        public string Token { get; private set; } = "";
        public Guid UserId { get; private set; }
        public Guid TenantId { get; private set; }
        public string Email { get; private set; } = "";
        public string Name { get; private set; } = "";
        public string Role { get; private set; } = "";
        public DateTime ExpiresAt { get; private set; }

        public bool IsAuthenticated => !string.IsNullOrEmpty(Token) && DateTime.UtcNow < ExpiresAt;

        // ─── Role helpers ─────────────────────────────────────
        public bool IsSuperAdmin => RoleEquals("SuperAdmin");
        public bool IsAdmin => RoleEquals("Admin");
        public bool IsManager => RoleEquals("Manager");
        public bool IsSalesStaff => RoleEquals("SalesStaff");

        /// <summary>Admin or SuperAdmin</summary>
        public bool IsElevated => IsAdmin || IsSuperAdmin;

        /// <summary>Manager, Admin, or SuperAdmin</summary>
        public bool CanManageTeam => IsManager || IsElevated;

        /// <summary>True if user's role is in the provided list (case-insensitive).</summary>
        public bool HasRole(params string[] roles)
            => roles.Any(RoleEquals);

        private bool RoleEquals(string r)
            => string.Equals(Role, r, StringComparison.OrdinalIgnoreCase);

        // ─── Session lifecycle ────────────────────────────────
        public void SetSession(
            string token, Guid userId, Guid tenantId,
            string email, string name, string role,
            DateTime expiresAt)
        {
            Token = token;
            UserId = userId;
            TenantId = tenantId;
            Email = email;
            Name = name;
            Role = role;
            ExpiresAt = expiresAt;
        }

        public void Clear()
        {
            Token = "";
            UserId = Guid.Empty;
            TenantId = Guid.Empty;
            Email = "";
            Name = "";
            Role = "";
            ExpiresAt = DateTime.MinValue;
        }
    }
}