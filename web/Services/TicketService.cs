using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using ServiceHub_IT.Models;

namespace ServiceHub_IT.Services;

public class TicketService
{
    private readonly HttpClient _httpClient;
    private readonly SupabaseSettings _settings;
    private readonly ILogger<TicketService> _logger;
    private readonly ImageStorageService _imageStorageService;
    private readonly NotificationService _notificationService;

    public TicketService(
        HttpClient httpClient,
        IOptions<SupabaseSettings> options,
        ILogger<TicketService> logger,
        ImageStorageService imageStorageService,
        NotificationService notificationService)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;
        _imageStorageService = imageStorageService;
        _notificationService = notificationService;
    }

    private static Ticket NormalizeTicket(Ticket ticket)
    {
        ticket.priority = string.IsNullOrWhiteSpace(ticket.priority) ? "Medium" : ticket.priority.Trim();
        ticket.status = string.IsNullOrWhiteSpace(ticket.status) ? "Open" : ticket.status.Trim();
        return ticket;
    }

    private static IReadOnlyList<Ticket> NormalizeTickets(IReadOnlyList<Ticket> tickets)
    {
        return tickets.Select(NormalizeTicket).ToList();
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url, string? accessToken = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Clear();
        request.Headers.Add("apikey", _settings.ApiKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", string.IsNullOrWhiteSpace(accessToken) ? _settings.ApiKey : accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    public async Task<IReadOnlyList<Ticket>> GetTicketsAsync(
        string? requester = null,
        string? assignedTechnician = null,
        Guid? employeeId = null,
        Guid? technicianId = null,
        string? accessToken = null,
        CancellationToken cancellationToken = default,
        bool throwOnError = false)
    {
        if (string.IsNullOrWhiteSpace(_settings.Url) || string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            if (throwOnError)
            {
                throw new InvalidOperationException("Supabase is not configured for ticket reads.");
            }
            return Array.Empty<Ticket>();
        }

        try
        {
            var query = new List<string> { "select=*" };
            if (!string.IsNullOrWhiteSpace(requester))
            {
                query.Add($"requester=eq.{Uri.EscapeDataString(requester)}");
            }
            if (technicianId.HasValue && technicianId.Value != Guid.Empty &&
                !string.IsNullOrWhiteSpace(assignedTechnician))
            {
                var technicianOrFilter =
                    $"(technician_id.eq.{technicianId.Value},assigned_technician.eq.{assignedTechnician})";
                query.Add($"or={Uri.EscapeDataString(technicianOrFilter)}");
            }
            else if (!string.IsNullOrWhiteSpace(assignedTechnician))
            {
                query.Add($"assigned_technician=eq.{Uri.EscapeDataString(assignedTechnician)}");
            }
            if ((!technicianId.HasValue || technicianId.Value == Guid.Empty || string.IsNullOrWhiteSpace(assignedTechnician)) &&
                technicianId.HasValue && technicianId.Value != Guid.Empty)
            {
                query.Add($"technician_id=eq.{Uri.EscapeDataString(technicianId.Value.ToString())}");
            }
            if (employeeId.HasValue && employeeId.Value != Guid.Empty)
            {
                query.Add($"employee_id=eq.{Uri.EscapeDataString(employeeId.Value.ToString())}");
            }

            var endpoint = $"{_settings.Url.TrimEnd('/')}/rest/v1/tickets?{string.Join('&', query)}";
            
            using var request = CreateRequest(HttpMethod.Get, endpoint, accessToken);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                if (throwOnError)
                {
                    throw new HttpRequestException(
                        "Supabase rejected the ticket list request.", null, response.StatusCode);
                }
                return Array.Empty<Ticket>();
            }

            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            var tickets = JsonSerializer.Deserialize<List<Ticket>>(payload, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<Ticket>();

            return NormalizeTickets(tickets);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to load tickets from Supabase.");
            if (throwOnError)
            {
                throw;
            }
            return Array.Empty<Ticket>();
        }
    }

    public async Task<IReadOnlyList<Ticket>> GetOverdueTicketsAsync(
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.Url) ||
            string.IsNullOrWhiteSpace(_settings.ApiKey) ||
            string.IsNullOrWhiteSpace(_settings.ServiceRoleKey))
        {
            throw new InvalidOperationException(
                "Supabase URL, API key, and ServiceRoleKey are required for overdue ticket checks.");
        }

        const string statusFilter = "not.in.(Resolved,Closed)";
        var endpoint = $"{_settings.Url.TrimEnd('/')}/rest/v1/tickets?select=*&status={Uri.EscapeDataString(statusFilter)}";
        using var request = CreateRequest(HttpMethod.Get, endpoint, _settings.ServiceRoleKey);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Supabase overdue ticket query failed with status {(int)response.StatusCode}: {body}",
                null,
                response.StatusCode);
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        var tickets = JsonSerializer.Deserialize<List<Ticket>>(
            payload,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Ticket>();

        return NormalizeTickets(tickets);
    }

    public Task<Ticket?> GetTicketAsync(string id, CancellationToken cancellationToken = default)
    {
        return GetTicketAsync(id, null, cancellationToken);
    }

    public async Task<Ticket?> GetTicketAsync(
        string id,
        string? accessToken = null,
        CancellationToken cancellationToken = default,
        bool throwOnError = false)
    {
        if (string.IsNullOrWhiteSpace(_settings.Url) || string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            if (throwOnError)
            {
                throw new InvalidOperationException("Supabase is not configured for ticket reads.");
            }
            return null;
        }

        try
        {
            var endpoint = $"{_settings.Url.TrimEnd('/')}/rest/v1/tickets?id=eq.{Uri.EscapeDataString(id)}&select=*";
            using var request = CreateRequest(HttpMethod.Get, endpoint, accessToken);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                if (throwOnError)
                {
                    throw new HttpRequestException(
                        "Supabase rejected the ticket lookup request.", null, response.StatusCode);
                }
                return null;
            }

            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            var ticket = JsonSerializer.Deserialize<List<Ticket>>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?.FirstOrDefault();
            return ticket is null ? null : NormalizeTicket(ticket);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to load ticket from Supabase.");
            if (throwOnError)
            {
                throw;
            }
            return null;
        }
    }

    public Task<Ticket?> GetTicketByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return GetTicketByIdAsync(id, null, cancellationToken);
    }

    public Task<Ticket?> GetTicketByIdAsync(
        string id,
        string? accessToken = null,
        CancellationToken cancellationToken = default,
        bool throwOnError = false)
    {
        return GetTicketAsync(id, accessToken, cancellationToken, throwOnError);
    }

    public async Task<IReadOnlyList<Ticket>> GetEmployeeTicketsAsync(string requester, CancellationToken cancellationToken = default)
    {
        return await GetTicketsAsync(requester: requester, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<Ticket>> GetTechnicianTicketsAsync(Guid technicianId, string assignedTechnician, string? accessToken = null, CancellationToken cancellationToken = default)
    {
        if (technicianId != Guid.Empty && !string.IsNullOrWhiteSpace(assignedTechnician))
        {
            // Match either field so legacy rows with only one assignment column are included.
            return await GetTicketsAsync(
                assignedTechnician: assignedTechnician,
                technicianId: technicianId,
                accessToken: accessToken,
                cancellationToken: cancellationToken);
        }

        if (technicianId != Guid.Empty)
        {
            return await GetTicketsAsync(
                technicianId: technicianId,
                accessToken: accessToken,
                cancellationToken: cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(assignedTechnician))
        {
            return await GetTicketsAsync(
                assignedTechnician: assignedTechnician,
                accessToken: accessToken,
                cancellationToken: cancellationToken);
        }

        return Array.Empty<Ticket>();
    }

    public async Task<bool> CreateTicketAsync(Ticket ticket, string? accessToken = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_settings.Url) || !Uri.TryCreate(_settings.Url, UriKind.Absolute, out var baseUri))
            {
                throw new InvalidOperationException($"Supabase Url is not configured or is not an absolute URI. Value: '{_settings.Url ?? string.Empty}'");
            }

            NormalizeTicket(ticket);
            var alreadyAssigned = ticket.technician_id.HasValue;
            await AssignTechnicianByDeviceTypeAsync(ticket, accessToken, cancellationToken);
            var wasAutoAssigned = !alreadyAssigned && ticket.technician_id.HasValue;

            _logger.LogInformation(
                "Inserting ticket for device type {DeviceType} with assigned technician ID {TechnicianId} and email {TechnicianEmail}.",
                ticket.device_type,
                ticket.technician_id,
                ticket.assigned_technician);

            if (string.IsNullOrWhiteSpace(ticket.id))
            {
                ticket.id = null;
            }

            var endpoint = $"{baseUri.Scheme}://{baseUri.Host}{baseUri.AbsolutePath.TrimEnd('/')}/rest/v1/tickets";
            var serializerOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            var json = JsonSerializer.Serialize(ticket, serializerOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = CreateRequest(HttpMethod.Post, endpoint, accessToken);
            request.Headers.Add("Prefer", "return=representation");
            request.Content = content;
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation("TicketService.CreateTicketAsync Supabase response status: {StatusCode}. Response body: {ResponseBody}", response.StatusCode, responseBody);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("TicketService.CreateTicketAsync Supabase insert failed. Status: {StatusCode}. Response: {ResponseBody}", response.StatusCode, responseBody);
                throw new HttpRequestException($"Supabase ticket insert failed with status {(int)response.StatusCode}: {responseBody}");
            }

            Ticket? insertedTicket = null;
            try
            {
                var inserted = JsonSerializer.Deserialize<List<Ticket>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (inserted is { Count: > 0 })
                {
                    insertedTicket = inserted[0];
                    if (response.StatusCode == System.Net.HttpStatusCode.Created)
                    {
                        _logger.LogInformation(
                            "Supabase created ticket {TicketId} with technician_id {TechnicianId} and assigned_technician {TechnicianEmail}.",
                            insertedTicket.id,
                            insertedTicket.technician_id,
                            insertedTicket.assigned_technician);
                    }

                    if (ticket.technician_id != insertedTicket.technician_id ||
                        !string.Equals(ticket.assigned_technician, insertedTicket.assigned_technician, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning(
                            "Ticket assignment returned by Supabase differs from the pre-insert assignment. IntendedId={IntendedId}, ReturnedId={ReturnedId}, IntendedEmail={IntendedEmail}, ReturnedEmail={ReturnedEmail}.",
                            ticket.technician_id,
                            insertedTicket.technician_id,
                            ticket.assigned_technician,
                            insertedTicket.assigned_technician);
                    }
                    if (!string.IsNullOrWhiteSpace(insertedTicket.id))
                    {
                        ticket.id = insertedTicket.id;
                    }

                    ticket.ticket_number = insertedTicket.ticket_number;
                    if (insertedTicket.technician_id.HasValue)
                    {
                        ticket.technician_id = insertedTicket.technician_id;
                    }
                    if (!string.IsNullOrWhiteSpace(insertedTicket.assigned_technician))
                    {
                        ticket.assigned_technician = insertedTicket.assigned_technician;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to parse inserted ticket response from Supabase.");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Created &&
                wasAutoAssigned && ticket.technician_id is { } assignedTechnicianId &&
                assignedTechnicianId != Guid.Empty)
            {
                try
                {
                    await _notificationService.CreateNotificationAsync(
                        assignedTechnicianId,
                        "New Ticket Assigned",
                        $"You have been auto-assigned a new {ticket.device_type ?? "support"} ticket.",
                        "ticket_assigned",
                        $"/Tickets/Details?id={Uri.EscapeDataString(insertedTicket?.id ?? ticket.id ?? string.Empty)}",
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Ticket {TicketId} was created, but assignment notification failed for technician {TechnicianId}.",
                        insertedTicket?.id ?? ticket.id,
                        assignedTechnicianId);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TicketService.CreateTicketAsync exception while inserting into Supabase tickets table. Exception message: {Message}", ex.Message);
            throw;
        }
    }

    private async Task AssignTechnicianByDeviceTypeAsync(
        Ticket ticket,
        string? accessToken,
        CancellationToken cancellationToken)
    {
        if (ticket.technician_id.HasValue)
        {
            return;
        }

        var email = ticket.device_type?.Trim().ToLowerInvariant() switch
        {
            "printer" => "tech@servicehub.com",
            "tv" => "sarah.tech@servicehub.com",
            "laptop" or "cellphone" => "michael.tech@servicehub.com",
            _ => null,
        };

        if (email is null)
        {
            return;
        }

        // The lookup runs on the server; prefer the server credential so profile RLS
        // does not prevent a client from resolving the configured technician.
        var lookupToken = !string.IsNullOrWhiteSpace(_settings.ServiceRoleKey)
            ? _settings.ServiceRoleKey
            : accessToken;
        if (string.IsNullOrWhiteSpace(lookupToken))
        {
            _logger.LogWarning("No Supabase credential is available to resolve auto-assigned technician {Email}.", email);
            return;
        }

        var endpoint = $"{_settings.Url.TrimEnd('/')}/rest/v1/profiles?email=eq.{Uri.EscapeDataString(email)}&role=eq.Technician&select=id,email&limit=1";
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Add("apikey", _settings.ApiKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", lookupToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Unable to resolve auto-assigned technician {Email}. Supabase returned {StatusCode}.",
                email,
                response.StatusCode);
            return;
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        var profile = JsonSerializer.Deserialize<List<Profile>>(
                payload,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?
            .FirstOrDefault();

        if (profile is null || profile.Id == Guid.Empty)
        {
            _logger.LogWarning("No technician profile was found for auto-assignment email {Email}.", email);
            return;
        }

        ticket.technician_id = profile.Id;
        ticket.assigned_technician = profile.Email;
        if (string.Equals(ticket.status, "Open", StringComparison.OrdinalIgnoreCase))
        {
            ticket.status = "Assigned";
        }
        _logger.LogInformation(
            "Auto-assigned ticket for device type {DeviceType} to technician {TechnicianId} and set status to {Status}.",
            ticket.device_type,
            profile.Id,
            ticket.status);
    }

    public Task<bool> UpdateTicketAsync(Ticket ticket, CancellationToken cancellationToken = default) =>
        UpdateTicketAsync(ticket, accessToken: null, cancellationToken);

    public async Task<bool> UpdateTicketAsync(Ticket ticket, string? accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            NormalizeTicket(ticket);

            var payload = new Dictionary<string, object?>
            {
                ["status"] = ticket.status,
                ["technician_id"] = ticket.technician_id,
                ["assigned_technician"] = ticket.assigned_technician,
                ["due_date"] = ticket.due_date,
                ["comments"] = ticket.comments,
                ["screenshot_url"] = ticket.screenshot_url,
                ["payment_method"] = ticket.payment_method,
                ["payment_status"] = ticket.payment_status,
                ["updated_at"] = ticket.updated_at ?? DateTime.UtcNow
            };
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var endpoint = $"{_settings.Url.TrimEnd('/')}/rest/v1/tickets?id=eq.{Uri.EscapeDataString(ticket.id!)}";
            
            using var request = CreateRequest(HttpMethod.Patch, endpoint, GetWriteBearerToken(accessToken));
            request.Headers.Add("Prefer", "return=representation");
            request.Content = content;
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to update ticket in Supabase.");
            return false;
        }
    }

    public async Task<IReadOnlyList<TicketAttachment>> GetTicketAttachmentsAsync(string ticketId, string? accessToken = null, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(ticketId, out var parsedTicketId))
        {
            return Array.Empty<TicketAttachment>();
        }

        try
        {
            var endpoint = $"{_settings.Url.TrimEnd('/')}/rest/v1/ticket_attachments?ticket_id=eq.{parsedTicketId}&select=*&order=created_at.asc";
            
            using var request = CreateRequest(HttpMethod.Get, endpoint, accessToken);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Unable to load attachments for ticket {TicketId}. Status={StatusCode}", ticketId, response.StatusCode);
                return Array.Empty<TicketAttachment>();
            }

            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<TicketAttachment>>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new List<TicketAttachment>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to load attachments for ticket {TicketId}.", ticketId);
            return Array.Empty<TicketAttachment>();
        }
    }

    public async Task<IReadOnlyList<TicketAttachment>> UploadTicketImagesAsync(string ticketId, IEnumerable<IFormFile> files, Guid uploadedBy, string? accessToken, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(ticketId, out var parsedTicketId) || uploadedBy == Guid.Empty || string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("The ticket or authenticated user could not be resolved for image upload.");
        }

        var uploadedAttachments = new List<TicketAttachment>();
        foreach (var file in files.Where(file => file is not null))
        {
            var path = $"tickets/{parsedTicketId}/{ImageStorageService.CreateObjectName(file)}";
            var fileUrl = await _imageStorageService.UploadAsync(file, path, accessToken, cancellationToken);
            var attachment = new TicketAttachment
            {
                Id = Guid.NewGuid(),
                TicketId = parsedTicketId,
                FileUrl = fileUrl,
                FileName = Path.GetFileName(file.FileName),
                FileType = file.ContentType,
                FileSize = file.Length,
                UploadedBy = uploadedBy,
                CreatedAt = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(attachment);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var endpoint = $"{_settings.Url.TrimEnd('/')}/rest/v1/ticket_attachments";
            
            using var request = CreateRequest(HttpMethod.Post, endpoint, GetWriteBearerToken(accessToken));
            request.Headers.Add("Prefer", "return=representation");
            request.Content = content;
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Attachment metadata insert failed. Status={StatusCode}, Response={Response}", response.StatusCode, responseBody);
                throw new HttpRequestException("The ticket attachment metadata could not be saved.");
            }

            uploadedAttachments.Add(attachment);
            if (uploadedAttachments.Count == 1 && !await UpdateTicketScreenshotAsync(ticketId, attachment.FileUrl, accessToken, cancellationToken))
            {
                throw new HttpRequestException("The screenshot reference could not be saved to the ticket.");
            }
        }

        return uploadedAttachments;
    }

    public async Task<bool> UpdateTicketScreenshotAsync(string ticketId, string screenshotUrl, string? accessToken = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ticketId) || string.IsNullOrWhiteSpace(screenshotUrl))
        {
            return false;
        }

        try
        {
            var json = JsonSerializer.Serialize(new { screenshot_url = screenshotUrl });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var endpoint = $"{_settings.Url.TrimEnd('/')}/rest/v1/tickets?id=eq.{Uri.EscapeDataString(ticketId)}";
            
            using var request = CreateRequest(HttpMethod.Patch, endpoint, GetWriteBearerToken(accessToken));
            request.Headers.Add("Prefer", "return=representation");
            request.Content = content;
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Unable to save primary screenshot for ticket {TicketId}. Status={StatusCode}, Response={Response}", ticketId, response.StatusCode, responseBody);
                return false;
            }

            var updatedTickets = JsonSerializer.Deserialize<List<Ticket>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return updatedTickets is { Count: 1 };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to save primary screenshot for ticket {TicketId}.", ticketId);
            return false;
        }
    }

    private string GetWriteBearerToken(string? accessToken)
    {
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            return accessToken;
        }

        if (!string.IsNullOrWhiteSpace(_settings.ServiceRoleKey))
        {
            return _settings.ServiceRoleKey;
        }

        throw new InvalidOperationException("No authenticated Supabase write credential is configured.");
    }

    public async Task<bool> ConfirmPaymentAsync(
        string ticketId,
        CancellationToken cancellationToken = default,
        string? accessToken = null)
    {
        try
        {
            var json = JsonSerializer.Serialize(new { payment_status = "Paid" });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var endpoint = $"{_settings.Url.TrimEnd('/')}/rest/v1/tickets?id=eq.{Uri.EscapeDataString(ticketId)}";

            using var request = CreateRequest(HttpMethod.Patch, endpoint, GetWriteBearerToken(accessToken));
            request.Headers.Add("Prefer", "return=representation");
            request.Content = content;
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to confirm payment for ticket {TicketId}.", ticketId);
            return false;
        }
    }

    public async Task<bool> DeleteTicketAsync(
        string id,
        CancellationToken cancellationToken = default,
        string? accessToken = null)
    {
        try
        {
            var endpoint = $"{_settings.Url.TrimEnd('/')}/rest/v1/tickets?id=eq.{Uri.EscapeDataString(id)}";
            
            using var request = CreateRequest(HttpMethod.Delete, endpoint, GetWriteBearerToken(accessToken));
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to delete ticket in Supabase.");
            return false;
        }
    }

    public string GetScreenshotBucketName()
    {
        return string.IsNullOrWhiteSpace(_settings.TicketScreenshotBucketName)
            ? "screenshots"
            : _settings.TicketScreenshotBucketName;
    }

    public async Task<string?> UploadScreenshotAsync(IFormFile file, string path, string? accessToken = null, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0) return null;

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("A valid Supabase access token is required to upload a screenshot.");
        }

        const long maxFileSize = 15 * 1024 * 1024;
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var uploadContentType = extension == ".png" ? "image/png" : "image/jpeg";
        if (file.Length > maxFileSize || !new[] { ".jpg", ".jpeg", ".png" }.Contains(extension))
        {
            throw new InvalidDataException("Screenshots must be JPG or PNG images no larger than 15 MB.");
        }

        var bucketName = GetScreenshotBucketName();
        var endpoint = $"{_settings.Url.TrimEnd('/')}/storage/v1/object/{bucketName}/{string.Join('/', path.Split('/').Select(Uri.EscapeDataString))}";
        var publicUrl = $"{_settings.Url.TrimEnd('/')}/storage/v1/object/public/{bucketName}/{string.Join('/', path.Split('/').Select(Uri.EscapeDataString))}";

        try
        {
            using var fileStream = file.OpenReadStream();
            using var content = new StreamContent(fileStream);
            content.Headers.ContentType = new MediaTypeHeaderValue(uploadContentType);
            
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Clear();
            request.Headers.Add("apikey", _settings.ApiKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Add("x-upsert", "true");
            request.Content = content;

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Supabase screenshot upload failed. Status={StatusCode}, Response={ResponseBody}", response.StatusCode, responseBody);
                throw new HttpRequestException($"Supabase screenshot upload failed with HTTP {(int)response.StatusCode}: {responseBody}");
            }

            return publicUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to upload screenshot to Supabase Storage.");
            throw;
        }
    }
}
