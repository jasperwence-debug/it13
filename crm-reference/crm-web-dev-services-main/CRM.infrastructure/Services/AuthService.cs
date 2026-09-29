using CRM.domain.Entities;
using CRM.infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly TenantCrmDbContext _db;

        public AuthService(TenantCrmDbContext db)
        {
            _db = db;
        }

        public async Task<User> RegisterAsync(
            string name,
            string email,
            string password,
            string role)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            var exists = await _db.Users
                .AnyAsync(u => u.Email == normalizedEmail);

            if (exists)
                throw new InvalidOperationException(
                    $"A user with email '{normalizedEmail}' already exists.");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = name.Trim(),
                Email = normalizedEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return user;
        }

        public async Task<User?> ValidateCredentialsAsync(
            string email,
            string password)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            if (user == null || !user.IsActive)
                return null;

            var valid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            if (!valid)
                return null;

            user.LastLoginAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return user;
        }
    }
}