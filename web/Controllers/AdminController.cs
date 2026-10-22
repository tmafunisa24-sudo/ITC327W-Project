using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceHub_IT.DTOs;
using ServiceHub_IT.Models;
using ServiceHub_IT.Services;
using ServiceHub_IT.Interfaces;
using System.Globalization;

namespace ServiceHub_IT.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly TicketService _ticketService;
    private readonly ISupabaseService _supabaseService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(TicketService ticketService, ISupabaseService supabaseService, ILogger<AdminController> logger)
    {
        _ticketService = ticketService;
        _supabaseService = supabaseService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var tickets = (await _ticketService.GetTicketsAsync(cancellationToken: cancellationToken)).ToList();
        var profiles = (await _supabaseService.GetAllProfilesAsync(cancellationToken)).ToList();

        var clientProfiles = profiles.Where(p => string.Equals(p.Role, "Client", StringComparison.OrdinalIgnoreCase)).ToList();
        var technicianProfiles = profiles.Where(p => string.Equals(p.Role, "Technician", StringComparison.OrdinalIgnoreCase)).ToList();
        var administratorProfiles = profiles.Where(p => string.Equals(p.Role, "Admin", StringComparison.OrdinalIgnoreCase)).ToList();
        var pendingProfiles = profiles.Where(p => string.Equals(p.Status, "Pending", StringComparison.OrdinalIgnoreCase)
            || string.Equals(p.Status, "Awaiting Approval", StringComparison.OrdinalIgnoreCase)).ToList();

        var technicianTicketCounts = tickets
            .Where(t => !string.IsNullOrWhiteSpace(t.assigned_technician))
            .GroupBy(t => t.assigned_technician!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new { Technician = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        var topTechnician = technicianTicketCounts.FirstOrDefault();
        var lowestTechnician = technicianTicketCounts.LastOrDefault();

        var technicianPerformance = BuildTechnicianPerformanceViewModel(tickets, profiles);

        var model = new AdminDashboardViewModel
        {
            TotalUsers = profiles.Count,
            PendingApprovals = pendingProfiles.Count,
            Employees = clientProfiles.Count,
            Technicians = technicianProfiles.Count,
            Administrators = administratorProfiles.Count,
            ResolvedTickets = tickets.Count(t => string.Equals(t.status, "Resolved", StringComparison.OrdinalIgnoreCase) || string.Equals(t.status, "Closed", StringComparison.OrdinalIgnoreCase)),
            UnresolvedTickets = tickets.Count(t => !string.Equals(t.status, "Resolved", StringComparison.OrdinalIgnoreCase) && !string.Equals(t.status, "Closed", StringComparison.OrdinalIgnoreCase)),
            TopTechnician = topTechnician?.Technician,
            TopTechnicianCount = topTechnician?.Count ?? 0,
            LowestTechnician = lowestTechnician?.Technician,
            LowestTechnicianCount = lowestTechnician?.Count ?? 0,
            Tickets = tickets,
            Profiles = profiles,
            TechnicianPerformance = technicianPerformance
        };

        return View(model);
    }

    private static TechnicianPerformanceViewModel BuildTechnicianPerformanceViewModel(IReadOnlyList<Ticket> tickets, IReadOnlyList<Profile> profiles)
    {
        var technicianProfileKeys = profiles
            .Where(p => string.Equals(p.Role, "Technician", StringComparison.OrdinalIgnoreCase))
            .Select(p => new
            {
                DisplayName = string.IsNullOrWhiteSpace(p.FullName) ? p.Email : p.FullName,
                Keys = new[]
                {
                    p.Email,
                    p.FullName,
                    p.Id.ToString()
                }
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
            })
            .ToList();

        var technicianRows = technicianProfileKeys
            .Select(profile =>
            {
                var assignedTickets = tickets.Count(t => profile.Keys.Any(key => string.Equals(t.assigned_technician, key, StringComparison.OrdinalIgnoreCase))
                    || (Guid.TryParse(profile.Keys.LastOrDefault(), out var profileId) && t.technician_id == profileId));
                var resolvedTickets = tickets.Count(t => (profile.Keys.Any(key => string.Equals(t.assigned_technician, key, StringComparison.OrdinalIgnoreCase))
                    || (Guid.TryParse(profile.Keys.LastOrDefault(), out var profileId) && t.technician_id == profileId))
                    && (string.Equals(t.status, "Resolved", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(t.status, "Closed", StringComparison.OrdinalIgnoreCase)));

                var resolutionHours = tickets
                    .Where(t => (profile.Keys.Any(key => string.Equals(t.assigned_technician, key, StringComparison.OrdinalIgnoreCase))
                        || (Guid.TryParse(profile.Keys.LastOrDefault(), out var profileId) && t.technician_id == profileId))
                        && (string.Equals(t.status, "Resolved", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(t.status, "Closed", StringComparison.OrdinalIgnoreCase)))
                    .Select(t =>
                    {
                        var createdAt = t.created_at ?? DateTime.UtcNow;
                        var closedAt = t.closed_at ?? t.updated_at ?? t.created_at ?? DateTime.UtcNow;
                        if (createdAt > closedAt)
                        {
                            return 0d;
                        }

                        return Math.Max(0, (closedAt - createdAt).TotalHours);
                    })
                    .ToList();

                var averageResolutionHours = resolutionHours.Any() ? resolutionHours.Average() : 0d;
                return new TechnicianPerformanceRow
                {
                    TechnicianName = profile.DisplayName,
                    AssignedTickets = assignedTickets,
                    ResolvedTickets = resolvedTickets,
                    AverageResolutionHours = Math.Round(averageResolutionHours, 1),
                    RatingLabel = GetRatingLabel(averageResolutionHours)
                };
            })
            .Where(row => row.AssignedTickets > 0 || row.ResolvedTickets > 0)
            .OrderByDescending(row => row.ResolvedTickets)
            .ThenBy(row => row.AverageResolutionHours)
            .ToList();

        var topRow = technicianRows.FirstOrDefault();

        return new TechnicianPerformanceViewModel
        {
            TopTechnicianName = topRow?.TechnicianName ?? "No technician data",
            TopTechnicianResolvedTickets = topRow?.ResolvedTickets ?? 0,
            TopTechnicianAverageResolutionHours = topRow?.AverageResolutionHours ?? 0d,
            TopTechnicianRatingLabel = topRow?.RatingLabel ?? string.Empty,
            Leaderboard = technicianRows,
            ChartLabels = technicianRows.Select(r => r.TechnicianName).ToList(),
            ResolvedTicketCounts = technicianRows.Select(r => r.ResolvedTickets).ToList(),
            AverageHours = technicianRows.Select(r => r.AverageResolutionHours).ToList()
        };
    }

    private static string GetRatingLabel(double averageHours)
    {
        if (averageHours < 3)
        {
            return "★★★★★";
        }

        if (averageHours < 5)
        {
            return "★★★★☆";
        }

        if (averageHours < 8)
        {
            return "★★★☆☆";
        }

        if (averageHours < 12)
        {
            return "★★☆☆☆";
        }

        return "★☆☆☆☆";
    }

    [HttpGet]
    public async Task<IActionResult> Users(CancellationToken cancellationToken)
    {
        var profiles = (await _supabaseService.GetAllProfilesAsync(cancellationToken)).ToList();
        var model = new AdminUsersViewModel
        {
            Profiles = profiles,
            TotalUsers = profiles.Count,
            PendingApprovals = profiles.Count(p => string.Equals(p.Status, "Pending", StringComparison.OrdinalIgnoreCase)
                || string.Equals(p.Status, "Awaiting Approval", StringComparison.OrdinalIgnoreCase)),
            Employees = profiles.Count(p => string.Equals(p.Role, "Client", StringComparison.OrdinalIgnoreCase)),
            Technicians = profiles.Count(p => string.Equals(p.Role, "Technician", StringComparison.OrdinalIgnoreCase)),
            Administrators = profiles.Count(p => string.Equals(p.Role, "Admin", StringComparison.OrdinalIgnoreCase))
        };

        return View(model);
    }

    private static string NormalizeRole(string? role)
    {
        return role?.Trim().ToLowerInvariant() switch
        {
            "admin" => "Admin",
            "technician" => "Technician",
            "client" => "Client",
            _ => string.Empty
        };
    }

    private static string NormalizeStatus(string? status)
    {
        return status?.Trim().ToLowerInvariant() switch
        {
            "pending" or "awaiting approval" => "Pending",
            "active" => "Active",
            "disabled" => "Disabled",
            "approved" => "Approved",
            _ => "Active"
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(Guid id, string role, string status, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            TempData["ErrorMessage"] = "A valid user record is required to update the profile.";
            return RedirectToAction(nameof(Users));
        }

        try
        {
            var profile = await _supabaseService.GetProfileByIdAsync(id, cancellationToken);
            if (profile is null)
            {
                TempData["ErrorMessage"] = "The selected user profile could not be found.";
                return RedirectToAction(nameof(Users));
            }

            profile.Role = NormalizeRole(role);
            profile.Status = NormalizeStatus(status);
            if (string.IsNullOrWhiteSpace(profile.Role))
            {
                TempData["ErrorMessage"] = "The selected role is not supported.";
                return RedirectToAction(nameof(Users));
            }
            profile.UpdatedAt = DateTime.UtcNow;

            var success = await _supabaseService.UpdateProfileAsync(profile, cancellationToken);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? "Profile updated successfully."
                : "The profile could not be updated right now.";

            return RedirectToAction(nameof(Users));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Profile update failed for profile {ProfileId}", id);
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Users));
        }
    }
}
