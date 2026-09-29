using CRM.winforms.Models;

namespace CRM.winforms.Services
{
    public class AuthApiClient : ApiClientBase
    {
        public async Task<AuthResponse> LoginAsync(string email, string password, Guid tenantId)
        {
            var url = $"/api/auth/login";
            var body = new LoginRequest
            {
                Email = email,
                Password = password,
                TenantId = tenantId
            };

            var resp = await PostAsync<AuthResponse>(url, body)
                       ?? throw new ApiException("Empty response from login.", 0);

            // Set token temporarily so /me works
            SessionManager.Current.SetSession(
                resp.Token, Guid.Empty, tenantId,
                resp.Email, resp.Name, resp.Role, resp.ExpiresAt);

            // Fetch userId from /me
            var me = await GetAsync<MeResponse>("/api/auth/me");
            if (me?.UserId != null)
            {
                SessionManager.Current.SetSession(
                    resp.Token, me.UserId.Value, tenantId,
                    resp.Email, resp.Name, resp.Role, resp.ExpiresAt);
            }

            return resp;
        }
    }

    public class MeResponse
    {
        public Guid? UserId { get; set; }
        public Guid? TenantId { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
    }
}