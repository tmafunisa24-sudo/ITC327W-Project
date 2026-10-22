using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceHub_IT.Interfaces;
using ServiceHub_IT.Models;
using ServiceHub_IT.Services;

namespace ServiceHub_IT.Controllers;

[Authorize]
public class TicketsController : Controller
{
    private readonly TicketService _ticketService;
    private readonly ISupabaseService _supabaseService;
    private readonly NotificationService _notificationService;
    private readonly ILogger<TicketsController> _logger;

    public TicketsController(TicketService ticketService, ISupabaseService supabaseService, NotificationService notificationService, ILogger<TicketsController> logger)
    {
        _ticketService = ticketService;
        _supabaseService = supabaseService;
        _notificationService = notificationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var email = User.Identity?.Name ?? User.FindFirst("email")?.Value ?? string.Empty;
        IReadOnlyList<Ticket> tickets;

        if (User.IsInRole("Admin"))
        {
            tickets = await _ticketService.GetTicketsAsync(cancellationToken: cancellationToken);
        }
        else if (User.IsInRole("Technician"))
        {
            tickets = await _ticketService.GetTechnicianTicketsAsync(GetCurrentProfileId(), email, cancellationToken);
        }
        else
        {
            var profileId = GetCurrentProfileId();
            tickets = profileId == Guid.Empty
                ? Array.Empty<Ticket>()
                : await _ticketService.GetTicketsAsync(employeeId: profileId, cancellationToken: cancellationToken);
        }

        var technicians = (await _supabaseService.GetTechniciansAsync(cancellationToken)).OrderBy(p => p.FullName).ThenBy(p => p.Email).ToList();
        ViewBag.Technicians = technicians;
        return View(tickets);
    }

    [HttpGet]
    [Authorize(Roles = "Client")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        try
        {
            await PopulateCatalogAsync(cancellationToken);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "TicketsController.Create could not load the service catalog because the Supabase session expired.");
            return RedirectToAction("Login", "Auth", new { returnUrl = Request.Path.ToString() });
        }

