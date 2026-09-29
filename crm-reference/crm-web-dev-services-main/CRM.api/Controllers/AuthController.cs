using CRM.api.Contracts.Auth;
using CRM.api.Services;
using CRM.domain.Entities;
using CRM.infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;
        private readonly ITokenService _tokens;
        private readonly ITenantContext _context;
        private readonly IConfiguration _config;

        public AuthController(
            IAuthService auth,
            ITokenService tokens,
            ITenantContext context,
            IConfiguration config)
        {
            _auth = auth;
            _tokens = tokens;
            _context = context;
            _config = config;
        }

        // ────────────────────────────────────────────────────────
        // POST /api/auth/register
        // Creates a user inside the CURRENT tenant's database.
        // NOTE: This endpoint requires an admin token in production.
        // ────────────────────────────────────────────────────────
        [HttpPost("register")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            try
            {
                var user = await _auth.RegisterAsync(
                    request.Name,
                    request.Email,
                    request.Password,
                    request.Role);

                return Ok(new
                {
                    user.Id,
                    user.Name,
                    user.Email,
                    user.Role
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // ────────────────────────────────────────────────────────
        // POST /api/auth/login
        // Accepts email+password, returns a JWT.
        // tenantId must be supplied (until we add subdomain/header routing)
        // ────────────────────────────────────────────────────────
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request.TenantId == Guid.Empty)
                return BadRequest(new { message = "tenantId is required." });

            var user = await _auth.ValidateCredentialsAsync(
                request.Email, request.Password);

            if (user == null)
                return Unauthorized(new { message = "Invalid credentials." });

            var token = _tokens.GenerateToken(user, request.TenantId);
            var expiryMinutes = int.Parse(
                _config["Jwt:ExpiryMinutes"] ?? "60");

            return Ok(new AuthResponse(
                token,
                DateTime.UtcNow.AddMinutes(expiryMinutes),
                user.Name,
                user.Email,
                user.Role));
        }

        // ────────────────────────────────────────────────────────
        // GET /api/auth/me
        // Quick test that JWT works.
        // ────────────────────────────────────────────────────────
        [HttpGet("me")]
        [Authorize]
        public IActionResult Me()
        {
            return Ok(new
            {
                userId = _context.UserId,
                tenantId = _context.TenantId,
                email = _context.Email,
                role = _context.Role
            });
        }
    }
}