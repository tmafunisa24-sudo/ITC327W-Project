using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using ServiceHub_IT.Models;
using ServiceHub_IT.Controllers.Api;

namespace ServiceHub_IT.Services;

public class DepartmentService
{
    private readonly HttpClient _httpClient;
    private readonly SupabaseSettings _settings;
    private readonly ILogger<DepartmentService> _logger;

    private bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.Url) && !string.IsNullOrWhiteSpace(_settings.ApiKey);

    public DepartmentService(HttpClient httpClient, IOptions<SupabaseSettings> options, ILogger<DepartmentService> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Department>> GetDepartmentsAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.Url) || string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            return Array.Empty<Department>();
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_settings.Url.TrimEnd('/')}/rest/v1/departments?select=*");
            request.Headers.Clear();
            request.Headers.Add("apikey", _settings.ApiKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Array.Empty<Department>();
            }

            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<Department>>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Department>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to load departments from Supabase.");
            return Array.Empty<Department>();
        }
    }

    public async Task<Department?> GetDepartmentAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_settings.Url.TrimEnd('/')}/rest/v1/departments?id=eq.{id}&select=*");
            request.Headers.Clear();
            request.Headers.Add("apikey", _settings.ApiKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<Department>>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to load department from Supabase.");
            return null;
        }
    }

    public async Task<bool> CreateDepartmentAsync(Department department, CancellationToken cancellationToken = default)
{
    var currentDepartment = department;
    if (currentDepartment is null || !IsConfigured)
    {
        return false;
    }

    // Only send columns that exist in the DB
    var payload = new Dictionary<string, object?>
    {
        ["department_name"] = currentDepartment.department_name ?? currentDepartment.name,
        ["description"] = currentDepartment.description,
        ["created_at"] = currentDepartment.created_at ?? DateTime.UtcNow,
        ["updated_at"] = currentDepartment.updated_at ?? DateTime.UtcNow,
    };

    using var request = new HttpRequestMessage(
        HttpMethod.Post,
        $"{_settings.Url.TrimEnd('/')}/rest/v1/departments")
    {
        Content = new StringContent(
            JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            }),
            Encoding.UTF8,
            "application/json")
    };

    request.Headers.Clear();
    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    request.Headers.Add("apikey", _settings.ApiKey);
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", string.IsNullOrWhiteSpace(_settings.ServiceRoleKey) ? _settings.ApiKey : _settings.ServiceRoleKey);
    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    request.Headers.Add("Prefer", "return=representation");

    using var response = await _httpClient.SendAsync(request, cancellationToken);
    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

    if (!response.IsSuccessStatusCode)
    {
        _logger.LogWarning(
            "Supabase department insert failed. Status: {StatusCode}. Response: {Response}",
            response.StatusCode, responseBody);
        return false;
    }

    return true;
}

public async Task<bool> UpdateDepartmentAsync(Department department, CancellationToken cancellationToken = default)
{
    var currentDepartment = department;
    if (currentDepartment is null || string.IsNullOrWhiteSpace(currentDepartment.id) || !IsConfigured)
    {
        return false;
    }

    var departmentId = currentDepartment.id;
    var payload = new Dictionary<string, object?>
    {
        ["department_name"] = currentDepartment.department_name ?? currentDepartment.name,
        ["description"] = currentDepartment.description,
        ["updated_at"] = DateTime.UtcNow,
    };

    using var request = new HttpRequestMessage(
        HttpMethod.Patch,
        $"{_settings.Url.TrimEnd('/')}/rest/v1/departments?id=eq.{Uri.EscapeDataString(departmentId)}")
    {
        Content = new StringContent(
            JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            }),
            Encoding.UTF8,
            "application/json")
    };

    request.Headers.Clear();
    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    request.Headers.Add("apikey", _settings.ApiKey);
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", string.IsNullOrWhiteSpace(_settings.ServiceRoleKey) ? _settings.ApiKey : _settings.ServiceRoleKey);
    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    request.Headers.Add("Prefer", "return=representation");

    using var response = await _httpClient.SendAsync(request, cancellationToken);
    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

    if (!response.IsSuccessStatusCode)
    {
        _logger.LogWarning(
            "Supabase department update failed. Status: {StatusCode}. Response: {Response}",
            response.StatusCode, responseBody);
        return false;
    }

    return true;
}

    public async Task<bool> DeleteDepartmentAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"{_settings.Url.TrimEnd('/')}/rest/v1/departments?id=eq.{id}");
            request.Headers.Clear();
            request.Headers.Add("apikey", _settings.ApiKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to delete department from Supabase.");
            return false;
        }
    }
}
