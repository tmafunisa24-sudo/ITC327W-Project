using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceHub_IT.Interfaces;
using ServiceHub_IT.Models;

namespace ServiceHub_IT.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly ISupabaseService _supabaseService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(ISupabaseService supabaseService, ILogger<NotificationsController> logger)
    {
        _supabaseService = supabaseService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var profileId = await GetCurrentProfileIdAsync(cancellationToken);
        if (profileId == Guid.Empty)
        {
            return View(Array.Empty<Notification>());
        }

        var accessToken = User.FindFirst("supabase_access_token")?.Value;
        _logger.LogInformation(
            "Fetching notifications for ProfileId: {ProfileId}, HasToken: {HasToken}",
            profileId,
            !string.IsNullOrWhiteSpace(accessToken));
        var notifications = (await _supabaseService.GetUserNotificationsAsync(profileId, accessToken, cancellationToken)).ToList();
        return View(notifications);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return RedirectToAction(nameof(Index));
        }

        var profileId = await GetCurrentProfileIdAsync(cancellationToken);
        if (profileId == Guid.Empty)
        {
            return RedirectToAction(nameof(Index));
        }

        var accessToken = User.FindFirst("supabase_access_token")?.Value;
        var notification = (await _supabaseService.GetUserNotificationsAsync(profileId, accessToken, cancellationToken))
            .FirstOrDefault(x => x.id == id);

        if (notification is null || notification.user_id != profileId)
        {
            return Forbid();
        }

        await _supabaseService.MarkNotificationAsReadAsync(id, profileId, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var profileId = await GetCurrentProfileIdAsync(cancellationToken);
        if (profileId == Guid.Empty)
        {
            return RedirectToAction(nameof(Index));
        }

        await _supabaseService.MarkAllNotificationsAsReadAsync(profileId, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Recent(CancellationToken cancellationToken)
    {
        var profileId = await GetCurrentProfileIdAsync(cancellationToken);
        if (profileId == Guid.Empty)
        {
            return Json(new { unreadCount = 0, notifications = Array.Empty<object>() });
        }

        var accessToken = User.FindFirst("supabase_access_token")?.Value;
        var recentNotifications = (await _supabaseService.GetNotificationsForUserAsync(profileId, 6, accessToken, cancellationToken)).ToList();
        var unreadCount = await _supabaseService.GetUnreadNotificationCountAsync(profileId, accessToken, cancellationToken);

        var payload = recentNotifications.Select(notification => new
        {
            notification.id,
            notification.title,
            notification.message,
            notification.link,
            isRead = notification.is_read,
            notification.type,
            createdAt = notification.created_at,
            icon = GetNotificationIcon(notification.type)
        });

        _logger.LogInformation("NotificationsController.Recent resolved profile ID {ProfileId}; unreadCount={UnreadCount}; recentNotifications={RecentCount}", profileId, unreadCount, recentNotifications.Count);
        return Json(new { unreadCount, notifications = payload });
    }

    [HttpGet]
    public async Task<IActionResult> Diagnostic(CancellationToken cancellationToken)
    {
        var user = User;
        var isAuthenticated = user?.Identity?.IsAuthenticated == true;
        var email = user?.FindFirst(ClaimTypes.Email)?.Value ?? user?.Identity?.Name;
        var name = user?.FindFirst(ClaimTypes.Name)?.Value;
        var role = user?.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";
        var nameIdentifier = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var profileIdClaim = user?.FindFirst("profile_id")?.Value;
        var subClaim = user?.FindFirst("sub")?.Value;
        var profileIdClaim2 = user?.FindFirst("profileId")?.Value;
        var profileIdClaim3 = user?.FindFirst("ProfileId")?.Value;

        _logger.LogInformation("Notification debug: Authenticated={Authenticated}, Email={Email}, Name={Name}, NameIdentifier={NameIdentifier}, profile_id={ProfileIdClaim}, sub={SubClaim}, profileId={ProfileIdClaim2}, ProfileId={ProfileIdClaim3}, Role={Role}",
            isAuthenticated, email, name, nameIdentifier, profileIdClaim, subClaim, profileIdClaim2, profileIdClaim3, role);

        var resolvedProfileId = await GetCurrentProfileIdAsync(cancellationToken);
        var accessToken = User.FindFirst("supabase_access_token")?.Value;
        var unreadCount = resolvedProfileId == Guid.Empty ? 0 : await _supabaseService.GetUnreadNotificationCountAsync(resolvedProfileId, accessToken, cancellationToken);
        var unreadNotifications = resolvedProfileId == Guid.Empty
            ? Array.Empty<string>()
            : (await _supabaseService.GetUnreadNotificationsForUserAsync(resolvedProfileId, accessToken, cancellationToken))
                .Where(n => !string.IsNullOrWhiteSpace(n.title))
                .Select(n => n.title!).ToArray();

        return Json(new
        {
            isAuthenticated,
            email,
            name,
            nameIdentifier,
            profileIdClaim,
            subClaim,
            profileIdClaim2,
            profileIdClaim3,
            role,
            resolvedProfileId,
            unreadCount,
            unreadNotifications
        });
    }

    [HttpGet]
    public async Task<IActionResult> UnreadCount(CancellationToken cancellationToken)
    {
        var profileId = await GetCurrentProfileIdAsync(cancellationToken);
        if (profileId == Guid.Empty)
        {
            return Json(new { count = 0 });
        }

        var accessToken = User.FindFirst("supabase_access_token")?.Value;
        var count = await _supabaseService.GetUnreadNotificationCountAsync(profileId, accessToken, cancellationToken);
        return Json(new { count });
    }

    private static string GetNotificationIcon(string type)
    {
        return type?.ToLowerInvariant() switch
        {
            "ticket_assigned" => "bi-ticket-detailed",
            "ticket_status" => "bi-arrow-repeat",
            "ticket_resolved" => "bi-check-circle",
            "ticket_comment" => "bi-chat-dots",
            "new_ticket" => "bi-plus-circle",
            "user_registration" => "bi-person-plus",
            "asset_assigned" => "bi-box-seam",
            "maintenance" => "bi-tools",
            "system" => "bi-info-circle",
            _ => "bi-bell"
        };
    }

    private async Task<Guid> GetCurrentProfileIdAsync(CancellationToken cancellationToken)
    {
        var user = User;
        var profileIdClaim = user?.FindFirst("profile_id")?.Value;
        
        if (profileIdClaim != null && Guid.TryParse(profileIdClaim, out var profileId) && profileId != Guid.Empty)
        {
            return profileId;
        }

        // No fallback to email lookup - return Guid.Empty if profile_id claim is missing
        return Guid.Empty;
    }
}