        return View(new CreateTicketViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Client")]
    public async Task<IActionResult> Create(CreateTicketViewModel model, CancellationToken cancellationToken)
    {
        _logger.LogInformation("TicketsController.Create POST entered.");

        if (string.IsNullOrWhiteSpace(model.Title) && !string.IsNullOrWhiteSpace(model.ProblemType))
        {
            model.Title = $"{model.DeviceType} repair: {model.ProblemType}";
        }

        if (string.IsNullOrWhiteSpace(model.Category))
        {
            model.Category = model.ProblemCategory;
        }

        ModelState.Remove(nameof(model.Title));
        ModelState.Remove(nameof(model.Category));

        List<ServiceCatalogItem> catalog;
        try
        {
            catalog = (await _supabaseService.GetActiveServiceCatalogAsync(GetSupabaseAccessToken(), cancellationToken)).ToList();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "TicketsController.Create could not load the service catalog because the Supabase session expired.");
            return RedirectToAction("Login", "Auth", new { returnUrl = Request.Path.ToString() });
        }
        var selectedService = catalog.FirstOrDefault(service => service.Id == model.ServiceCatalogId
            && string.Equals(service.DeviceType, model.DeviceType, StringComparison.OrdinalIgnoreCase)
            && string.Equals(service.ProblemCategory, model.ProblemCategory, StringComparison.OrdinalIgnoreCase)
            && string.Equals(service.ProblemType, model.ProblemType, StringComparison.OrdinalIgnoreCase));
        if (selectedService is null)
        {
            ModelState.AddModelError(nameof(model.ServiceCatalogId), "Select a valid active repair problem.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.ServiceCatalog = catalog;
            _logger.LogWarning("TicketsController.Create ModelState invalid. Errors: {Errors}", string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return View(model);
        }

        _logger.LogInformation("TicketsController.Create ModelState valid.");

        var requester = User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst("email")?.Value
            ?? User.Identity?.Name
            ?? string.Empty;
        var clientProfileId = GetCurrentProfileId();
        if (clientProfileId == Guid.Empty)
        {
            ModelState.AddModelError(string.Empty, "Your Client profile could not be resolved.");
            ViewBag.ServiceCatalog = catalog;
            return View(model);
        }
        _logger.LogInformation(
            "TicketsController.Create received repair request: DeviceType={DeviceType}, ProblemCategory={ProblemCategory}, ProblemType={ProblemType}, ServiceCatalogId={ServiceCatalogId}, EmployeeId={EmployeeId}, Title={Title}, Description={Description}, Priority={Priority}",
            model.DeviceType, model.ProblemCategory, model.ProblemType, model.ServiceCatalogId, clientProfileId, model.Title, model.Description, model.Priority);
        var requesterProfile = await _supabaseService.GetProfileByEmailAsync(requester, cancellationToken);
        var requesterName = !string.IsNullOrWhiteSpace(requesterProfile?.FullName) ? requesterProfile.FullName : requester;
        var ticket = new Ticket
        {
            employee_id = clientProfileId,
            device_type = model.DeviceType,
            problem_category = model.ProblemCategory,
            problem_type = model.ProblemType,
            service_catalog_id = model.ServiceCatalogId,
            title = model.Title,
            description = model.Description,
            requester = requester,
            priority = model.Priority,
            category = model.Category,
            payment_status = "Pending",
            comments = string.Empty,
            // Initial technician assignment is handled by the Supabase BEFORE INSERT trigger.
            assigned_technician = null,
            technician_id = null,
            status = "Open",
            created_at = DateTime.UtcNow,
            updated_at = DateTime.UtcNow
        };

        _logger.LogInformation(
            "TicketsController.Create building ticket payload. Requester={Requester}, Title={Title}, Category={Category}, Priority={Priority}, Status={Status}, CreatedAt={CreatedAt}",
            ticket.requester,
            ticket.title,
            ticket.category,
            ticket.priority,
            ticket.status,
            ticket.created_at);

        try
        {
            var created = await _ticketService.CreateTicketAsync(ticket, GetSupabaseAccessToken(), cancellationToken);
            if (!created)
            {
                TempData.Remove("Success");
                TempData["Error"] = "Unable to create the ticket right now.";
                _logger.LogError("TicketsController.Create CreateTicketAsync returned false for ticket title {Title}.", ticket.title);
                return View(model);
            }

            var imageFiles = model.Images.Where(file => file is not null).ToList();
            if (model.Screenshot is not null)
            {
                imageFiles.Add(model.Screenshot);
            }
            if (imageFiles.Count > 0)
            {
                try
                {
                    var uploadedAttachments = await _ticketService.UploadTicketImagesAsync(ticket.id!, imageFiles, clientProfileId, GetSupabaseAccessToken(), cancellationToken);
                    var primaryAttachment = uploadedAttachments.FirstOrDefault();
                    if (primaryAttachment is not null)
                    {
                        ticket.screenshot_url = primaryAttachment.FileUrl;
                    }
                    else
                    {
                        throw new HttpRequestException("The uploaded screenshot record could not be found.");
                    }
                }
                catch (Exception ex) when (ex is InvalidDataException or HttpRequestException or InvalidOperationException)
                {
                    _logger.LogError(ex, "Ticket image upload failed after ticket {TicketId} was created.", ticket.id);
                    TempData["Error"] = "The ticket was created, but one or more images could not be uploaded. Please try again.";
                }
            }

            var ticketLink = Url.Action("Details", "Tickets", new { id = ticket.id }) ?? $"/Tickets/Details?id={ticket.id}";
            var shortTicketId = !string.IsNullOrWhiteSpace(ticket.ticket_number)
                ? ticket.ticket_number
                : string.IsNullOrWhiteSpace(ticket.id)
                    ? "unknown"
                    : ticket.id[..Math.Min(ticket.id.Length, 8)];
            var deviceType = string.IsNullOrWhiteSpace(ticket.device_type) ? "device" : ticket.device_type.Trim();
            var problem = !string.IsNullOrWhiteSpace(ticket.problem_type)
                ? ticket.problem_type.Trim()
                : !string.IsNullOrWhiteSpace(ticket.problem_category)
                    ? ticket.problem_category.Trim()
                    : ticket.category?.Trim();
            var problemDetails = string.IsNullOrWhiteSpace(problem) ? string.Empty : $" Problem: {problem}.";
            var technicianId = ticket.technician_id.GetValueOrDefault();
            Profile? assignedTechnician = null;
            if (technicianId != Guid.Empty)
            {
                try
                {
                    assignedTechnician = await _supabaseService.GetProfileByIdAsync(technicianId, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to resolve automatically assigned technician {TechnicianId} for ticket {TicketId}.", technicianId, ticket.id);
                }
            }

            var technicianName = !string.IsNullOrWhiteSpace(assignedTechnician?.FullName)
                ? assignedTechnician.FullName
                : !string.IsNullOrWhiteSpace(assignedTechnician?.Email)
                    ? assignedTechnician.Email
                    : !string.IsNullOrWhiteSpace(ticket.assigned_technician)
                        ? ticket.assigned_technician
                        : "the assigned technician";
            var adminMessage = technicianId == Guid.Empty
                ? $"New {deviceType} ticket #{shortTicketId} was created by {requesterName}; automatic technician assignment was not returned.{problemDetails}"
                : $"New {deviceType} ticket #{shortTicketId} was created by {requesterName} and automatically assigned to {technicianName}.{problemDetails}";
            var technicianMessage = $"New {deviceType} ticket #{shortTicketId} has been automatically assigned to you.{problemDetails}";
            var notifiedProfileIds = new HashSet<Guid>();

            List<Profile> adminProfiles;
            try
            {
                adminProfiles = (await _supabaseService.GetProfilesByRoleAsync("Admin", cancellationToken))
                    .Where(profile => profile is not null && profile.Id != Guid.Empty)
                    .GroupBy(profile => profile.Id)
                    .Select(group => group.First())
                    .ToList();
            }
            catch (Exception ex)
            {
                adminProfiles = new List<Profile>();
                _logger.LogError(ex, "Failed to load Admin profiles for new ticket {TicketId} notifications.", ticket.id);
            }

            if (adminProfiles.Count == 0)
            {
                _logger.LogWarning("TicketsController.Create no admin profiles found for ticket {TicketId}.", ticket.id);
            }

            foreach (var adminProfile in adminProfiles)
            {
                if (!notifiedProfileIds.Add(adminProfile.Id))
                {
                    continue;
                }

                var isAssignedTechnician = adminProfile.Id == technicianId;
                try
                {
                    _logger.LogInformation("Creating new ticket notification for admin {AdminUserId} for ticket {TicketId}", adminProfile.Id, ticket.id);
                    await _notificationService.CreateNotificationAsync(
                        adminProfile.Id,
                        isAssignedTechnician ? "New Ticket Assigned" : "New Ticket Created",
                        isAssignedTechnician ? technicianMessage : adminMessage,
                        isAssignedTechnician ? "ticket_assigned" : "new_ticket",
                        ticketLink,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create new ticket notification for admin {AdminUserId} for ticket {TicketId}", adminProfile.Id, ticket.id);
                }
            }

            if (technicianId != Guid.Empty && notifiedProfileIds.Add(technicianId))
            {
                try
                {
                    _logger.LogInformation("Creating automatic assignment notification for technician {TechnicianId} for ticket {TicketId}", technicianId, ticket.id);
                    await _notificationService.CreateNotificationAsync(
                        technicianId,
                        "New Ticket Assigned",
                        technicianMessage,
                        "ticket_assigned",
                        ticketLink,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create automatic assignment notification for technician {TechnicianId} for ticket {TicketId}", technicianId, ticket.id);
                }
            }

            if (TempData["Error"] is not null)
            {
                return RedirectToAction(nameof(Payment), new { id = ticket.id });
            }

            TempData.Remove("Error");
            TempData["Success"] = "Ticket created successfully.";
            _logger.LogInformation("TicketsController.Create ticket inserted successfully and redirecting to My Tickets.");
            return RedirectToAction(nameof(Payment), new { id = ticket.id });
        }
        catch (Exception ex)
        {
            TempData.Remove("Success");
            TempData["Error"] = "Unable to create the ticket right now.";
            _logger.LogError(ex, "TicketsController.Create exception while creating ticket for title {Title}.", ticket.title);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id, CancellationToken cancellationToken)
    {
        var ticket = await _ticketService.GetTicketByIdAsync(id, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        if (User.IsInRole("Client") && ticket.employee_id != GetCurrentProfileId())
        {
            return Forbid();
        }

        if (User.IsInRole("Technician") && !User.IsInRole("Admin") && !IsAssignedToCurrentTechnician(ticket))
        {
            return Forbid();
        }

        var technicians = (await _supabaseService.GetTechniciansAsync(cancellationToken)).OrderBy(p => p.FullName).ThenBy(p => p.Email).ToList();
        ViewBag.Technicians = technicians;
        ViewBag.Attachments = await _ticketService.GetTicketAttachmentsAsync(id, GetSupabaseAccessToken(), cancellationToken);
        if (string.IsNullOrWhiteSpace(ticket.screenshot_url) && string.IsNullOrWhiteSpace(ticket.image_url)
            && ViewBag.Attachments is IReadOnlyList<TicketAttachment> attachments && attachments.Count > 0)
        {
            ticket.screenshot_url = attachments[0].FileUrl;
        }
        return View(ticket);
    }

    [HttpGet]
    [Authorize(Roles = "Client")]
    public async Task<IActionResult> Payment(string id, CancellationToken cancellationToken)
    {
        var ticket = await _ticketService.GetTicketByIdAsync(id, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        if (ticket.employee_id != GetCurrentProfileId())
        {
            return Forbid();
        }

        return View(ticket);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Client")]
    public async Task<IActionResult> Payment(string id, PaymentMethodViewModel model, CancellationToken cancellationToken)
    {
        var ticket = await _ticketService.GetTicketByIdAsync(id, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        if (ticket.employee_id != GetCurrentProfileId())
        {
            return Forbid();
        }

        var paymentMethod = model.PaymentMethod?.Trim();
        if (!string.Equals(paymentMethod, "Card", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(paymentMethod, "Cash", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.PaymentMethod), "Select Card or Cash.");
            return View(ticket);
        }

        ticket.payment_method = string.Equals(paymentMethod, "Card", StringComparison.OrdinalIgnoreCase) ? "Card" : "Cash";
        ticket.payment_status = "Pending";
        ticket.updated_at = DateTime.UtcNow;

        if (!await _ticketService.UpdateTicketAsync(ticket, cancellationToken))
        {
            ModelState.AddModelError(string.Empty, "Unable to save the payment method. Please try again.");
            return View(ticket);
        }

        TempData["PaymentSuccess"] = $"Payment method saved: {ticket.payment_method}";
        return RedirectToAction(nameof(Details), new { id = ticket.id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ConfirmPayment(string id, CancellationToken cancellationToken)
    {
        var ticket = await _ticketService.GetTicketByIdAsync(id, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        var hasPaymentMethod = string.Equals(ticket.payment_method, "Card", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ticket.payment_method, "Cash", StringComparison.OrdinalIgnoreCase);
        if (!hasPaymentMethod || !string.Equals(ticket.payment_status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = string.Equals(ticket.payment_status, "Paid", StringComparison.OrdinalIgnoreCase)
                ? "Payment is already confirmed."
                : "Only a pending Card or Cash payment can be confirmed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (!await _ticketService.ConfirmPaymentAsync(id, cancellationToken))
        {
            TempData["Error"] = "Unable to confirm the payment.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["PaymentSuccess"] = "Payment confirmed successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateDueDate(string id, DateTime? dueDate, bool returnToIndex = false, CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketService.GetTicketByIdAsync(id, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        ticket.due_date = dueDate;
        ticket.updated_at = DateTime.UtcNow;
        if (!await _ticketService.UpdateTicketAsync(ticket, cancellationToken))
        {
            TempData["Error"] = "Unable to update the due date.";
            return RedirectToAction(returnToIndex ? nameof(Index) : nameof(Details), new { id });
        }

        TempData["Success"] = "Due date updated successfully.";
        return RedirectToAction(returnToIndex ? nameof(Index) : nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Technician")]
    public async Task<IActionResult> UpdateStatus(string id, UpdateTicketStatusViewModel model, CancellationToken cancellationToken)
    {
        var ticket = await _ticketService.GetTicketByIdAsync(id, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        if (User.IsInRole("Technician") && !User.IsInRole("Admin") && !IsAssignedToCurrentTechnician(ticket))
        {
            return Forbid();
        }

        var normalizedStatus = NormalizeStatus(model.Status);
        if (User.IsInRole("Technician") && !User.IsInRole("Admin")
            && normalizedStatus is not ("In Progress" or "Resolved" or "Closed"))
        {
            TempData["Error"] = "Technicians may only set tickets to In Progress, Resolved, or Closed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (!CanTransition(ticket.status, normalizedStatus))
        {
            TempData["Error"] = "Status updates must follow the required workflow order.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var actor = User.Identity?.Name ?? User.FindFirst("email")?.Value ?? string.Empty;
        ticket.status = normalizedStatus;
        ticket.updated_at = DateTime.UtcNow;
        if (model.DueDate.HasValue)
        {
            ticket.due_date = model.DueDate.Value;
        }

        if (!string.IsNullOrWhiteSpace(model.Comment))
        {
            ticket.comments = string.IsNullOrWhiteSpace(ticket.comments)
                ? $"{actor}: {model.Comment}"
                : $"{ticket.comments}\n{actor}: {model.Comment}";
        }

        var updated = await _ticketService.UpdateTicketAsync(ticket, cancellationToken);
        if (!updated)
        {
            TempData["Error"] = "Unable to update the ticket.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var ticketLink = Url.Action("Details", "Tickets", new { id = ticket.id }) ?? $"/Tickets/Details?id={ticket.id}";
        var statusMessage = $"Ticket '{ticket.title}' is now {ticket.status}.";
        await _notificationService.CreateNotificationForEmailAsync(
            ticket.requester,
            "Ticket status updated",
            statusMessage,
            "ticket_status",
            ticketLink,
            cancellationToken);

        var assignedTechnician = !string.IsNullOrWhiteSpace(ticket.assigned_technician)
            ? ticket.assigned_technician
            : (ticket.technician_id.HasValue ? (await ResolveProfileAsync(ticket.technician_id.Value.ToString(), cancellationToken))?.Email : null);
        if (!string.IsNullOrWhiteSpace(assignedTechnician) && !string.Equals(assignedTechnician, ticket.requester, StringComparison.OrdinalIgnoreCase))
        {
            await _notificationService.CreateNotificationForEmailAsync(
                assignedTechnician,
                "Ticket status updated",
                statusMessage,
                "ticket_status",
                ticketLink,
                cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(model.Comment))
        {
            var commenter = User.Identity?.Name ?? User.FindFirst("email")?.Value ?? "Someone";
            var commentRecipient = !string.Equals(ticket.requester, commenter, StringComparison.OrdinalIgnoreCase)
                ? ticket.requester
                : assignedTechnician;

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

        TempData["Success"] = "Ticket status updated successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Technician,Client")]
    public async Task<IActionResult> AddComment(string id, string? comment, CancellationToken cancellationToken)
    {
        var ticket = await _ticketService.GetTicketByIdAsync(id, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        var email = User.Identity?.Name ?? User.FindFirst("email")?.Value ?? string.Empty;
        var profileId = GetCurrentProfileId();
        var canComment = User.IsInRole("Admin")
            || (User.IsInRole("Client") && ticket.employee_id == profileId)
            || (User.IsInRole("Technician") && (ticket.technician_id == profileId
                || string.Equals(ticket.assigned_technician, email, StringComparison.OrdinalIgnoreCase)));
        if (!canComment)
        {
            return Forbid();
        }

        var commentText = comment?.Trim();
        if (string.IsNullOrWhiteSpace(commentText))
        {
            TempData["Error"] = "Enter a comment before saving.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var actor = string.IsNullOrWhiteSpace(email) ? "User" : email;
        ticket.comments = string.IsNullOrWhiteSpace(ticket.comments)
            ? $"{actor}: {commentText}"
            : $"{ticket.comments}\n{actor}: {commentText}";
        ticket.updated_at = DateTime.UtcNow;

        if (!await _ticketService.UpdateTicketAsync(ticket, cancellationToken))
        {
            TempData["Error"] = "Unable to save the comment.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var ticketLink = Url.Action("Details", "Tickets", new { id = ticket.id }) ?? $"/Tickets/Details?id={ticket.id}";
        var recipient = User.IsInRole("Client")
            ? (!string.IsNullOrWhiteSpace(ticket.assigned_technician)
                ? ticket.assigned_technician
                : (ticket.technician_id.HasValue ? (await ResolveProfileAsync(ticket.technician_id.Value.ToString(), cancellationToken))?.Email : null))
            : ticket.requester;
        if (!string.IsNullOrWhiteSpace(recipient) && !string.Equals(recipient, email, StringComparison.OrdinalIgnoreCase))
        {
            await _notificationService.CreateNotificationForEmailAsync(
                recipient,
                "New comment on your ticket",
                $"{actor} added a comment to ticket '{ticket.title}'.",
                "ticket_comment",
                ticketLink,
                cancellationToken);
        }

        TempData["Success"] = "Comment added successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Client")]
    public async Task<IActionResult> ConfirmClose(string id, CancellationToken cancellationToken)
    {
        var ticket = await _ticketService.GetTicketByIdAsync(id, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        if (ticket.employee_id != GetCurrentProfileId())
        {
            return Forbid();
        }

        if (!string.Equals(ticket.status, "Resolved", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "The ticket must be marked resolved before it can be closed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        ticket.status = "Closed";
        ticket.updated_at = DateTime.UtcNow;
        var updated = await _ticketService.UpdateTicketAsync(ticket, cancellationToken);
        if (!updated)
        {
            TempData["Error"] = "Unable to close the ticket.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var ticketLink = Url.Action("Details", "Tickets", new { id = ticket.id }) ?? $"/Tickets/Details?id={ticket.id}";
        await _notificationService.CreateNotificationForEmailAsync(
            ticket.requester,
            "Ticket closed",
            $"Your ticket '{ticket.title}' has been closed.",
            "ticket_resolved",
            ticketLink,
            cancellationToken);

        TempData["Success"] = "Ticket closed successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var deleted = await _ticketService.DeleteTicketAsync(id, cancellationToken);
        if (!deleted)
        {
            TempData["Error"] = "Unable to delete the ticket.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Ticket deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<Profile?> ResolveProfileAsync(string? identifier, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return null;
        }

        if (Guid.TryParse(identifier, out var profileId))
        {
            return await _supabaseService.GetProfileByIdAsync(profileId, cancellationToken);
        }

        return await _supabaseService.GetProfileByEmailAsync(identifier, cancellationToken);
    }

    private Guid GetCurrentProfileId()
    {
        var value = User.FindFirst("profile_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var profileId) ? profileId : Guid.Empty;
    }

    private bool IsAssignedToCurrentTechnician(Ticket ticket)
    {
        var profileId = GetCurrentProfileId();
        var email = User.Identity?.Name ?? User.FindFirst("email")?.Value ?? string.Empty;
        return (profileId != Guid.Empty && ticket.technician_id == profileId)
            || (!string.IsNullOrWhiteSpace(email)
                && string.Equals(ticket.assigned_technician, email, StringComparison.OrdinalIgnoreCase));
    }

    private async Task PopulateCatalogAsync(CancellationToken cancellationToken)
    {
        var catalog = (await _supabaseService.GetActiveServiceCatalogAsync(GetSupabaseAccessToken(), cancellationToken)).ToList();
        _logger.LogInformation("TicketsController.Create catalog: Count={Count}, First={FirstCatalogItem}",
            catalog.Count, catalog.FirstOrDefault());
        ViewBag.ServiceCatalog = catalog;
    }

    private string? GetSupabaseAccessToken()
    {
        return User.FindFirst("supabase_access_token")?.Value;
    }

    private static bool CanTransition(string currentStatus, string nextStatus)
    {
        var workflow = new[]
        {
            "Open",
            "Assigned",
            "In Progress",
            "Resolved",
            "Closed"
        };

        var currentIndex = Array.IndexOf(workflow, currentStatus);
        var nextIndex = Array.IndexOf(workflow, nextStatus);

        if (currentIndex < 0 || nextIndex < 0)
        {
            return false;
        }

        return nextIndex == currentIndex + 1 || (currentIndex == 0 && nextIndex == 1) || (currentIndex == 3 && nextIndex == 4);
    }

    private static string NormalizeStatus(string status)
    {
        return status switch
        {
            "Open" => "Open",
            "Assigned" => "Assigned",
            "In Progress" => "In Progress",
            "Resolved" => "Resolved",
            "Closed" => "Closed",
            _ => status
        };
    }

}
