using CRM.api.Contracts.Users;
using CRM.domain.Constants;
using CRM.domain.Entities;
using CRM.infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.api.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly ITenantDbProvider _tenantDb;
        private readonly ITenantContext _ctx;

        public UsersController(ITenantDbProvider tenantDb, ITenantContext ctx)
        {
            _tenantDb = tenantDb;
            _ctx = ctx;
        }

        // ─────────────────────────────────────────────────────
        // GET /api/users/assignable
        // Lightweight list of active users who can own records.
        // Any authenticated user can call this (needed for dropdowns).
        // ─────────────────────────────────────────────────────
        [HttpGet("assignable")]
        public async Task<IActionResult> Assignable()
        {
            var db = await _tenantDb.GetAsync();

            var users = await db.Users
                .AsNoTracking()
                .Where(u => u.IsActive)
                .OrderBy(u => u.Name)
                .Select(u => new UserSummaryDto(u.Id, u.Name, u.Email, u.Role))
                .ToListAsync();

            return Ok(users);
        }

        // ─────────────────────────────────────────────────────
        // GET /api/users?search=&role=&includeInactive=
        // Full user list — Admin+ only.
        // ─────────────────────────────────────────────────────
        [HttpGet]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
        public async Task<IActionResult> List(
            [FromQuery] string? search,
            [FromQuery] string? role,
            [FromQuery] bool includeInactive = false)
        {
            var db = await _tenantDb.GetAsync();

            var q = db.Users.AsNoTracking().AsQueryable();

            if (!includeInactive)
                q = q.Where(u => u.IsActive);

            if (!string.IsNullOrWhiteSpace(role))
                q = q.Where(u => u.Role == role);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                q = q.Where(u =>
                    u.Name.ToLower().Contains(s) ||
                    u.Email.ToLower().Contains(s));
            }

            var rows = await q
                .OrderBy(u => u.Role)
                .ThenBy(u => u.Name)
                .Select(u => new UserListDto(
                    u.Id, u.Name, u.Email, u.Role,
                    u.IsActive, u.LastLoginAt, u.CreatedAt))
                .ToListAsync();

            return Ok(rows);
        }

        // ─────────────────────────────────────────────────────
        // GET /api/users/{id}
        // Full detail with activity snapshot — Admin+ only.
        // ─────────────────────────────────────────────────────
        [HttpGet("{id:guid}")]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var db = await _tenantDb.GetAsync();

            var user = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            int assignedCustomers = await db.Customers
                .CountAsync(c => c.AssignedUserId == id);

            int assignedLeads = await db.Leads
                .CountAsync(l => l.AssignedUserId == id);

            int loggedActivities = await db.Activities
                .CountAsync(a => a.UserId == id);

            int openFollowUps = await db.FollowUps
                .CountAsync(f => f.UserId == id &&
                    f.Status != domain.Enums.FollowUpStatus.Completed &&
                    f.Status != domain.Enums.FollowUpStatus.Cancelled);

            return Ok(new UserDetailDto(
                user.Id, user.Name, user.Email, user.Role,
                user.IsActive, user.LastLoginAt,
                user.CreatedAt, user.UpdatedAt,
                assignedCustomers, assignedLeads,
                loggedActivities, openFollowUps));
        }

        // ─────────────────────────────────────────────────────
        // POST /api/users
        // Create a user inside the current tenant. Admin+ only.
        // ─────────────────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
        public async Task<IActionResult> Create(CreateUserRequest request)
        {
            var db = await _tenantDb.GetAsync();

            var normalized = request.Email.Trim().ToLowerInvariant();

            // Admin can't create SuperAdmins
            if (!string.Equals(_ctx.Role, Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(request.Role, Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Only SuperAdmin can create SuperAdmin accounts." });
            }

            // Validate role is one of the known roles
            if (!IsValidRole(request.Role))
                return BadRequest(new { message = $"Invalid role '{request.Role}'." });

            bool exists = await db.Users.AnyAsync(u => u.Email == normalized);
            if (exists)
                return Conflict(new { message = $"A user with email '{normalized}' already exists." });

            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Email = normalized,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = request.Role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById),
                new { id = user.Id },
                new UserListDto(user.Id, user.Name, user.Email, user.Role,
                    user.IsActive, user.LastLoginAt, user.CreatedAt));
        }

        // ─────────────────────────────────────────────────────
        // PUT /api/users/{id}
        // Update name, email, role. Admin+ only.
        // ─────────────────────────────────────────────────────
        [HttpPut("{id:guid}")]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
        public async Task<IActionResult> Update(Guid id, UpdateUserRequest request)
        {
            var db = await _tenantDb.GetAsync();

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            // Only SuperAdmin can modify another SuperAdmin
            if (string.Equals(user.Role, Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(_ctx.Role, Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            // Only SuperAdmin can promote someone to SuperAdmin
            if (string.Equals(request.Role, Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(_ctx.Role, Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Only SuperAdmin can assign the SuperAdmin role." });
            }

            if (!IsValidRole(request.Role))
                return BadRequest(new { message = $"Invalid role '{request.Role}'." });

            var normalized = request.Email.Trim().ToLowerInvariant();

            // Email uniqueness (excluding self)
            bool emailTaken = await db.Users
                .AnyAsync(u => u.Email == normalized && u.Id != id);
            if (emailTaken)
                return Conflict(new { message = $"A user with email '{normalized}' already exists." });

            user.Name = request.Name.Trim();
            user.Email = normalized;
            user.Role = request.Role;
            user.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Ok(new UserListDto(user.Id, user.Name, user.Email, user.Role,
                user.IsActive, user.LastLoginAt, user.CreatedAt));
        }

        // ─────────────────────────────────────────────────────
        // PATCH /api/users/{id}/deactivate
        // ─────────────────────────────────────────────────────
        [HttpPatch("{id:guid}/deactivate")]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            var db = await _tenantDb.GetAsync();

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            // Can't deactivate yourself
            if (user.Id == _ctx.UserId)
                return BadRequest(new { message = "You cannot deactivate your own account." });

            // Only SuperAdmin can deactivate SuperAdmin
            if (string.Equals(user.Role, Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(_ctx.Role, Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return NoContent();
        }

        // ─────────────────────────────────────────────────────
        // PATCH /api/users/{id}/reactivate
        // ─────────────────────────────────────────────────────
        [HttpPatch("{id:guid}/reactivate")]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
        public async Task<IActionResult> Reactivate(Guid id)
        {
            var db = await _tenantDb.GetAsync();

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return NoContent();
        }

        // ─────────────────────────────────────────────────────
        // PATCH /api/users/{id}/password
        // Admin+ can reset anyone's password. User can reset own.
        // ─────────────────────────────────────────────────────
        [HttpPatch("{id:guid}/password")]
        public async Task<IActionResult> ChangePassword(Guid id, ChangePasswordRequest request)
        {
            var db = await _tenantDb.GetAsync();

            // Only Admin+ can change others' passwords; users can change own
            bool isSelf = _ctx.UserId == id;
            bool isAdmin = string.Equals(_ctx.Role, Roles.Admin, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(_ctx.Role, Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase);

            if (!isSelf && !isAdmin)
                return Forbid();

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return NoContent();
        }

        // ─── Helpers ──────────────────────────────────────────
        private static bool IsValidRole(string role)
            => string.Equals(role, Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, Roles.Admin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, Roles.Manager, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, Roles.SalesStaff, StringComparison.OrdinalIgnoreCase);
    }
}