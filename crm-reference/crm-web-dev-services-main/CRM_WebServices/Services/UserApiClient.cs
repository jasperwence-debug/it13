using CRM.winforms.Models;

namespace CRM.winforms.Services
{
    public class UserApiClient : ApiClientBase
    {
        // ─── Assignable (any authenticated user) ─────────────
        public Task<List<UserSummary>?> ListAssignableAsync()
            => GetAsync<List<UserSummary>>("/api/users/assignable");

        // ─── Admin-only ──────────────────────────────────────
        public Task<List<UserListDto>?> ListAsync(
            string? search = null,
            string? role = null,
            bool includeInactive = false)
        {
            var url = $"/api/users?includeInactive={(includeInactive ? "true" : "false")}";
            if (!string.IsNullOrWhiteSpace(search)) url += $"&search={Uri.EscapeDataString(search)}";
            if (!string.IsNullOrWhiteSpace(role)) url += $"&role={Uri.EscapeDataString(role)}";
            return GetAsync<List<UserListDto>>(url);
        }

        public Task<UserDetailDto?> GetAsync(Guid id)
            => GetAsync<UserDetailDto>($"/api/users/{id}");

        public Task<UserListDto?> CreateAsync(CreateUserRequest req)
            => PostAsync<UserListDto>("/api/users", req);

        public Task<UserListDto?> UpdateAsync(Guid id, UpdateUserRequest req)
            => PutAsync<UserListDto>($"/api/users/{id}", req);

        public Task DeactivateAsync(Guid id)
            => PatchAsync($"/api/users/{id}/deactivate");

        public Task ReactivateAsync(Guid id)
            => PatchAsync($"/api/users/{id}/reactivate");

        public Task ChangePasswordAsync(Guid id, string newPassword)
            => PatchAsync($"/api/users/{id}/password", new { newPassword });
    }
}