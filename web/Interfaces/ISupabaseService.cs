using ServiceHub_IT.DTOs;
using ServiceHub_IT.Models;

namespace ServiceHub_IT.Interfaces;

public interface ISupabaseService
{
    bool IsConfigured { get; }

    Task<SupabaseConnectionStatus> CheckConnectionAsync(CancellationToken cancellationToken = default);
    Task<SupabaseConnectionStatus> CheckAuthenticationAsync(CancellationToken cancellationToken = default);
    Task<SupabaseConnectionStatus> CheckStorageAsync(CancellationToken cancellationToken = default);
    Task<SupabaseConnectionStatus> CheckPostgresAsync(CancellationToken cancellationToken = default);

    Task<SupabaseAuthResult> SignInAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<SupabaseAuthResult> SignUpAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<string> GetRoleForEmailAsync(string email, string accessToken, CancellationToken cancellationToken = default);
    Task<bool> SendPasswordResetAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ResetPasswordAsync(string token, string password, CancellationToken cancellationToken = default);

    Task<Profile?> GetProfileByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Profile?> GetProfileByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Profile>> GetAllProfilesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Profile>> GetTechniciansAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Profile>> GetProfilesByRoleAsync(string role, CancellationToken cancellationToken = default);
    Task<IEnumerable<ServiceCatalogItem>> GetActiveServiceCatalogAsync(string? accessToken = null, CancellationToken cancellationToken = default);
    Task<bool> CreateProfileAsync(Profile profile, CancellationToken cancellationToken = default);
    Task<bool> UpdateProfileAsync(Profile profile, CancellationToken cancellationToken = default);
    Task<bool> DeleteProfileAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<Notification>> GetUserNotificationsAsync(Guid profileId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Notification>> GetNotificationsForUserAsync(Guid profileId, int limit, CancellationToken cancellationToken = default);
    Task<IEnumerable<Notification>> GetUnreadNotificationsForUserAsync(Guid profileId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadNotificationCountAsync(Guid profileId, CancellationToken cancellationToken = default);
    Task<bool> CreateNotificationAsync(Notification notification, CancellationToken cancellationToken = default);
    Task<bool> MarkNotificationAsReadAsync(Guid id, Guid profileId, CancellationToken cancellationToken = default);
    Task<bool> MarkAllNotificationsAsReadAsync(Guid profileId, CancellationToken cancellationToken = default);

    Task<IEnumerable<Maintenance>> GetAllMaintenanceAsync(CancellationToken cancellationToken = default);
    Task<Maintenance?> GetMaintenanceByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> CreateMaintenanceAsync(Maintenance maintenance, CancellationToken cancellationToken = default);
    Task<bool> UpdateMaintenanceAsync(Maintenance maintenance, CancellationToken cancellationToken = default);
    Task<bool> DeleteMaintenanceAsync(Guid id, CancellationToken cancellationToken = default);
}
