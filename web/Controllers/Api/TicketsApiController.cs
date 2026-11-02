// Controllers/Api/TicketsApiController.cs

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceHub_IT.DTOs.Api;
using ServiceHub_IT.Interfaces;
using ServiceHub_IT.Models;
using ServiceHub_IT.Services;

namespace ServiceHub_IT.Controllers.Api;

[ApiController]
[Route("api/tickets")]
[Authorize]
[Produces("application/json")]
public sealed class TicketsApiController : ControllerBase
{
    private readonly TicketService _ticketService;
    private readonly ILogger<TicketsApiController> _logger;
    private readonly ISupabaseService _supabaseService;
    private readonly NotificationService _notificationService;

    public TicketsApiController(
        TicketService ticketService,
        ISupabaseService supabaseService,
        NotificationService notificationService,
        ILogger<TicketsApiController> logger
        
        )
    
    {
        _ticketService = ticketService;
        _supabaseService=supabaseService;
        _notificationService = notificationService;
        _logger = logger;
        
    }

    // ────────────────────────────────────────────────────────
    // GET /api/tickets?status=&priority=
    // Role-aware: Admin=all, Technician=assigned, Client=own
    // ────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? priority,
        CancellationToken cancellationToken)
    {
        try
        {
            var email = User.Identity?.Name
                ?? User.FindFirst(ClaimTypes.Email)?.Value
                ?? string.Empty;

            IReadOnlyList<ServiceHub_IT.Models.Ticket> tickets;

            if (User.IsInRole("Admin"))
            {
                tickets = await _ticketService.GetTicketsAsync(
                    cancellationToken: cancellationToken);
            }
            else if (User.IsInRole("Technician"))
            {
                tickets = await _ticketService.GetTicketsAsync(
                    assignedTechnician: email,
                    cancellationToken: cancellationToken);
            }
            else
            {
                var profileIdClaim = User.FindFirst("profile_id")?.Value
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(profileIdClaim, out var profileId))
                {
                    return Ok(Array.Empty<object>());
                }

                tickets = await _ticketService.GetTicketsAsync(
                    employeeId: profileId,
                    cancellationToken: cancellationToken);
            }

            // Optional query-string filters
            var filtered = tickets.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                filtered = filtered.Where(t =>
                    string.Equals(t.status, status, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(priority))
            {
                filtered = filtered.Where(t =>
                    string.Equals(t.priority, priority, StringComparison.OrdinalIgnoreCase));
            }

            return Ok(filtered.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to load tickets for API.");
            return StatusCode(500, new ApiErrorDto
            {
                Error = "Unable to load tickets right now."
            });
        }
    }

    // ────────────────────────────────────────────────────────
    // GET /api/tickets/{id}
    // Returns one ticket. Clients can only see their own.
    // ────────────────────────────────────────────────────────
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(
        string id,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new ApiErrorDto { Error = "Ticket id is required." });
        }

        try
        {
            var ticket = await _ticketService.GetTicketByIdAsync(id, User.FindFirst("supabase_access_token")?.Value, cancellationToken);
            if (ticket is null)
            {
                return NotFound(new ApiErrorDto { Error = "Ticket not found." });
            }

            // ── Role-based access check ──
            if (User.IsInRole("Client"))
            {
                var profileIdClaim = User.FindFirst("profile_id")?.Value
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(profileIdClaim, out var profileId))
                {
                    return StatusCode(403, new ApiErrorDto
                    {
                        Error = "Your profile could not be resolved."
                    });
                }

                // Client can only see their own tickets
                if (ticket.employee_id != profileId)
                {
                    return StatusCode(403, new ApiErrorDto
                    {
                        Error = "You do not have access to this ticket."
                    });
                }
            }

            return Ok(ticket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to load ticket {TicketId} for API.", id);
            return StatusCode(500, new ApiErrorDto
            {
                Error = "Unable to load the ticket right now."
            });
        }
    }

    // ────────────────────────────────────────────────────────
    // POST /api/tickets
    // Multipart form: title, description, deviceType, problemCategory,
    // problemType, serviceCatalogId, priority, category, images[]
    // Client-only (role check).
    // ────────────────────────────────────────────────────────
    [HttpPost]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50 MB total
    [Authorize(Roles = "Client")]
    public async Task<IActionResult> Create(
        [FromForm] CreateTicketRequest request,
        CancellationToken cancellationToken)
    {
        // ── Basic validation ──
        if (string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.Description) ||
            string.IsNullOrWhiteSpace(request.DeviceType) ||
            string.IsNullOrWhiteSpace(request.ProblemCategory) ||
            string.IsNullOrWhiteSpace(request.ProblemType) ||
            request.ServiceCatalogId is null)
        {
            return BadRequest(new ApiErrorDto
            {
                Error = "Please fill in all required fields."
            });
        }

        // ── Resolve current user ──
        var requester = User.Identity?.Name
            ?? User.FindFirst(ClaimTypes.Email)?.Value
            ?? string.Empty;

        var profileIdClaim = User.FindFirst("profile_id")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(profileIdClaim, out var profileId))
        {
            return StatusCode(403, new ApiErrorDto
            {
                Error = "Your profile could not be resolved."
            });
        }

        var accessToken = User.FindFirst("supabase_access_token")?.Value;
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return StatusCode(401, new ApiErrorDto
            {
                Error = "Your session has expired. Please log in again."
            });
        }

        // ── Validate service catalog selection ──
        List<ServiceHub_IT.Models.ServiceCatalogItem> catalog;
        try
        {
            catalog = (await _supabaseService.GetActiveServiceCatalogAsync(
                null, cancellationToken)).ToList();
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(401, new ApiErrorDto
            {   
                Error = "Your session has expired. Please log in again."
            });
        }

        var selectedService = catalog.FirstOrDefault(s =>
            s.Id == request.ServiceCatalogId &&
            string.Equals(s.DeviceType, request.DeviceType, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(s.ProblemCategory, request.ProblemCategory, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(s.ProblemType, request.ProblemType, StringComparison.OrdinalIgnoreCase));

        if (selectedService is null)
        {
            return BadRequest(new ApiErrorDto
            {
                Error = "Select a valid active repair problem."
            });
        }

        // ── Build ticket ──
        var category = string.IsNullOrWhiteSpace(request.Category)
            ? request.ProblemCategory
            : request.Category;

        var title = string.IsNullOrWhiteSpace(request.Title)
            ? $"{request.DeviceType} repair: {request.ProblemType}"
            : request.Title;

        var ticket = new ServiceHub_IT.Models.Ticket
        {
            employee_id = profileId,
            device_type = request.DeviceType,
            problem_category = request.ProblemCategory,
            problem_type = request.ProblemType,
            service_catalog_id = request.ServiceCatalogId,
            title = title,
            description = request.Description,
            requester = requester,
            priority = string.IsNullOrWhiteSpace(request.Priority) ? "Medium" : request.Priority,
            category = category,
            payment_status = "Pending",
            comments = string.Empty,
            assigned_technician = null,
            status = "Open",
            created_at = DateTime.UtcNow,
            updated_at = DateTime.UtcNow,
        };

        // ── Insert ticket ──
        try
        {
            var created = await _ticketService.CreateTicketAsync(ticket, accessToken, cancellationToken);
            if (!created)
            {
                return StatusCode(500, new ApiErrorDto
                {
                    Error = "Unable to create the ticket right now."
                });
            }

            string? imageUploadWarning = null;

            // ── Upload images (optional) ──
            var files = (request.Images ?? new List<IFormFile>())
                .Where(f => f is not null && f.Length > 0)
                .ToList();

            if (files.Count > 0 && !string.IsNullOrWhiteSpace(ticket.id))
            {
                try
                {
                    var uploaded = await _ticketService.UploadTicketImagesAsync(
                        ticket.id, files, profileId, accessToken, cancellationToken);

                    var primary = uploaded.FirstOrDefault();
                    if (primary is not null)
                    {
                        ticket.screenshot_url = primary.FileUrl;
                    }
                }
                catch (Exception ex) when (
                    ex is InvalidDataException or
                    HttpRequestException or
                    InvalidOperationException)
                {
                    _logger.LogWarning(ex,
                        "Images failed to upload for new ticket {TicketId}.", ticket.id);

                    // Keep processing notifications: the ticket exists even if its images do not.
                    imageUploadWarning = "The ticket was created, but one or more images could not be uploaded.";
                }
            }

            // ── Notify all admins ──
            try
            {
                //Get requester's name for the message
                var requesterProfile = await _supabaseService.GetProfileByEmailAsync(requester, cancellationToken);
                var requesterName = !string.IsNullOrWhiteSpace(requesterProfile?.FullName)
                ? requesterProfile.FullName
                : requester;

                var ticketLink = $"/Tickets/Details?id={Uri.EscapeDataString(ticket.id ?? string.Empty)}";

                var adminProfiles = (await _supabaseService.GetProfilesByRoleAsync("Admin", cancellationToken))
                .Where(profile => profile is not null && profile.Id != Guid.Empty)
                .GroupBy(profile => profile.Id)
                .Select(group => group.First())
                .ToList();

                if (adminProfiles.Count == 0)
                {
                    _logger.LogWarning("No admin profiles found to notify for ticket {TicketId}.", ticket.id);
                }
                else
                {
                    foreach (var adminProfile in adminProfiles)
                    {
                        try
                        {
                            await _notificationService.CreateNotificationAsync(
                                adminProfile.Id,
                                "New Support Ticket",
                                $"A new support ticket '{ticket.title}' has been created by {requesterName}",
                                "new_ticket",
                                ticketLink,
                                cancellationToken
                            );

                            _logger.LogInformation(
                                "Notification created for admin {AdminId} for ticket {TicketId.}",
                                adminProfile.Id, ticket.id
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, 
                            "Failed to create notification for admin {AdminId} for ticket {TicketId}", adminProfile.Id, ticket.id
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notification creation failed for ticket {TicketId}.", ticket.id);
            }

            return imageUploadWarning is null
                ? StatusCode(201, ticket)
                : StatusCode(201, new { ticket, warning = imageUploadWarning });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Supabase insert failed for new ticket.");
            return StatusCode(500, new ApiErrorDto
            {
                Error = "Unable to create the ticket right now. Please try again."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating ticket.");
            return StatusCode(500, new ApiErrorDto
            {
                Error = "Unable to create the ticket right now."
            });
        }
    }

    // ────────────────────────────────────────────────────────
// PATCH /api/tickets/{id}/status
// Updates status, assigned technician, due date, and appends a comment.
// Admin + Technician only.
// Enforces the workflow state machine (Open → Assigned → In Progress → Resolved → Closed).
// ────────────────────────────────────────────────────────
[HttpPatch("{id}/status")]
[Authorize(Roles = "Admin,Technician")]
public async Task<IActionResult> UpdateStatus(
    string id,
    [FromBody] UpdateTicketStatusRequest request,
    CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(id))
    {
        return BadRequest(new ApiErrorDto { Error = "Ticket id is required." });
    }

    if (request is null || string.IsNullOrWhiteSpace(request.Status))
    {
        return BadRequest(new ApiErrorDto { Error = "Status is required." });
    }

        if ((request.AssignmentSubmitted || request.AssignedTechnicianId.HasValue) && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

    try
    {
        // ── Load ticket ──
        var ticket = await _ticketService.GetTicketByIdAsync(id, User.FindFirst("supabase_access_token")?.Value, cancellationToken);
        if (ticket is null)
        {
            return NotFound(new ApiErrorDto { Error = "Ticket not found." });
        }

        // ── Role check for technicians ──
        // Technicians can only update tickets assigned to them.
        // Admins can update anything.
        if (User.IsInRole("Technician") && !User.IsInRole("Admin"))
        {
            var currentUserEmail = User.Identity?.Name
                ?? User.FindFirst(ClaimTypes.Email)?.Value
                ?? string.Empty;

            var isAssigned = string.Equals(
                ticket.assigned_technician,
                currentUserEmail,
                StringComparison.OrdinalIgnoreCase);

            // Allow if unassigned (they're taking it) OR already assigned to them
            var isUnassigned = string.IsNullOrWhiteSpace(ticket.assigned_technician);

            if (!isAssigned && !isUnassigned)
            {
                return StatusCode(403, new ApiErrorDto
                {
                    Error = "You can only update tickets assigned to you."
                });
            }
        }

        // ── Validate workflow transition ──
        var normalizedNextStatus = NormalizeStatus(request.Status);

        // ── Apply updates ──
        var actor = User.Identity?.Name
            ?? User.FindFirst(ClaimTypes.Email)?.Value
            ?? "Someone";

        // Optional assignment changes are admin-only; technicians may only self-assign an unassigned ticket.
        var assignmentChanged = false;
        Profile? assignedTechnicianProfile = null;
        if (request.AssignmentSubmitted || request.AssignedTechnicianId.HasValue)
        {
            if (!request.AssignedTechnicianId.HasValue)
            {
                assignmentChanged = ticket.technician_id.HasValue || !string.IsNullOrWhiteSpace(ticket.assigned_technician);
                ticket.technician_id = null;
                ticket.assigned_technician = null;
            }
            else
            {
                assignedTechnicianProfile = await _supabaseService.GetProfileByIdAsync(
                    request.AssignedTechnicianId.Value,
                    User.FindFirst("supabase_access_token")?.Value ?? string.Empty,
                    cancellationToken);
                if (assignedTechnicianProfile is null || assignedTechnicianProfile.Id == Guid.Empty
                    || !string.Equals(assignedTechnicianProfile.Role, "Technician", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new ApiErrorDto { Error = "The selected technician could not be found." });
                }

                assignmentChanged = ticket.technician_id != assignedTechnicianProfile.Id
                    || !string.Equals(ticket.assigned_technician, assignedTechnicianProfile.Email, StringComparison.OrdinalIgnoreCase);
                ticket.technician_id = assignedTechnicianProfile.Id;
                ticket.assigned_technician = assignedTechnicianProfile.Email;
            }
        }
        else if (normalizedNextStatus == "Assigned"
                 && string.IsNullOrWhiteSpace(ticket.assigned_technician)
                 && User.IsInRole("Technician"))
        {
            var profileIdClaim = User.FindFirst("profile_id")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            assignedTechnicianProfile = Guid.TryParse(profileIdClaim, out var currentTechnicianId)
                ? await _supabaseService.GetProfileByIdAsync(
                    currentTechnicianId,
                    User.FindFirst("supabase_access_token")?.Value ?? string.Empty,
                    cancellationToken)
                : null;
            if (assignedTechnicianProfile is not null && assignedTechnicianProfile.Id != Guid.Empty)
            {
                ticket.technician_id = assignedTechnicianProfile.Id;
                ticket.assigned_technician = assignedTechnicianProfile.Email;
                assignmentChanged = true;
            }
        }

        if (assignmentChanged && assignedTechnicianProfile is not null
            && string.Equals(ticket.status, "Open", StringComparison.OrdinalIgnoreCase)
            && string.Equals(normalizedNextStatus, ticket.status, StringComparison.OrdinalIgnoreCase))
        {
            normalizedNextStatus = "Assigned";
        }

        var statusUnchanged = string.Equals(ticket.status, normalizedNextStatus, StringComparison.OrdinalIgnoreCase);
        if (!CanTransition(ticket.status, normalizedNextStatus) && !(assignmentChanged && statusUnchanged))
        {
            return BadRequest(new ApiErrorDto
            {
                Error = $"Cannot move from '{ticket.status}' to '{normalizedNextStatus}'. " +
                        "Status must advance one step in order: " +
                        "Open → Assigned → In Progress → Resolved → Closed."
            });
        }

        ticket.status = normalizedNextStatus;
        ticket.updated_at = DateTime.UtcNow;

        // Optional: due date
        if (request.DueDate.HasValue)
        {
            ticket.due_date = request.DueDate.Value;
        }

        // Optional: comment (appended to existing comments)
        if (!string.IsNullOrWhiteSpace(request.Comment))
        {
            var trimmedComment = request.Comment.Trim();
            ticket.comments = string.IsNullOrWhiteSpace(ticket.comments)
                ? $"{actor}: {trimmedComment}"
                : $"{ticket.comments}\n{actor}: {trimmedComment}";
        }

        // Auto-set closed_at when transitioning to Closed
        if (normalizedNextStatus == "Closed")
        {
            ticket.closed_at = DateTime.UtcNow;
        }

        // ── Persist ──
        var updated = await _ticketService.UpdateTicketAsync(ticket, User.FindFirst("supabase_access_token")?.Value, cancellationToken);
        if (!updated)
        {
            return StatusCode(500, new ApiErrorDto
            {
                Error = "Unable to update the ticket right now."
            });
        }

        _logger.LogInformation(
            "Ticket {TicketId} status updated from {From} to {To} by {Actor}.",
            id, ticket.status, normalizedNextStatus, actor);

            // _____________________________________________________________
            // Notifications (mirrors MVC TicketsController.UpdateStatus)
            // _____________________________________________________________

            try
            {
                var ticketLink = $"/Tickets/Details?id={Uri.EscapeDataString(ticket.id ?? string.Empty)}";
                var statusMessage = $"Ticket '{ticket.title}' is now {ticket.status}";

                if (assignmentChanged && assignedTechnicianProfile is not null)
                {
                    await _notificationService.CreateNotificationAsync(
                        assignedTechnicianProfile.Id,
                        "Ticket Assigned",
                        $"Ticket '{ticket.title}' has been assigned to you.",
                        "ticket_assigned",
                        ticketLink,
                        cancellationToken);
                }

                //notify the requester (unless they are the one updating)
                if(!string.IsNullOrWhiteSpace(ticket.requester) && !string.Equals(ticket.requester, actor, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        await _notificationService.CreateNotificationForEmailAsync(
                            ticket.requester,
                            "Ticket status updated",
                            statusMessage,
                            "ticket_status",
                            ticketLink,
                            cancellationToken
                        );
                    }
                    catch(Exception ex)
                    {
                        _logger.LogWarning(ex,
                        "Failed to notify technician {Tech} for ticket {TicketId}.", ticket.assigned_technician, ticket.id);
                    }
                }

                // If a comment was added, notify the "other party"
                if (!string.IsNullOrWhiteSpace(request.Comment))
                {
                    var commentRecipient = !string.Equals(ticket.requester, actor, StringComparison.OrdinalIgnoreCase)
                    ? ticket.requester
                    : ticket.assigned_technician;

                    if(!string.IsNullOrWhiteSpace(commentRecipient) && 
                    !string.Equals(commentRecipient, actor, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            await _notificationService.CreateNotificationForEmailAsync(
                                commentRecipient,
                                "New comment on your ticket",
                                $"{actor} added a comment to ticket '{ticket.title}'.",
                                "ticket_comment",
                                ticketLink,
                                cancellationToken
                            );
                        }
                        catch(Exception ex)
                        {
                            _logger.LogWarning(ex,
                            "Failed to notify comment recipient {Recipient} for ticket {TicketId}.", commentRecipient, ticket.id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Never fail the request if notification fail
                _logger.LogError(ex,
                "Notification dispatch failed for ticket {TicketId} status update.", ticket.id);
            }

        // Return the updated ticket
        var refreshed = await _ticketService.GetTicketByIdAsync(id, User.FindFirst("supabase_access_token")?.Value, cancellationToken);
        return Ok(refreshed ?? ticket);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unable to update ticket {TicketId} status.", id);
        return StatusCode(500, new ApiErrorDto
        {
            Error = "Unable to update the ticket right now."
        });
    }
}

// ────────────────────────────────────────────────────────
// PATCH /api/tickets/{id}/payment-method
// Client selects the payment method for their own ticket.
// Body: { "paymentMethod": "Card" | "Cash" }
// ────────────────────────────────────────────────────────
[HttpPatch("{id}/payment-method")]
[Authorize(Roles = "Client")]
public async Task<IActionResult> SetPaymentMethod(
    string id,
    [FromBody] SetPaymentMethodRequest request,
    CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(id))
    {
        return BadRequest(new ApiErrorDto { Error = "Ticket id is required." });
    }

    var method = request.PaymentMethod?.Trim();
    if (!string.Equals(method, "Card", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(method, "Cash", StringComparison.OrdinalIgnoreCase))
    {
        return BadRequest(new ApiErrorDto
        {
            Error = "Select Card or Cash."
        });
    }

    try
    {
        var ticket = await _ticketService.GetTicketByIdAsync(id, User.FindFirst("supabase_access_token")?.Value, cancellationToken);
        if (ticket is null)
        {
            return NotFound(new ApiErrorDto { Error = "Ticket not found." });
        }

        // Client must own the ticket
        var profileIdClaim = User.FindFirst("profile_id")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(profileIdClaim, out var profileId) ||
            ticket.employee_id != profileId)
        {
            return StatusCode(403, new ApiErrorDto
            {
                Error = "You can only update payment for your own tickets."
            });
        }

        // Can't change once closed
        if (string.Equals(ticket.status, "Closed", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ApiErrorDto
            {
                Error = "Cannot change payment method on a closed ticket."
            });
        }

        // Apply
        ticket.payment_method = string.Equals(method, "Card", StringComparison.OrdinalIgnoreCase)
            ? "Card"
            : "Cash";
        ticket.payment_status = "Pending";
        ticket.updated_at = DateTime.UtcNow;

        var updated = await _ticketService.UpdateTicketAsync(ticket, User.FindFirst("supabase_access_token")?.Value, cancellationToken);
        if (!updated)
        {
            return StatusCode(500, new ApiErrorDto
            {
                Error = "Unable to save the payment method right now."
            });
        }

        var refreshed = await _ticketService.GetTicketByIdAsync(id, User.FindFirst("supabase_access_token")?.Value, cancellationToken);
        return Ok(refreshed ?? ticket);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unable to set payment method for ticket {TicketId}.", id);
        return StatusCode(500, new ApiErrorDto
        {
            Error = "Unable to save the payment method right now."
        });
    }
}

// ────────────────────────────────────────────────────────
// POST /api/tickets/{id}/confirm-payment
// Admin confirms a pending payment.
// ────────────────────────────────────────────────────────
[HttpPost("{id}/confirm-payment")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> ConfirmPayment(
    string id,
    CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(id))
    {
        return BadRequest(new ApiErrorDto { Error = "Ticket id is required." });
    }

    try
    {
        var ticket = await _ticketService.GetTicketByIdAsync(id, User.FindFirst("supabase_access_token")?.Value, cancellationToken);
        if (ticket is null)
        {
            return NotFound(new ApiErrorDto { Error = "Ticket not found." });
        }

        var hasMethod =
            string.Equals(ticket.payment_method, "Card", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(ticket.payment_method, "Cash", StringComparison.OrdinalIgnoreCase);

        if (!hasMethod)
        {
            return BadRequest(new ApiErrorDto
            {
                Error = "The client must select a payment method first."
            });
        }

        if (!string.Equals(ticket.payment_status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ApiErrorDto
            {
                Error = string.Equals(ticket.payment_status, "Paid", StringComparison.OrdinalIgnoreCase)
                    ? "Payment is already confirmed."
                    : "Only pending payments can be confirmed."
            });
        }

        var confirmed = await _ticketService.ConfirmPaymentAsync(id, cancellationToken);
        if (!confirmed)
        {
            return StatusCode(500, new ApiErrorDto
            {
                Error = "Unable to confirm the payment right now."
            });
        }

        var refreshed = await _ticketService.GetTicketByIdAsync(id, User.FindFirst("supabase_access_token")?.Value, cancellationToken);
        return Ok(refreshed ?? ticket);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unable to confirm payment for ticket {TicketId}.", id);
        return StatusCode(500, new ApiErrorDto
        {
            Error = "Unable to confirm the payment right now."
        });
    }
}

// ────────────────────────────────────────────────────────
// Workflow helpers (mirrors MVC TicketsController logic)
// ────────────────────────────────────────────────────────
private static bool CanTransition(string? currentStatus, string nextStatus)
{
    if (string.IsNullOrWhiteSpace(currentStatus))
    {
        return false;
    }

    var workflow = new[] { "Open", "Assigned", "In Progress", "Resolved", "Closed" };

    var currentIndex = Array.IndexOf(workflow, currentStatus);
    var nextIndex = Array.IndexOf(workflow, nextStatus);

    if (currentIndex < 0 || nextIndex < 0)
    {
        return false;
    }

    // Allow: advance one step, or same-status no-op (idempotent update)
    if (nextIndex == currentIndex)
    {
        return true;
    }

    return nextIndex == currentIndex + 1
        || (currentIndex == 0 && nextIndex == 1)   // Open → Assigned
        || (currentIndex == 3 && nextIndex == 4);  // Resolved → Closed
}

private static string NormalizeStatus(string status)
{
    return status?.Trim() switch
    {
        "Open" => "Open",
        "Assigned" => "Assigned",
        "In Progress" => "In Progress",
        "Resolved" => "Resolved",
        "Closed" => "Closed",
        _ => status?.Trim() ?? string.Empty,
    };
}
}
