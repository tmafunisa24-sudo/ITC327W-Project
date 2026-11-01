// Controllers/Api/NotificationsApiController.cs

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceHub_IT.DTOs.Api;
using ServiceHub_IT.Interfaces;

namespace ServiceHub_IT.Controllers.Api;

[ApiController]
[Route("api/notifications")]
[Authorize]
[Produces("application/json")]
public sealed class NotificationsApiController : ControllerBase
{
    private readonly ISupabaseService _supabaseService;
    private readonly ILogger<NotificationsApiController> _logger;

    public NotificationsApiController(
        ISupabaseService supabaseService,
        ILogger<NotificationsApiController> logger)
    {
        _supabaseService = supabaseService;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────
    // GET /api/notifications
    // Returns all notifications for the current user,
    // ordered newest-first (handled by SupabaseService).
    // ────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var profileId = ResolveProfileId();
        if (profileId == Guid.Empty)
        {
            return Unauthorized(new ApiErrorDto
            {
                Error = "Unable to resolve your profile."
            });
        }

        try
        {
            var accessToken = User.FindFirst("supabase_access_token")?.Value;
            var notifications = await _supabaseService.GetUserNotificationsAsync(
                profileId, accessToken, cancellationToken);

            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to load notifications for user {ProfileId}.", profileId);
            return StatusCode(500, new ApiErrorDto
            {
                Error = "Unable to load notifications right now."
            });
        }
    }

    // ────────────────────────────────────────────────────────
    // GET /api/notifications/recent?limit=6
    // Returns the N most recent notifications + unread count.
    // Single call for the app-bar bell.
    // ────────────────────────────────────────────────────────
    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent(
        [FromQuery] int limit = 6,
        CancellationToken cancellationToken = default)
    {
        var profileId = ResolveProfileId();
        if (profileId == Guid.Empty)
        {
            return Unauthorized(new ApiErrorDto
            {
                Error = "Unable to resolve your profile."
            });
        }

        // Clamp limit to something sane
        if (limit < 1) limit = 6;
        if (limit > 50) limit = 50;

        try
        {
            var accessToken = User.FindFirst("supabase_access_token")?.Value;
            var recent = await _supabaseService.GetNotificationsForUserAsync(
                profileId, limit, accessToken, cancellationToken);

            var unread = await _supabaseService.GetUnreadNotificationCountAsync(
                profileId, accessToken, cancellationToken);

            return Ok(new
            {
                unreadCount = unread,
                notifications = recent.Select(n => new
                {
                    n.id,
                    n.title,
                    n.message,
                    n.link,
                    isRead = n.is_read,
                    n.type,
                    createdAt = n.created_at,
                }).ToList(),
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to load recent notifications for user {ProfileId}.", profileId);
            return StatusCode(500, new ApiErrorDto
            {
                Error = "Unable to load notifications right now."
            });
        }
    }

    // ────────────────────────────────────────────────────────
    // GET /api/notifications/unread-count
    // Lightweight endpoint for polling the badge.
    // ────────────────────────────────────────────────────────
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var profileId = ResolveProfileId();
        if (profileId == Guid.Empty)
        {
            return Unauthorized(new ApiErrorDto
            {
                Error = "Unable to resolve your profile."
            });
        }

        try
        {
            var accessToken = User.FindFirst("supabase_access_token")?.Value;
            var count = await _supabaseService.GetUnreadNotificationCountAsync(
                profileId, accessToken, cancellationToken);

            return Ok(new { count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to get unread count for user {ProfileId}.", profileId);
            return StatusCode(500, new ApiErrorDto
            {
                Error = "Unable to load notifications right now."
            });
        }
    }

    // ────────────────────────────────────────────────────────
    // POST /api/notifications/{id}/read
    // Marks a single notification as read.
    // ────────────────────────────────────────────────────────
    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(
        string id,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id) || !Guid.TryParse(id, out var notificationId))
        {
            return BadRequest(new ApiErrorDto { Error = "Invalid notification id." });
        }

        var profileId = ResolveProfileId();
        if (profileId == Guid.Empty)
        {
            return Unauthorized(new ApiErrorDto
            {
                Error = "Unable to resolve your profile."
            });
        }

        try
        {
            var ok = await _supabaseService.MarkNotificationAsReadAsync(
                notificationId, profileId, cancellationToken);

            if (!ok)
            {
                return StatusCode(500, new ApiErrorDto
                {
                    Error = "Unable to mark the notification as read."
                });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to mark notification {Id} as read.", id);
            return StatusCode(500, new ApiErrorDto
            {
                Error = "Unable to mark the notification as read."
            });
        }
    }

    // ────────────────────────────────────────────────────────
    // POST /api/notifications/read-all
    // Marks every unread notification for the current user as read.
    // ────────────────────────────────────────────────────────
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var profileId = ResolveProfileId();
        if (profileId == Guid.Empty)
        {
            return Unauthorized(new ApiErrorDto
            {
                Error = "Unable to resolve your profile."
            });
        }

        try
        {
            var ok = await _supabaseService.MarkAllNotificationsAsReadAsync(
                profileId, cancellationToken);

            if (!ok)
            {
                return StatusCode(500, new ApiErrorDto
                {
                    Error = "Unable to mark notifications as read."
                });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to mark all notifications as read for user {ProfileId}.", profileId);
            return StatusCode(500, new ApiErrorDto
            {
                Error = "Unable to mark notifications as read."
            });
        }
    }

    // ────────────────────────────────────────────────────────
    // Helper: resolve the current user's profile_id from claims.
    // ────────────────────────────────────────────────────────
    private Guid ResolveProfileId()
    {
        var value = User.FindFirst("profile_id")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }
}