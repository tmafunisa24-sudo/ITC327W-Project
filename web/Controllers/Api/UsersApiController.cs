// Controllers/Api/UsersApiController.cs

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceHub_IT.DTOs.Api;
using ServiceHub_IT.Interfaces;

namespace ServiceHub_IT.Controllers.Api;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public sealed class UsersApiController : ControllerBase
{
    private readonly ISupabaseService _supabaseService;
    private readonly ILogger<UsersApiController> _logger;

    public UsersApiController(
        ISupabaseService supabaseService,
        ILogger<UsersApiController> logger)
    {
        _supabaseService = supabaseService;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────
    // GET /api/users
    // Returns all profiles. Admin only.
    // ────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var profiles = (await _supabaseService.GetAllProfilesAsync(cancellationToken))
                .Where(p => p is not null)
                .OrderBy(p => p.FullName)
                .ThenBy(p => p.Email)
                .Select(p => new
                {
                    id = p.Id,
                    fullName = p.FullName,
                    email = p.Email,
                    role = p.Role,
                    status = p.Status,
                    phone = p.Phone,
                    position = p.Position,
                    departmentId = p.DepartmentId,
                    emailVerified = p.EmailVerified,
                    createdAt = p.CreatedAt,
                })
                .ToList();

            return Ok(profiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to load users for API.");
            return StatusCode(500, new ApiErrorDto
            {
                Error = "Unable to load users right now."
            });
        }
    }

    // ────────────────────────────────────────────────────────
    // PATCH /api/users/{id}
    // Updates a user's role and/or status. Admin only.
    // Prevents admins from disabling or demoting themselves.
    // ────────────────────────────────────────────────────────
    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new ApiErrorDto { Error = "User id is required." });
        }

        // ── Prevent self-demotion / self-disable ──
        var currentProfileIdClaim = User.FindFirst("profile_id")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (Guid.TryParse(currentProfileIdClaim, out var currentProfileId) &&
            currentProfileId == id)
        {
            var willDemote = !string.IsNullOrWhiteSpace(request.Role) &&
                !string.Equals(NormalizeRole(request.Role), "Admin", StringComparison.OrdinalIgnoreCase);

            var willDisable = !string.IsNullOrWhiteSpace(request.Status) &&
                string.Equals(request.Status.Trim(), "Disabled", StringComparison.OrdinalIgnoreCase);

            if (willDemote)
            {
                return BadRequest(new ApiErrorDto
                {
                    Error = "You cannot change your own role."
                });
            }

            if (willDisable)
            {
                return BadRequest(new ApiErrorDto
                {
                    Error = "You cannot disable your own account."
                });
            }
        }

        try
        {
            var profile = await _supabaseService.GetProfileByIdAsync(
                id,
                User.FindFirst("supabase_access_token")?.Value ?? string.Empty,
                cancellationToken);
            if (profile is null)
            {
                return NotFound(new ApiErrorDto { Error = "User not found." });
            }

            // ── Apply changes ──
            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                var normalizedRole = NormalizeRole(request.Role);
                if (string.IsNullOrWhiteSpace(normalizedRole))
                {
                    return BadRequest(new ApiErrorDto
                    {
                        Error = "Invalid role. Allowed: Admin, Technician, Client."
                    });
                }
                profile.Role = normalizedRole;
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                var normalizedStatus = NormalizeStatus(request.Status);
                if (string.IsNullOrWhiteSpace(normalizedStatus))
                {
                    return BadRequest(new ApiErrorDto
                    {
                        Error = "Invalid status. Allowed: Active, Pending, Disabled, Approved."
                    });
                }
                profile.Status = normalizedStatus;
            }

            var updated = await _supabaseService.UpdateProfileAsync(profile, cancellationToken);
            if (!updated)
            {
                return StatusCode(500, new ApiErrorDto
                {
                    Error = "Unable to update the user right now."
                });
            }

            _logger.LogInformation(
                "User {UserId} updated by admin {AdminId}. Role={Role}, Status={Status}.",
                id, currentProfileIdClaim, profile.Role, profile.Status);

            return Ok(new
            {
                id = profile.Id,
                fullName = profile.FullName,
                email = profile.Email,
                role = profile.Role,
                status = profile.Status,
                phone = profile.Phone,
                position = profile.Position,
                departmentId = profile.DepartmentId,
                emailVerified = profile.EmailVerified,
                createdAt = profile.CreatedAt,
            });
        }
        catch (InvalidOperationException ex)
        {
            // SupabaseService.UpdateProfileAsync throws this on Supabase failure
            _logger.LogWarning(ex, "Supabase rejected user update for {UserId}.", id);
            return StatusCode(500, new ApiErrorDto
            {
                Error = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to update user {UserId}.", id);
            return StatusCode(500, new ApiErrorDto
            {
                Error = "Unable to update the user right now."
            });
        }
    }

    // ────────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────────
    private static string NormalizeRole(string? raw)
    {
        return raw?.Trim().ToLowerInvariant() switch
        {
            "admin" => "Admin",
            "technician" => "Technician",
            "client" => "Client",
            _ => string.Empty,
        };
    }

    private static string NormalizeStatus(string? raw)
    {
        return raw?.Trim().ToLowerInvariant() switch
        {
            "active" => "Active",
            "pending" => "Pending",
            "awaiting approval" => "Awaiting Approval",
            "disabled" => "Disabled",
            "approved" => "Approved",
            _ => string.Empty,
        };
    }
}