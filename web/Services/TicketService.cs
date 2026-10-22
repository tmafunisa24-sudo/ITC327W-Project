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

    public TicketService(HttpClient httpClient, IOptions<SupabaseSettings> options, ILogger<TicketService> logger, ImageStorageService imageStorageService)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;
        _imageStorageService = imageStorageService;
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

    public async Task<IReadOnlyList<Ticket>> GetTicketsAsync(string? requester = null, string? assignedTechnician = null, Guid? employeeId = null, Guid? technicianId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.Url) || string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            return Array.Empty<Ticket>();
        }

        try
        {
            var query = new List<string> { "select=*" };
            if (!string.IsNullOrWhiteSpace(requester))
            {
                query.Add($"requester=eq.{Uri.EscapeDataString(requester)}");
            }
            if (!string.IsNullOrWhiteSpace(assignedTechnician))
            {
                query.Add($"assigned_technician=eq.{Uri.EscapeDataString(assignedTechnician)}");
            }
            if (technicianId.HasValue && technicianId.Value != Guid.Empty)
            {
                query.Add($"technician_id=eq.{Uri.EscapeDataString(technicianId.Value.ToString())}");
            }
            if (employeeId.HasValue && employeeId.Value != Guid.Empty)
            {
                query.Add($"employee_id=eq.{Uri.EscapeDataString(employeeId.Value.ToString())}");
            }

            var endpoint = $"{_settings.Url.TrimEnd('/')}/rest/v1/tickets?{string.Join('&', query)}";
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("apikey", _settings.ApiKey);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiKey}");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
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
            return Array.Empty<Ticket>();
        }
    }

    public async Task<Ticket?> GetTicketAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("apikey", _settings.ApiKey);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiKey}");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _httpClient.GetAsync($"{_settings.Url.TrimEnd('/')}/rest/v1/tickets?id=eq.{id}&select=*", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            var ticket = JsonSerializer.Deserialize<List<Ticket>>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?.FirstOrDefault();
            return ticket is null ? null : NormalizeTicket(ticket);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to load ticket from Supabase.");
            return null;
        }
    }

    public async Task<Ticket?> GetTicketByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await GetTicketAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyList<Ticket>> GetEmployeeTicketsAsync(string requester, CancellationToken cancellationToken = default)
    {
        return await GetTicketsAsync(requester: requester, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<Ticket>> GetTechnicianTicketsAsync(Guid technicianId, string assignedTechnician, CancellationToken cancellationToken = default)
    {
        var tickets = new List<Ticket>();
        if (technicianId != Guid.Empty)
        {
            tickets.AddRange(await GetTicketsAsync(technicianId: technicianId, cancellationToken: cancellationToken));
        }

        if (!string.IsNullOrWhiteSpace(assignedTechnician))
        {
            tickets.AddRange(await GetTicketsAsync(assignedTechnician: assignedTechnician, cancellationToken: cancellationToken));
        }

        return tickets
            .GroupBy(ticket => ticket.id ?? Guid.NewGuid().ToString(), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
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

            var maskedApiKey = string.IsNullOrWhiteSpace(_settings.ApiKey)
                ? "<missing>"
                : _settings.ApiKey.Length <= 8
                    ? "<masked>"
                    : $"{_settings.ApiKey[..4]}...{_settings.ApiKey[^4..]}";

            _logger.LogInformation("TicketService.CreateTicketAsync using Supabase URL {Url} and table {Table}.", endpoint, "tickets");
            _logger.LogInformation("TicketService.CreateTicketAsync request headers: apikey={ApiKeyMask}, HasAccessToken={HasAccessToken}, Content-Type=application/json, Prefer=return=representation.", maskedApiKey, !string.IsNullOrWhiteSpace(accessToken));
            _logger.LogInformation("TicketService.CreateTicketAsync final request payload: {Payload}", json);

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("apikey", _settings.ApiKey);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {(string.IsNullOrWhiteSpace(accessToken) ? _settings.ApiKey : accessToken)}");
            _httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation("TicketService.CreateTicketAsync Supabase response status: {StatusCode}. Response body: {ResponseBody}", response.StatusCode, responseBody);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("TicketService.CreateTicketAsync Supabase insert failed. Status: {StatusCode}. Response: {ResponseBody}", response.StatusCode, responseBody);
                throw new HttpRequestException($"Supabase ticket insert failed with status {(int)response.StatusCode}: {responseBody}");
            }

            try
            {
                var inserted = JsonSerializer.Deserialize<List<Ticket>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (inserted is { Count: > 0 })
                {
                    var insertedTicket = inserted[0];
                    if (!string.IsNullOrWhiteSpace(insertedTicket.id))
                    {
                        ticket.id = insertedTicket.id;
                    }

                    ticket.technician_id = insertedTicket.technician_id;
                    ticket.assigned_technician = insertedTicket.assigned_technician;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to parse inserted ticket response from Supabase.");
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TicketService.CreateTicketAsync exception while inserting into Supabase tickets table. Exception message: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<bool> UpdateTicketAsync(Ticket ticket, CancellationToken cancellationToken = default)
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
                ["payment_method"] = ticket.payment_method,
                ["payment_status"] = ticket.payment_status,
                ["updated_at"] = ticket.updated_at
            };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("apikey", _settings.ApiKey);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiKey}");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _httpClient.PatchAsync($"{_settings.Url.TrimEnd('/')}/rest/v1/tickets?id=eq.{ticket.id}", content, cancellationToken);
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
            ConfigureJsonHeaders(accessToken);
            using var response = await _httpClient.GetAsync($"{_settings.Url.TrimEnd('/')}/rest/v1/ticket_attachments?ticket_id=eq.{parsedTicketId}&select=*&order=created_at.asc", cancellationToken);
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

            var content = new StringContent(JsonSerializer.Serialize(attachment), Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("apikey", _settings.ApiKey);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetServerWriteToken(accessToken));
            _httpClient.DefaultRequestHeaders.Add("Prefer", "return=minimal");
            var endpoint = $"{_settings.Url.TrimEnd('/')}/rest/v1/ticket_attachments";
            _logger.LogInformation(
                "Supabase attachment INSERT request. Method={Method}, Endpoint={Endpoint}, TicketId={TicketId}, FileUrl={FileUrl}, FileName={FileName}, UploadedBy={UploadedBy}, Payload={Payload}",
                HttpMethod.Post,
                endpoint,
                ticketId,
                attachment.FileUrl,
                attachment.FileName,
                attachment.UploadedBy,
                JsonSerializer.Serialize(attachment));
            using var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("Supabase attachment INSERT response. Method={Method}, Endpoint={Endpoint}, Status={StatusCode}, TicketId={TicketId}, FileUrl={FileUrl}, Response={Response}", HttpMethod.Post, endpoint, response.StatusCode, ticketId, attachment.FileUrl, responseBody);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Attachment metadata insert failed. TicketId={TicketId}, FileUrl={FileUrl}, FileName={FileName}, UploadedBy={UploadedBy}, Status={StatusCode}, Response={Response}",
                    ticketId,
                    attachment.FileUrl,
                    attachment.FileName,
                    attachment.UploadedBy,
                    response.StatusCode,
                    responseBody);
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
            var content = new StringContent(JsonSerializer.Serialize(new { screenshot_url = screenshotUrl }), Encoding.UTF8, "application/json");
            ConfigureJsonHeaders(GetServerWriteToken(accessToken));
            _httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");
            var endpoint = $"{_settings.Url.TrimEnd('/')}/rest/v1/tickets?id=eq.{Uri.EscapeDataString(ticketId)}";
            _logger.LogInformation("Supabase screenshot PATCH request. Method={Method}, Endpoint={Endpoint}, TicketId={TicketId}, ScreenshotUrl={ScreenshotUrl}", HttpMethod.Patch, endpoint, ticketId, screenshotUrl);
            using var response = await _httpClient.PatchAsync(endpoint, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("Supabase screenshot PATCH response. Method={Method}, Endpoint={Endpoint}, Status={StatusCode}, TicketId={TicketId}, ScreenshotUrl={ScreenshotUrl}, Response={Response}", HttpMethod.Patch, endpoint, response.StatusCode, ticketId, screenshotUrl, responseBody);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Unable to save primary screenshot for ticket {TicketId}. Status={StatusCode}, Response={Response}", ticketId, response.StatusCode, responseBody);
                return false;
            }

            var updatedTickets = JsonSerializer.Deserialize<List<Ticket>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (updatedTickets is not { Count: 1 })
            {
                _logger.LogError(
                    "Supabase did not update exactly one ticket row. TicketId={TicketId}, ScreenshotUrl={ScreenshotUrl}, Status={StatusCode}, Response={Response}, RowsReturned={RowsReturned}",
                    ticketId,
                    screenshotUrl,
                    response.StatusCode,
                    responseBody,
                    updatedTickets?.Count ?? 0);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to save primary screenshot for ticket {TicketId}.", ticketId);
            return false;
        }
    }

    private void ConfigureJsonHeaders(string? accessToken = null)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("apikey", _settings.ApiKey);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", string.IsNullOrWhiteSpace(accessToken) ? _settings.ApiKey : accessToken);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private string GetServerWriteToken(string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.ServiceRoleKey) && string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("No authenticated Supabase write credential is configured.");
        }

        return string.IsNullOrWhiteSpace(_settings.ServiceRoleKey) ? accessToken ?? _settings.ApiKey : _settings.ServiceRoleKey;
    }

    public async Task<bool> ConfirmPaymentAsync(string ticketId, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = JsonSerializer.Serialize(new { payment_status = "Paid" });
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("apikey", _settings.ApiKey);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiKey}");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _httpClient.PatchAsync($"{_settings.Url.TrimEnd('/')}/rest/v1/tickets?id=eq.{Uri.EscapeDataString(ticketId)}", content, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to confirm payment for ticket {TicketId}.", ticketId);
            return false;
        }
    }

    public async Task<bool> DeleteTicketAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("apikey", _settings.ApiKey);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiKey}");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _httpClient.DeleteAsync($"{_settings.Url.TrimEnd('/')}/rest/v1/tickets?id=eq.{Uri.EscapeDataString(id)}", cancellationToken);
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
        if (file is null || file.Length == 0)
        {
            return null;
        }

        if (!HasValidAccessToken(accessToken))
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
            _logger.LogInformation("Starting screenshot upload. Endpoint={Endpoint}, Bucket={Bucket}, Path={Path}, FileName={FileName}, ContentType={ContentType}, SizeBytes={SizeBytes}, HasAccessToken={HasAccessToken}", endpoint, bucketName, path, Path.GetFileName(file.FileName), uploadContentType, file.Length, !string.IsNullOrWhiteSpace(accessToken));
            using var fileStream = file.OpenReadStream();
            using var content = new StreamContent(fileStream);
            content.Headers.ContentType = new MediaTypeHeaderValue(uploadContentType);
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("apikey", _settings.ApiKey);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            _httpClient.DefaultRequestHeaders.Add("x-upsert", "true");

            using var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("Screenshot upload response. StatusCode={StatusCode}, Bucket={Bucket}, Path={Path}, ResponseBody={ResponseBody}", (int)response.StatusCode, bucketName, path, responseBody);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Supabase screenshot upload failed. Bucket={Bucket}, Path={Path}, Status={StatusCode}, Response={ResponseBody}", bucketName, path, response.StatusCode, responseBody);
                throw new HttpRequestException($"Supabase screenshot upload failed with HTTP {(int)response.StatusCode}: {responseBody}");
            }

            return publicUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to upload screenshot to Supabase Storage. Endpoint={Endpoint}, Bucket={Bucket}, Path={Path}, FileName={FileName}, ContentType={ContentType}, SizeBytes={SizeBytes}, Exception={ExceptionMessage}, InnerException={InnerExceptionMessage}", endpoint, bucketName, path, Path.GetFileName(file.FileName), uploadContentType, file.Length, ex.Message, ex.InnerException?.Message);
            throw;
        }
    }

    private static bool HasValidAccessToken(string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return false;
        }

        var tokenParts = accessToken.Split('.');
        if (tokenParts.Length != 3)
        {
            return false;
        }

        try
        {
            var payload = tokenParts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var document = JsonDocument.Parse(Convert.FromBase64String(payload));
            return document.RootElement.TryGetProperty("exp", out var expiration)
                && expiration.TryGetInt64(out var expirationUnixTime)
                && expirationUnixTime > DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
