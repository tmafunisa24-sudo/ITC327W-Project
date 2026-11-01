// Controllers/Api/AuthApiController.cs

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceHub_IT.DTOs.Api;
using ServiceHub_IT.Interfaces;
using ServiceHub_IT.Utilities;
using ServiceHub_IT.Models;

namespace ServiceHub_IT.Controllers.Api;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthApiController : ControllerBase
{
    private readonly ISupabaseService _supabaseService;
    private readonly ILogger<AuthApiController> _logger;

    public AuthApiController(
        ISupabaseService supabaseService,
        ILogger<AuthApiController> logger)
    {
        _supabaseService = supabaseService;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────
    // POST /api/auth/login
    // Body: { email, password, rememberMe }
    // Returns: { user: {...} } or 401/403 with { error: "..." }
    // Sets the ServiceHubITAuth cookie on success.
    // ────────────────────────────────────────────────────────
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] ApiLoginRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ApiErrorDto
            {
                Error = "Email and password are required."
            });
        }

        if (!_supabaseService.IsConfigured)
        {
            return StatusCode(503, new ApiErrorDto
            {
                Error = "Authentication is not configured."
            });
        }

        try
        {
            // 1. Authenticate against Supabase (same as your MVC controller)
            var signIn = await _supabaseService.SignInAsync(
                request.Email, request.Password, cancellationToken);

            if (!signIn.IsSuccess || string.IsNullOrWhiteSpace(signIn.AccessToken))
            {
                return Unauthorized(new ApiErrorDto
                {
                    Error = signIn.ErrorMessage ?? "Invalid email or password."
                });
            }

            // 2. Load profile
            var userId = await _supabaseService.GetUserIdFromAccessTokenAsync(
                signIn.AccessToken, cancellationToken);

            if (userId is null)
            {
                return Unauthorized(new ApiErrorDto
                {
                    Error = "Unable to load your profile. Please try again."
                });
            }

            var profile = await _supabaseService.GetProfileByIdAsync(
                userId.Value, signIn.AccessToken, cancellationToken);

            if (profile is null)
            {
                return Unauthorized(new ApiErrorDto
                {
                    Error = "Profile not found."
                });
            }

            // 3. Block disabled accounts
            if (string.Equals(profile.Status, "Disabled", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(403, new ApiErrorDto
                {
                    Error = "Your account has been disabled. Please contact an administrator."
                });
            }

            // 4. Resolve role
            var rawRole = await _supabaseService.GetRoleForEmailAsync(
                request.Email, signIn.AccessToken, cancellationToken);

            var role = NormalizeRole(rawRole);

            if (string.Equals(role, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(403, new ApiErrorDto
                {
                    Error = "This account is awaiting administrator approval."
                });
            }

            if (string.IsNullOrWhiteSpace(role))
            {
                return StatusCode(403, new ApiErrorDto
                {
                    Error = "Your account has an unsupported role. Please contact an administrator."
                });
            }

            // 5. Sign in with the SAME cookie scheme your MVC uses
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, profile.Id.ToString()),
                new("profile_id", profile.Id.ToString()),
                new(ClaimTypes.Name, request.Email),
                new(ClaimTypes.Email, request.Email),
                new(ClaimTypes.Role, role),
                new("supabase_access_token", signIn.AccessToken),
            };

            var identity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = request.RememberMe,
                    ExpiresUtc = request.RememberMe
                        ? DateTimeOffset.UtcNow.AddDays(30)
                        : DateTimeOffset.UtcNow.AddDays(7)
                });

            _logger.LogInformation(
                "API login succeeded for {Email}, role={Role}, rememberMe={RememberMe}",
                request.Email, role, request.RememberMe);

            return Ok(new ApiAuthResponse
            {
                User = ApiUserDto.FromProfile(profile, role),
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API login failed for {Email}", request.Email);
            return StatusCode(500, new ApiErrorDto
            {
                Error = "Unable to sign in right now. Please try again later."
            });
        }
    }

        // ────────────────────────────────────────────────────────
    // POST /api/auth/register
    // Body: { email, password, fullName, phone? }
    // Creates a Supabase user. Supabase sends a confirmation email
    // if email confirmation is enabled on the project.
    // Returns: { success: true, requiresConfirmation: true, email }
    //          or 400 with { error: "..." }
    // ────────────────────────────────────────────────────────
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        [FromBody] ApiRegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.FullName))
        {
            return BadRequest(new ApiErrorDto
            {
                Error = "Email, password, and full name are required."
            });
        }

        if (!_supabaseService.IsConfigured)
        {
            return StatusCode(503, new ApiErrorDto
            {
                Error = "Authentication is not configured."
            });
        }

        try
        {
            // 1. Create the Supabase auth user.
            //    SignUpAsync already sends the confirmation email via Supabase.
            var signUp = await _supabaseService.SignUpAsync(
                request.Email, request.Password, null, cancellationToken);

            if (!signUp.IsSuccess)
            {
                return BadRequest(new ApiErrorDto
                {
                    Error = signUp.ErrorMessage ?? "Unable to create your account."
                });
            }

            _logger.LogInformation(
                "API register succeeded for {Email}", request.Email);

            // 2. Supabase sends the confirmation email automatically.
            //    The user cannot log in until they click the link.
            return Ok(new ApiRegisterResponse
            {
                Success = true,
                RequiresConfirmation = false,
                Email = request.Email,
                Message = "Account created. You can now sign in."
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API register failed for {Email}", request.Email);
            return StatusCode(500, new ApiErrorDto
            {
                Error = "Unable to create your account right now. Please try again later."
            });
        }
    }

    // ────────────────────────────────────────────────────────
    // POST /api/auth/forgot-password
    // Body: { email }
    // Sends a Supabase password-reset email if the account exists.
    // Always returns 200 to avoid leaking whether an email is registered.
    // ────────────────────────────────────────────────────────
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ApiForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new ApiErrorDto
            {
                Error = "Email is required."
            });
        }

        if (!_supabaseService.IsConfigured)
        {
            return StatusCode(503, new ApiErrorDto
            {
                Error = "Authentication is not configured."
            });
        }

        try
        {
            // Ask Supabase to send the reset email. It's a no-op if the
            // email isn't registered — that's intentional (don't leak).
            var resetUrl = Url.Action(
                "ResetPassword", "Auth", null, Request.Scheme, Request.Host.ToString())
                ?? $"{Request.Scheme}://{Request.Host}/Auth/ResetPassword";

            await _supabaseService.SendPasswordResetAsync(
                request.Email, resetUrl, cancellationToken);

            return Ok(new ApiForgotPasswordResponse
            {
                Success = true,
                Message = "If an account exists with that email, a reset link is on its way."
            });
        }
        catch (Exception ex)
        {
            // Log but don't reveal the actual error to the client.
            _logger.LogWarning(ex,
                "API forgot-password failed for {Email}", request.Email);

            return Ok(new ApiForgotPasswordResponse
            {
                Success = true,
                Message = "If an account exists with that email, a reset link is on its way."
            });
        }
    }

    // ────────────────────────────────────────────────────────
    // GET /api/auth/me
    // Requires auth (reads the cookie automatically).
    // Returns: { user: {...} }
    // ────────────────────────────────────────────────────────
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var profileIdClaim = User.FindFirst("profile_id")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(profileIdClaim) ||
            !Guid.TryParse(profileIdClaim, out var profileId))
        {
            return Unauthorized(new ApiErrorDto { Error = "Invalid session." });
        }

        var profile = await _supabaseService.GetProfileByIdAsync(
            profileId,
            User.FindFirst("supabase_access_token")?.Value ?? string.Empty,
            cancellationToken);

        if (profile is null)
        {
            return NotFound(new ApiErrorDto { Error = "Profile not found." });
        }

        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Client";
        return Ok(ApiUserDto.FromProfile(profile, role));
    }

    // ────────────────────────────────────────────────────────
    // PATCH /api/auth/me
    // Updates the current user's own profile (name, phone, position only).
    // Users cannot change their own role, status, or department.
    // ────────────────────────────────────────────────────────
    [HttpPatch("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMe(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var profileIdClaim = User.FindFirst("profile_id")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(profileIdClaim) ||
            !Guid.TryParse(profileIdClaim, out var profileId))
        {
            return Unauthorized(new ApiErrorDto { Error = "Invalid session." });
        }

        try
        {
            var profile = await _supabaseService.GetProfileByIdAsync(
                profileId,
                User.FindFirst("supabase_access_token")?.Value ?? string.Empty,
                cancellationToken);
            if (profile is null)
            {
                return NotFound(new ApiErrorDto { Error = "Profile not found." });
            }

            if (!string.IsNullOrWhiteSpace(request.FullName))
            {
                profile.FullName = request.FullName.Trim();
            }

            if (request.Phone != null)
            {
                profile.Phone = string.IsNullOrWhiteSpace(request.Phone)
                    ? null
                    : request.Phone.Trim();
            }

            if (request.Position != null)
            {
                profile.Position = string.IsNullOrWhiteSpace(request.Position)
                    ? null
                    : request.Position.Trim();
            }

            var updated = await _supabaseService.UpdateProfileAsync(profile, cancellationToken);
            if (!updated)
            {
                return StatusCode(500, new ApiErrorDto
                {
                    Error = "Unable to update your profile right now."
                });
            }

            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Client";
            return Ok(ApiUserDto.FromProfile(profile, role));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to update profile for {ProfileId}.", profileId);
            return StatusCode(500, new ApiErrorDto
            {
                Error = "Unable to update your profile right now."
            });
        }
    }

    // ────────────────────────────────────────────────────────
    // POST /api/auth/logout
    // Clears the cookie. Client should also clear its local jar.
    // ────────────────────────────────────────────────────────
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        Response.Cookies.Delete("ServiceHubITAuth");
        Response.Cookies.Delete("sb-access-token");
        Response.Cookies.Delete("sb-refresh-token");

        return NoContent();
    }

    // ────────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────────
    private static string NormalizeRole(string? raw)
    {
        var trimmed = raw?.Trim() ?? string.Empty;

        if (trimmed.Equals("admin", StringComparison.OrdinalIgnoreCase)) return "Admin";
        if (trimmed.Equals("technician", StringComparison.OrdinalIgnoreCase)) return "Technician";
        if (trimmed.Equals("client", StringComparison.OrdinalIgnoreCase)) return "Client";
        if (trimmed.Equals("pending", StringComparison.OrdinalIgnoreCase)) return "Pending";

        return string.Empty;
    }
}