using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceHub_IT.DTOs;
using ServiceHub_IT.Services;

namespace ServiceHub_IT.Controllers;

[Authorize(Roles = "Technician")]
public class TechnicianController : Controller
{
    private readonly TicketService _ticketService;

    public TechnicianController(TicketService ticketService)
    {
        _ticketService = ticketService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var accessToken = User.FindFirst("supabase_access_token")?.Value;
        var email = User.Identity?.Name ?? User.FindFirst("email")?.Value ?? string.Empty;
        var profileId = Guid.TryParse(User.FindFirst("profile_id")?.Value, out var parsedProfileId) ? parsedProfileId : Guid.Empty;
        var tickets = (await _ticketService.GetTechnicianTicketsAsync(profileId, email, accessToken, cancellationToken)).ToList();

        var model = new TechnicianDashboardViewModel
        {
            Tickets = tickets,
            CompletedCount = tickets.Count(t => string.Equals(t.status, "Resolved", StringComparison.OrdinalIgnoreCase) || string.Equals(t.status, "Closed", StringComparison.OrdinalIgnoreCase)),
            DueSoonCount = tickets.Count(t => !string.Equals(t.status, "Resolved", StringComparison.OrdinalIgnoreCase) && !string.Equals(t.status, "Closed", StringComparison.OrdinalIgnoreCase))
        };

        return View(model);
    }
}
