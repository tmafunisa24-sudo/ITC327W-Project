using System.Text.Json.Serialization;
using ServiceHub_IT.Interfaces;
using ServiceHub_IT.Models;

namespace ServiceHub_IT.Services;

public class NotificationService
{
    private readonly ISupabaseService _supabaseService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ISupabaseService supabaseService, ILogger<NotificationService> logger)
    {
        _supabaseService = supabaseService;
        _logger = logger;
    }

    public async Task CreateNotificationAsync(Guid userId, string title, string message, string type, string? link, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var notification = new Notification
        {
            id = Guid.NewGuid(),
            user_id = userId,
            title = title,
            message = message,
            type = string.IsNullOrWhiteSpace(type) ? "general" : type,
            link = link,
            is_read = false,
            created_at = DateTime.UtcNow
        };

        var created = await _supabaseService.CreateNotificationAsync(notification, cancellationToken);
        if (!created)
        {
            _logger.LogWarning("NotificationService failed to create notification for user {UserId} title {Title} link {Link}", userId, title, link);
        }
    }

    public async Task CreateNotificationForEmailAsync(string? email, string title, string message, string type, string? link, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var profile = await _supabaseService.GetProfileByEmailAsync(email, cancellationToken);
        if (profile is null || profile.Id == Guid.Empty)
        {
            _logger.LogWarning("NotificationService could not resolve profile for email {Email}", email);
            return;
        }

        await CreateNotificationAsync(profile.Id, title, message, type, link, cancellationToken);
    }

    public async Task CreateNotificationsForEmailsAsync(IEnumerable<string?> emails, string title, string message, string type, string? link, CancellationToken cancellationToken = default)
    {
        if (emails is null)
        {
            return;
        }

        var uniqueEmails = emails.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var email in uniqueEmails)
        {
            await CreateNotificationForEmailAsync(email, title, message, type, link, cancellationToken);
        }
    }

    public async Task CreateNotificationsForRolesAsync(IEnumerable<string> roles, string title, string message, string type, string? link, CancellationToken cancellationToken = default)
    {
        if (roles is null)
        {
            return;
        }

        var userIds = new HashSet<Guid>();
        foreach (var role in roles.Where(r => !string.IsNullOrWhiteSpace(r)))
        {
            var profiles = await _supabaseService.GetProfilesByRoleAsync(role.Trim(), cancellationToken);
            foreach (var profile in profiles)
            {
                if (profile is not null && profile.Id != Guid.Empty)
                {
                    userIds.Add(profile.Id);
                }
            }
        }

        foreach (var userId in userIds)
        {
            await CreateNotificationAsync(userId, title, message, type, link, cancellationToken);
        }
    }
}
