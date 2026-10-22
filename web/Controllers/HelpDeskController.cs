using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceHub_IT.Interfaces;
using ServiceHub_IT.Models;
using ServiceHub_IT.Services;
using ServiceHub_IT.Services.Ai;

namespace ServiceHub_IT.Controllers;

[Authorize]
public class HelpDeskController : Controller
{
    private readonly TicketService _ticketService;
    private readonly TicketPredictionModel _ticketPredictionModel;
    private readonly ISupabaseService _supabaseService;
    private readonly NotificationService _notificationService;
    private readonly ILogger<HelpDeskController> _logger;

    public HelpDeskController(TicketService ticketService, TicketPredictionModel ticketPredictionModel, ISupabaseService supabaseService, NotificationService notificationService, ILogger<HelpDeskController> logger)
    {
        _ticketService = ticketService;
        _ticketPredictionModel = ticketPredictionModel;
        _supabaseService = supabaseService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (User.IsInRole("Client"))
        {
            return RedirectToAction("Index", "Tickets");
        }

        var user = User.Identity?.Name ?? User.FindFirst("email")?.Value ?? string.Empty;
        var isAdmin = User.IsInRole("Admin");
        var isTechnician = User.IsInRole("Technician") || User.IsInRole("Admin");

        IReadOnlyList<Ticket> tickets;
        if (isAdmin)
        {
            tickets = await _ticketService.GetTicketsAsync(cancellationToken: cancellationToken);
        }
        else if (isTechnician)
        {
            tickets = await _ticketService.GetTicketsAsync(assignedTechnician: user, cancellationToken: cancellationToken);
        }
        else
        {
            tickets = await _ticketService.GetTicketsAsync(requester: user, cancellationToken: cancellationToken);
        }

        return View(tickets);
    }

