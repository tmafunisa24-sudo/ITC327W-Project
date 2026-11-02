// Controllers/Api/DashboardApiController.cs

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceHub_IT.DTOs.Api;
using ServiceHub_IT.Interfaces;
using ServiceHub_IT.Services;

namespace ServiceHub_IT.Controllers.Api;

[ApiController]
[Route("api/dashboard")]
[Authorize]
[Produces("application/json")]
public sealed class DashboardApiController : ControllerBase
{
    private readonly TicketService _ticketService;
    private readonly AssetService _assetService;
    private readonly ISupabaseService _supabaseService;
    private readonly ILogger<DashboardApiController> _logger;

    public DashboardApiController(
        TicketService ticketService,
        AssetService assetService,
        ISupabaseService supabaseService,
        ILogger<DashboardApiController> logger)
    {
        _ticketService = ticketService;
        _assetService = assetService;
        _supabaseService = supabaseService;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────
    // GET /api/dashboard
    // Returns role-appropriate metrics + recent tickets.
    // ────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        try
        {
            if (User.IsInRole("Admin"))
            {
                return await BuildAdminDashboard(cancellationToken);
            }
            if (User.IsInRole("Technician"))
            {
                return await BuildTechnicianDashboard(cancellationToken);
            }
            return await BuildClientDashboard(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to build dashboard for current user.");
            return StatusCode(500, new ApiErrorDto
            {
                Error = "Unable to load the dashboard right now."
            });
        }
    }

    // ────────────────────────────────────────────────────────
    // ADMIN
    // ────────────────────────────────────────────────────────
    private async Task<IActionResult> BuildAdminDashboard(CancellationToken ct)
    {
        var accessToken = User.FindFirst("supabase_access_token")?.Value;
        // Load all tickets, assets, profiles in parallel
        var ticketsTask = _ticketService.GetTicketsAsync(
            cancellationToken: ct, accessToken: accessToken, throwOnError: true);
        var assetsTask = _assetService.GetAssetsAsync(
            new ServiceHub_IT.Models.AssetSearchViewModel(), ct, accessToken, throwOnError: true);
        var profilesTask = _supabaseService.GetAllProfilesAsync(
            accessToken, ct, throwOnError: true);

        await Task.WhenAll(ticketsTask, assetsTask, profilesTask);

        var tickets = (await ticketsTask).ToList();
        var assets = (await assetsTask).ToList();
        var profiles = (await profilesTask).ToList();

        var pendingApprovals = profiles.Count(p =>
            string.Equals(p.Status, "Pending", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(p.Status, "Awaiting Approval", StringComparison.OrdinalIgnoreCase));

        var clients = profiles.Count(p =>
            string.Equals(p.Role, "Client", StringComparison.OrdinalIgnoreCase));

        var technicians = profiles.Count(p =>
            string.Equals(p.Role, "Technician", StringComparison.OrdinalIgnoreCase));

        var admins = profiles.Count(p =>
            string.Equals(p.Role, "Admin", StringComparison.OrdinalIgnoreCase));

        var resolved = tickets.Count(t =>
            string.Equals(t.status, "Resolved", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(t.status, "Closed", StringComparison.OrdinalIgnoreCase));

        var open = tickets.Count(t =>
            string.Equals(t.status, "Open", StringComparison.OrdinalIgnoreCase));

        var unresolved = tickets.Count - resolved;

        var availableAssets = assets.Count(a =>
            string.Equals(a.status, "Available", StringComparison.OrdinalIgnoreCase));
        var assignedAssets = assets.Count(a =>
            string.Equals(a.status, "Assigned", StringComparison.OrdinalIgnoreCase));
        var maintenanceAssets = assets.Count(a =>
            string.Equals(a.status, "Maintenance", StringComparison.OrdinalIgnoreCase));

        // Top technician = highest resolved ticket count
        var techCounts = tickets
            .Where(t => !string.IsNullOrWhiteSpace(t.assigned_technician))
            .Where(t =>
                string.Equals(t.status, "Resolved", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(t.status, "Closed", StringComparison.OrdinalIgnoreCase))
            .GroupBy(t => t.assigned_technician!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .FirstOrDefault();

        return Ok(new
        {
            role = "Admin",
            metrics = new
            {
                totalUsers = profiles.Count,
                pendingApprovals,
                clients,
                technicians,
                administrators = admins,
                totalTickets = tickets.Count,
                openTickets = open,
                unresolvedTickets = unresolved,
                resolvedTickets = resolved,
                totalAssets = assets.Count,
                availableAssets,
                assignedAssets,
                maintenanceAssets,
            },
            recentTickets = RecentTicketDtos(tickets, 5),
            topTechnician = techCounts is null
                ? null
                : new
                {
                    name = techCounts.Name,
                    resolvedCount = techCounts.Count,
                },
        });
    }

    // ────────────────────────────────────────────────────────
    // TECHNICIAN
    // ────────────────────────────────────────────────────────
    private async Task<IActionResult> BuildTechnicianDashboard(CancellationToken ct)
    {
        var accessToken = User.FindFirst("supabase_access_token")?.Value;
        var email = User.Identity?.Name
            ?? User.FindFirst(ClaimTypes.Email)?.Value
            ?? string.Empty;

        var ticketsTask = _ticketService.GetTicketsAsync(
            assignedTechnician: email, cancellationToken: ct, accessToken: accessToken,
            throwOnError: true);

        var allAssetsTask = _assetService.GetAssetsAsync(
            new ServiceHub_IT.Models.AssetSearchViewModel(), ct, accessToken, throwOnError: true);

        await Task.WhenAll(ticketsTask, allAssetsTask);

        var tickets = (await ticketsTask).ToList();
        var allAssets = (await allAssetsTask).ToList();

        var assignedAssets = allAssets.Count(a =>
            string.Equals(a.assigned_employee, email, StringComparison.OrdinalIgnoreCase));

        var completed = tickets.Count(t =>
            string.Equals(t.status, "Resolved", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(t.status, "Closed", StringComparison.OrdinalIgnoreCase));

        var inProgress = tickets.Count(t =>
            string.Equals(t.status, "In Progress", StringComparison.OrdinalIgnoreCase));

        var assigned = tickets.Count(t =>
            string.Equals(t.status, "Assigned", StringComparison.OrdinalIgnoreCase));

        var open = tickets.Count(t =>
            string.Equals(t.status, "Open", StringComparison.OrdinalIgnoreCase));

        return Ok(new
        {
            role = "Technician",
            metrics = new
            {
                assignedTickets = tickets.Count,
                completedTickets = completed,
                inProgressTickets = inProgress,
                pendingTickets = inProgress + assigned + open,
                assignedAssets,
            },
            recentTickets = RecentTicketDtos(tickets, 5),
        });
    }

    // ────────────────────────────────────────────────────────
    // CLIENT
    // ────────────────────────────────────────────────────────
    private async Task<IActionResult> BuildClientDashboard(CancellationToken ct)
    {
        var accessToken = User.FindFirst("supabase_access_token")?.Value;
        var profileIdClaim = User.FindFirst("profile_id")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(profileIdClaim, out var profileId))
        {
            return Ok(new
            {
                role = "Client",
                metrics = new
                {
                    myTickets = 0,
                    pendingTickets = 0,
                    resolvedTickets = 0,
                    assignedAssets = 0,
                },
                recentTickets = Array.Empty<object>(),
            });
        }

        var email = User.Identity?.Name
            ?? User.FindFirst(ClaimTypes.Email)?.Value
            ?? string.Empty;

        var ticketsTask = _ticketService.GetTicketsAsync(
            employeeId: profileId, cancellationToken: ct, accessToken: accessToken,
            throwOnError: true);

        var allAssetsTask = _assetService.GetAssetsAsync(
            new ServiceHub_IT.Models.AssetSearchViewModel(), ct, accessToken, throwOnError: true);

        await Task.WhenAll(ticketsTask, allAssetsTask);

        var tickets = (await ticketsTask).ToList();
        var allAssets = (await allAssetsTask).ToList();

        var assignedAssets = allAssets.Count(a =>
            string.Equals(a.assigned_employee, email, StringComparison.OrdinalIgnoreCase));

        var resolved = tickets.Count(t =>
            string.Equals(t.status, "Resolved", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(t.status, "Closed", StringComparison.OrdinalIgnoreCase));

        var pending = tickets.Count - resolved;

        return Ok(new
        {
            role = "Client",
            metrics = new
            {
                myTickets = tickets.Count,
                pendingTickets = pending,
                resolvedTickets = resolved,
                assignedAssets,
            },
            recentTickets = RecentTicketDtos(tickets, 5),
        });
    }

    // ────────────────────────────────────────────────────────
    // Helper — build compact ticket DTOs
    // ────────────────────────────────────────────────────────
    private static List<object> RecentTicketDtos(
        List<ServiceHub_IT.Models.Ticket> tickets,
        int limit)
    {
        return tickets
            .OrderByDescending(t => t.updated_at ?? t.created_at)
            .Take(limit)
            .Select(t => (object)new
            {
                id = t.id,
                title = t.title,
                status = t.status,
                priority = t.priority,
                assignedTechnician = t.assigned_technician,
                updatedAt = t.updated_at ?? t.created_at,
            })
            .ToList();
    }
}
