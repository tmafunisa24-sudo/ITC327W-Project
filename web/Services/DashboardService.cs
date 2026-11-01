using System.Text.Json;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using ServiceHub_IT.DTOs;
using ServiceHub_IT.Models;

namespace ServiceHub_IT.Services;

public class DashboardService
{
    private readonly HttpClient _httpClient;
    private readonly SupabaseSettings _settings;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(HttpClient httpClient, IOptions<SupabaseSettings> options, ILogger<DashboardService> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<DashboardMetricsDto> GetMetricsAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var metrics = new DashboardMetricsDto();

        if (string.IsNullOrWhiteSpace(_settings.Url) || string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            return metrics;
        }

        try
        {
            using var assetsRequest = new HttpRequestMessage(HttpMethod.Get, $"{_settings.Url.TrimEnd('/')}/rest/v1/assets?select=*");
            assetsRequest.Headers.Clear();
            assetsRequest.Headers.Add("apikey", _settings.ApiKey);
            assetsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            assetsRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var assetsResponse = await _httpClient.SendAsync(assetsRequest, cancellationToken);
            if (assetsResponse.IsSuccessStatusCode)
            {
                var assets = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(await assetsResponse.Content.ReadAsStringAsync(cancellationToken));
                if (assets is not null)
                {
                    metrics.TotalAssets = assets.Count;
                    metrics.AssignedAssets = assets.Count(x => x.TryGetValue("assigned_employee", out var assignedEmployee) && !string.IsNullOrWhiteSpace(assignedEmployee.GetString()));
                    metrics.AvailableAssets = assets.Count - metrics.AssignedAssets;
                }
            }

            using var profilesRequest = new HttpRequestMessage(HttpMethod.Get, $"{_settings.Url.TrimEnd('/')}/rest/v1/profiles?select=*");
            profilesRequest.Headers.Clear();
            profilesRequest.Headers.Add("apikey", _settings.ApiKey);
            profilesRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            profilesRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var profilesResponse = await _httpClient.SendAsync(profilesRequest, cancellationToken);
            if (profilesResponse.IsSuccessStatusCode)
            {
                var profiles = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(await profilesResponse.Content.ReadAsStringAsync(cancellationToken));
                if (profiles is not null)
                {
                    metrics.Employees = profiles.Count(x => x.TryGetValue("role", out var role) && role.GetString()?.Equals("Client", StringComparison.OrdinalIgnoreCase) == true);
                    metrics.Technicians = profiles.Count(x => x.TryGetValue("role", out var role) && role.GetString()?.Equals("Technician", StringComparison.OrdinalIgnoreCase) == true);
                }
            }

            using var ticketsRequest = new HttpRequestMessage(HttpMethod.Get, $"{_settings.Url.TrimEnd('/')}/rest/v1/tickets?select=*");
            ticketsRequest.Headers.Clear();
            ticketsRequest.Headers.Add("apikey", _settings.ApiKey);
            ticketsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            ticketsRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var ticketsResponse = await _httpClient.SendAsync(ticketsRequest, cancellationToken);
            if (ticketsResponse.IsSuccessStatusCode)
            {
                var tickets = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(await ticketsResponse.Content.ReadAsStringAsync(cancellationToken));
                if (tickets is not null)
                {
                    metrics.OpenTickets = tickets.Count(x => x.TryGetValue("status", out var status) && !status.GetString()?.Equals("Closed", StringComparison.OrdinalIgnoreCase) == true);
                    metrics.ClosedTickets = tickets.Count(x => x.TryGetValue("status", out var status) && status.GetString()?.Equals("Closed", StringComparison.OrdinalIgnoreCase) == true);
                }
            }

            using var maintenanceRequest = new HttpRequestMessage(HttpMethod.Get, $"{_settings.Url.TrimEnd('/')}/rest/v1/maintenance?select=*");
            maintenanceRequest.Headers.Clear();
            maintenanceRequest.Headers.Add("apikey", _settings.ApiKey);
            maintenanceRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            maintenanceRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var maintenanceResponse = await _httpClient.SendAsync(maintenanceRequest, cancellationToken);
            if (maintenanceResponse.IsSuccessStatusCode)
            {
                var maintenance = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(await maintenanceResponse.Content.ReadAsStringAsync(cancellationToken));
                if (maintenance is not null)
                {
                    metrics.MaintenanceDue = maintenance.Count(x => x.TryGetValue("status", out var status) && status.GetString()?.Equals("Due", StringComparison.OrdinalIgnoreCase) == true);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to load dashboard metrics from Supabase.");
        }

        return metrics;
    }
}