    [HttpGet]
    [Authorize(Roles = "Client")]
    public IActionResult Create()
    {
        return RedirectToAction("Create", "Tickets");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Technician")]
    public async Task<IActionResult> Create(CreateTicketViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst("email")?.Value
            ?? User.Identity?.Name
            ?? string.Empty;
        string? screenshotUrl;
        try
        {
            screenshotUrl = await UploadScreenshotAsync(model.Screenshot, User.FindFirst("supabase_access_token")?.Value, cancellationToken);
        }
        catch (InvalidDataException ex)
        {
            ModelState.AddModelError(nameof(model.Screenshot), ex.Message);
            return View(model);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Screenshot upload failed. Status/response details are included in the exception. Authenticated token present={HasAccessToken}", !string.IsNullOrWhiteSpace(User.FindFirst("supabase_access_token")?.Value));
            ModelState.AddModelError(nameof(model.Screenshot), "The screenshot could not be uploaded. Please try again.");
            return View(model);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Screenshot upload authentication failed. Authenticated token present={HasAccessToken}", !string.IsNullOrWhiteSpace(User.FindFirst("supabase_access_token")?.Value));
            ModelState.AddModelError(nameof(model.Screenshot), "Your Supabase session has expired. Please sign in again before uploading a screenshot.");
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HelpDeskController.Create failed while uploading the screenshot.");
            ModelState.AddModelError(nameof(model.Screenshot), "The screenshot could not be uploaded. Please try again.");
            return View(model);
        }

        var ticket = new Ticket
        {
            title = model.Title,
            description = model.Description,
            requester = user,
            priority = model.Priority,
            category = model.Category,
            screenshot_url = screenshotUrl,
            comments = string.Empty,
            assigned_technician = null,
            status = "Open",
            created_at = DateTime.UtcNow,
            updated_at = DateTime.UtcNow
        };

        var accessToken = User.FindFirst("supabase_access_token")?.Value;
        var created = await _ticketService.CreateTicketAsync(ticket, accessToken, cancellationToken);
        if (!created)
        {
            TempData["Error"] = "Unable to create the ticket right now.";
            return View(model);
        }

        var ticketLink = Url.Action("Details", "HelpDesk", new { id = ticket.id }) ?? $"/HelpDesk/Details?id={ticket.id}";
        var adminProfiles = (await _supabaseService.GetProfilesByRoleAsync("Admin", cancellationToken))
            .Where(profile => profile is not null && profile.Id != Guid.Empty)
            .GroupBy(profile => profile.Id)
            .Select(group => group.First())
            .ToList();

        if (adminProfiles.Count == 0)
        {
            _logger.LogWarning("HelpDeskController.Create no admin profiles found for ticket {TicketId}.", ticket.id);
        }
        else
        {
            foreach (var adminProfile in adminProfiles)
            {
                try
                {
                    _logger.LogInformation("Creating new ticket notification for admin {AdminUserId} for ticket {TicketId}", adminProfile.Id, ticket.id);
                    await _notificationService.CreateNotificationAsync(
                        adminProfile.Id,
                        "New Support Ticket",
                        $"A new support ticket '{ticket.title}' has been created by {user}.",
                        "new_ticket",
                        ticketLink,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create new ticket notification for admin {AdminUserId} for ticket {TicketId}", adminProfile.Id, ticket.id);
                }
            }
        }

        TempData["Success"] = "Ticket created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id, CancellationToken cancellationToken)
    {
        var ticket = await _ticketService.GetTicketAsync(id, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        if (User.IsInRole("Client") && ticket.employee_id != GetCurrentProfileId())
        {
            return Forbid();
        }

        var prediction = _ticketPredictionModel.Predict(ticket.title, ticket.description, ticket.category, ticket.priority);
        ViewBag.AiPrediction = prediction;

        ViewBag.Attachments = await _ticketService.GetTicketAttachmentsAsync(id, User.FindFirst("supabase_access_token")?.Value, cancellationToken);
        if (string.IsNullOrWhiteSpace(ticket.screenshot_url) && string.IsNullOrWhiteSpace(ticket.image_url)
            && ViewBag.Attachments is IReadOnlyList<TicketAttachment> attachments && attachments.Count > 0)
        {
            ticket.screenshot_url = attachments[0].FileUrl;
        }

        return View(ticket);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Technician")]
    public async Task<IActionResult> AcceptTicket(string id, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Admin"))
        {
            return Forbid();
        }

        var ticket = await _ticketService.GetTicketAsync(id, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        var user = User.Identity?.Name ?? User.FindFirst("email")?.Value ?? string.Empty;
        ticket.assigned_technician = user;
        ticket.status = "Assigned";
        ticket.updated_at = DateTime.UtcNow;
        ticket.comments = string.IsNullOrWhiteSpace(ticket.comments)
            ? $"{user}: accepted ticket"
            : $"{ticket.comments}\n{user}: accepted ticket";

        var updated = await _ticketService.UpdateTicketAsync(ticket, cancellationToken);
        if (!updated)
        {
            TempData["Error"] = "Unable to accept the ticket.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var ticketLink = Url.Action("Details", "HelpDesk", new { id = ticket.id }) ?? $"/HelpDesk/Details?id={ticket.id}";
        var technicianProfile = await _supabaseService.GetProfileByEmailAsync(user, cancellationToken);
        if (technicianProfile is not null && technicianProfile.Id != Guid.Empty)
        {
            _logger.LogInformation("Creating assignment notification for technician {TechnicianUserId} for ticket {TicketId}", technicianProfile.Id, ticket.id);
            await _notificationService.CreateNotificationAsync(
                technicianProfile.Id,
                "Ticket assigned",
                $"You accepted ticket '{ticket.title}'.",
                "ticket_assigned",
                ticketLink,
                cancellationToken);
        }
        else
        {
            _logger.LogWarning("HelpDeskController.AcceptTicket could not resolve technician profile for assignment value {AssignedTechnician} for ticket {TicketId}.", user, ticket.id);
        }

        TempData["Success"] = "Ticket accepted.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Technician")]
    public async Task<IActionResult> UpdateStatus(string id, UpdateTicketStatusViewModel model, CancellationToken cancellationToken)
    {
        var ticket = await _ticketService.GetTicketAsync(id, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        var user = User.Identity?.Name ?? User.FindFirst("email")?.Value ?? string.Empty;
        if (User.IsInRole("Technician") && !User.IsInRole("Admin") && !IsAssignedToCurrentTechnician(ticket, user))
        {
            return Forbid();
        }

        if (User.IsInRole("Technician") && !User.IsInRole("Admin")
            && !IsTechnicianStatus(model.Status))
        {
            TempData["Error"] = "Technicians may only set tickets to In Progress, Resolved, or Closed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        ticket.status = model.Status;
        ticket.updated_at = DateTime.UtcNow;
        ticket.due_date = model.DueDate;
        if (!string.IsNullOrWhiteSpace(model.Comment))
        {
            ticket.comments = string.IsNullOrWhiteSpace(ticket.comments)
                ? $"{user}: {model.Comment}"
                : $"{ticket.comments}\n{user}: {model.Comment}";
        }

        var updated = await _ticketService.UpdateTicketAsync(ticket, cancellationToken);
        if (!updated)
        {
            TempData["Error"] = "Unable to update the ticket.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var ticketLink = Url.Action("Details", "HelpDesk", new { id = ticket.id }) ?? $"/HelpDesk/Details?id={ticket.id}";
        var statusMessage = $"Ticket '{ticket.title}' is now {ticket.status}.";

        if (!string.IsNullOrWhiteSpace(ticket.requester))
        {
            await _notificationService.CreateNotificationForEmailAsync(
                ticket.requester,
                "Ticket status updated",
                statusMessage,
                "ticket_status",
                ticketLink,
                cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(ticket.assigned_technician) && !string.Equals(ticket.assigned_technician, ticket.requester, StringComparison.OrdinalIgnoreCase))
        {
            await _notificationService.CreateNotificationForEmailAsync(
                ticket.assigned_technician,
                "Ticket status updated",
                statusMessage,
                "ticket_status",
                ticketLink,
                cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(model.Comment))
        {
            var commenter = user;
            var commentRecipient = !string.Equals(ticket.requester, commenter, StringComparison.OrdinalIgnoreCase)
                ? ticket.requester
                : ticket.assigned_technician;

            if (!string.IsNullOrWhiteSpace(commentRecipient))
            {
                await _notificationService.CreateNotificationForEmailAsync(
                    commentRecipient,
                    "New comment on your ticket",
                    $"{commenter} added a comment to ticket '{ticket.title}'.",
                    "ticket_comment",
                    ticketLink,
                    cancellationToken);
            }
        }

        TempData["Success"] = "Ticket updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<string?> UploadScreenshotAsync(IFormFile? file, string? accessToken, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var path = $"tickets/{Guid.NewGuid():N}/{Guid.NewGuid():N}{extension}";
        return await _ticketService.UploadScreenshotAsync(file, path, accessToken, cancellationToken);
    }

    private Guid GetCurrentProfileId()
    {
        var value = User.FindFirst("profile_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var profileId) ? profileId : Guid.Empty;
    }

    private bool IsAssignedToCurrentTechnician(Ticket ticket, string email)
    {
        var profileId = GetCurrentProfileId();
        return (profileId != Guid.Empty && ticket.technician_id == profileId)
            || (!string.IsNullOrWhiteSpace(email)
                && string.Equals(ticket.assigned_technician, email, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsTechnicianStatus(string? status)
    {
        return string.Equals(status, "In Progress", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Resolved", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Closed", StringComparison.OrdinalIgnoreCase);
    }
}
