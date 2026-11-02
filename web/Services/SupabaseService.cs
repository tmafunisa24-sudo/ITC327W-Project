using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using ServiceHub_IT.DTOs;
using ServiceHub_IT.Interfaces;
using ServiceHub_IT.Models;

namespace ServiceHub_IT.Services;

public sealed class SupabaseService : ISupabaseService
{
    private readonly SupabaseSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SupabaseService> _logger;

    public SupabaseService(IOptions<SupabaseSettings> options, HttpClient httpClient, ILogger<SupabaseService> logger)
    {
        _settings = options.Value;
        _httpClient = httpClient;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_settings.Url.TrimEnd('/') + "/");
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_settings.Url) && !string.IsNullOrWhiteSpace(_settings.ApiKey);

    private string ReadBearerToken => !string.IsNullOrWhiteSpace(_settings.ServiceRoleKey)
        ? _settings.ServiceRoleKey
        : _settings.ApiKey;

    private string WriteBearerToken => !string.IsNullOrWhiteSpace(_settings.ServiceRoleKey)
        ? _settings.ServiceRoleKey
        : throw new InvalidOperationException("Supabase ServiceRoleKey is required for this backend write operation.");

    public async Task<SupabaseConnectionStatus> CheckConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new SupabaseConnectionStatus
            {
                IsConfigured = false,
                IsConnected = false,
                Message = "Supabase settings are missing. Update the Supabase section in appsettings.json.",
                Error = "Configuration missing"
            };
        }

        var auth = await CheckAuthenticationAsync(cancellationToken);
        var storage = await CheckStorageAsync(cancellationToken);
        var postgres = await CheckPostgresAsync(cancellationToken);

        var results = new[] { auth, storage, postgres };
        var isConnected = results.All(x => x.IsConnected);

        return new SupabaseConnectionStatus
        {
            IsConfigured = true,
            IsConnected = isConnected,
            Message = string.Join(" | ", results.Select(x => x.Message).Where(x => !string.IsNullOrWhiteSpace(x))),
            Error = isConnected ? null : string.Join(" | ", results.Select(x => x.Error).Where(x => !string.IsNullOrWhiteSpace(x)))
        };
    }

    public async Task<SupabaseConnectionStatus> CheckAuthenticationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync("auth/v1/settings", cancellationToken);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return new SupabaseConnectionStatus
                {
                    IsConfigured = true,
                    IsConnected = true,
                    Message = "Supabase Authentication is reachable."
                };
            }

            return new SupabaseConnectionStatus
            {
                IsConfigured = true,
                IsConnected = false,
                Message = "Supabase Authentication returned an unexpected response.",
                Error = $"Status: {(int)response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Supabase Authentication check failed.");
            return new SupabaseConnectionStatus
            {
                IsConfigured = true,
                IsConnected = false,
                Message = "Supabase Authentication check failed.",
                Error = ex.Message
            };
        }
    }

    public async Task<SupabaseConnectionStatus> CheckStorageAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync("storage/v1/bucket", cancellationToken);
            if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.NotFound)
            {
                return new SupabaseConnectionStatus
                {
                    IsConfigured = true,
                    IsConnected = true,
                    Message = "Supabase Storage is reachable."
                };
            }

            return new SupabaseConnectionStatus
            {
                IsConfigured = true,
                IsConnected = false,
                Message = "Supabase Storage returned an unexpected response.",
                Error = $"Status: {(int)response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Supabase Storage check failed.");
            return new SupabaseConnectionStatus
            {
                IsConfigured = true,
                IsConnected = false,
                Message = "Supabase Storage check failed.",
                Error = ex.Message
            };
        }
    }

    public async Task<SupabaseConnectionStatus> CheckPostgresAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync("rest/v1/", cancellationToken);
            if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound)
            {
                return new SupabaseConnectionStatus
                {
                    IsConfigured = true,
                    IsConnected = true,
                    Message = "Supabase PostgreSQL endpoint is reachable."
                };
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            {
                return new SupabaseConnectionStatus
                {
                    IsConfigured = true,
                    IsConnected = false,
                    Message = "Supabase PostgreSQL endpoint rejected the provided key.",
                    Error = $"Status: {(int)response.StatusCode}"
                };
            }

            return new SupabaseConnectionStatus
            {
                IsConfigured = true,
                IsConnected = false,
                Message = "Supabase PostgreSQL returned an unexpected response.",
                Error = $"Status: {(int)response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Supabase PostgreSQL check failed.");
            return new SupabaseConnectionStatus
            {
                IsConfigured = true,
                IsConnected = false,
                Message = "Supabase PostgreSQL check failed.",
                Error = ex.Message
            };
        }
    }

    public async Task<SupabaseAuthResult> SignInAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || !IsConfigured)
        {
            return new SupabaseAuthResult
            {
                IsSuccess = false,
                ErrorMessage = "Invalid authentication request.",
                RawResponse = string.Empty
            };
        }

        var body = new { email, password };
        using var request = new HttpRequestMessage(HttpMethod.Post, "auth/v1/token?grant_type=password")
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };

        request.Headers.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("apikey", _settings.ApiKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Supabase signin failed. Status: {StatusCode}. Response: {Response}", response.StatusCode, payload);
            return new SupabaseAuthResult
            {
                IsSuccess = false,
                ErrorMessage = ExtractSupabaseError(payload) ?? "Unable to sign in with the supplied credentials.",
                RawResponse = payload
            };
        }

        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var accessToken = data?.GetValueOrDefault("access_token").GetString();

        return new SupabaseAuthResult
        {
            IsSuccess = !string.IsNullOrWhiteSpace(accessToken),
            AccessToken = accessToken,
            ErrorMessage = string.IsNullOrWhiteSpace(accessToken) ? "Authentication succeeded but no access token was returned." : null,
            RawResponse = payload
        };
    }

    public async Task<SupabaseAuthResult> SignUpAsync(string email, string password, string? fullName = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || !IsConfigured)
        {
            return new SupabaseAuthResult
            {
                IsSuccess = false,
                ErrorMessage = "Invalid signup request.",
                RawResponse = string.Empty
            };
        }

        var payloadObject = new Dictionary<string, object>
        {
            ["email"] = email,
            ["password"] = password
        };
    if (!string.IsNullOrWhiteSpace(fullName))
    {
        payloadObject["data"] = new Dictionary<string, object>
        {
            ["full_name"] = fullName
        };
    }



        var signupRequestUrl = "auth/v1/signup";
        if (!string.IsNullOrWhiteSpace(_settings.RedirectUrl))
        {
            signupRequestUrl += $"?redirect_to={Uri.EscapeDataString(_settings.RedirectUrl.Trim())}";
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, signupRequestUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(payloadObject), Encoding.UTF8, "application/json")
        };

        request.Headers.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("apikey", _settings.ApiKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Supabase signup failed. Status: {StatusCode}. Response: {Response}", response.StatusCode, payload);
            return new SupabaseAuthResult
            {
                IsSuccess = false,
                ErrorMessage = ExtractSupabaseError(payload) ?? "Unable to create your account.",
                RawResponse = payload
            };
        }

        return new SupabaseAuthResult
        {
            IsSuccess = true,
            RawResponse = payload
        };
    }

    private static string? ExtractSupabaseError(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (data is null)
            {
                return null;
            }

            string? errorText = null;

            if (data.TryGetValue("error_description", out var errorDescription))
            {
                errorText = GetStringValue(errorDescription);
            }

            if (string.IsNullOrWhiteSpace(errorText) && data.TryGetValue("error", out var errorElement))
            {
                errorText = GetStringValue(errorElement);
            }

            if (string.IsNullOrWhiteSpace(errorText) && data.TryGetValue("message", out var messageElement))
            {
                errorText = GetStringValue(messageElement);
            }

            if (string.IsNullOrWhiteSpace(errorText) && data.TryGetValue("msg", out var msgElement))
            {
                errorText = GetStringValue(msgElement);
            }

            return string.IsNullOrWhiteSpace(errorText) ? null : errorText;
        }
        catch
        {
            return null;
        }
    }

    private static string? GetStringValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Object => element.TryGetProperty("message", out var nestedMessage) ? nestedMessage.GetString() : null,
            JsonValueKind.Array => element.EnumerateArray().Select(GetStringValue).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)),
            _ => null
        };
    }

    public async Task<string> GetRoleForEmailAsync(string email, string accessToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(accessToken) || !IsConfigured)
        {
            return string.Empty;
        }

        var request = new HttpRequestMessage(HttpMethod.Get, $"rest/v1/profiles?email=eq.{Uri.EscapeDataString(email)}&select=role");
        request.Headers.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("apikey", _settings.ApiKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Supabase role lookup failed. Status: {StatusCode}. Response: {Response}", response.StatusCode, payload);
            return string.Empty;
        }

        var data = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (data is not null && data.Count > 0 && data[0].TryGetValue("role", out var roleValue))
        {
            return roleValue.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    public async Task<Profile?> GetProfileByAuthUserIdAsync(
        Guid authUserId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        if (authUserId == Guid.Empty || string.IsNullOrWhiteSpace(accessToken) || !IsConfigured)
        {
            return null;
        }

        var authId = Uri.EscapeDataString(authUserId.ToString());
        var profile = await GetProfileForAuthLookupAsync(
            $"rest/v1/profiles?user_id=eq.{authId}&select=*",
            accessToken,
            cancellationToken);

        if (profile is not null)
        {
            if (profile.UserId != authUserId)
            {
                throw new InvalidOperationException("The profile returned for the authenticated user did not match the Auth UUID.");
            }

            return profile;
        }

        // Older profiles may use the Supabase Auth UUID as their primary id.
        profile = await GetProfileForAuthLookupAsync(
            $"rest/v1/profiles?id=eq.{authId}&select=*",
            accessToken,
            cancellationToken);

        return profile?.Id == authUserId ? profile : null;
    }

    private async Task<Profile?> GetProfileForAuthLookupAsync(
        string requestUri,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("apikey", _settings.ApiKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Supabase Auth profile lookup failed. Status: {StatusCode}", response.StatusCode);
            throw new HttpRequestException("Supabase profile lookup failed.", null, response.StatusCode);
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        var profiles = JsonSerializer.Deserialize<List<Profile>>(
            payload,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Profile>();

        if (profiles.Count > 1)
        {
            throw new InvalidOperationException("The authenticated user maps to multiple profiles.");
        }

        return profiles.SingleOrDefault();
    }

    public async Task<Profile?> GetProfileByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || !IsConfigured)
        {
            return null;
        }

        email = email.Trim();
        var requestUri = $"rest/v1/profiles?email=eq.{Uri.EscapeDataString(email)}&select=*";
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Clear();
        request.Headers.Add("apikey", _settings.ApiKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", WriteBearerToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            var profiles = JsonSerializer.Deserialize<List<Profile>>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var profile = profiles?.FirstOrDefault();
            if (profile is not null)
            {
                return profile;
            }
        }
        else
        {
            _logger.LogWarning("Supabase profile lookup by email failed on exact match. Status: {StatusCode}, Email: {Email}", response.StatusCode, email);
        }

        var fallbackUri = $"rest/v1/profiles?email=ilike.{Uri.EscapeDataString(email)}&select=*";
        using var fallbackRequest = new HttpRequestMessage(HttpMethod.Get, fallbackUri);
        fallbackRequest.Headers.Clear();
        fallbackRequest.Headers.Add("apikey", _settings.ApiKey);
        fallbackRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", WriteBearerToken);
        fallbackRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var fallbackResponse = await _httpClient.SendAsync(fallbackRequest, cancellationToken);
        if (!fallbackResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("Supabase profile lookup by email fallback failed. Status: {StatusCode}, Email: {Email}", fallbackResponse.StatusCode, email);
            return null;
        }

        var fallbackPayload = await fallbackResponse.Content.ReadAsStringAsync(cancellationToken);
        var fallbackProfiles = JsonSerializer.Deserialize<List<Profile>>(fallbackPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return fallbackProfiles?.FirstOrDefault();
    }

    public async Task<Profile?> GetProfileByIdAsync(
        Guid id,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty || string.IsNullOrWhiteSpace(accessToken) || !IsConfigured)
            return null;

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"rest/v1/profiles?id=eq.{Uri.EscapeDataString(id.ToString())}&select=*");
        request.Headers.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("apikey", _settings.ApiKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Supabase profile lookup by id failed. Status: {StatusCode}", response.StatusCode);
            return null;
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        var profiles = JsonSerializer.Deserialize<List<Profile>>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return profiles?.FirstOrDefault();
    }

    public async Task<IEnumerable<Profile>> GetAllProfilesAsync(CancellationToken cancellationToken = default)
    {
        return await GetAllProfilesAsync(null, cancellationToken);
    }

    public async Task<IEnumerable<Profile>> GetAllProfilesAsync(
        string? accessToken,
        CancellationToken cancellationToken = default,
        bool throwOnError = false)
    {
        if (!IsConfigured)
        {
            if (throwOnError)
            {
                throw new InvalidOperationException("Supabase is not configured for profile reads.");
            }
            return Array.Empty<Profile>();
        }

        var requestUri = "rest/v1/profiles?select=*";
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Clear();
        request.Headers.Add("apikey", _settings.ApiKey);
        var token = string.IsNullOrWhiteSpace(accessToken) ? ReadBearerToken : accessToken;
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Supabase profile list request failed. Status: {StatusCode}", response.StatusCode);
            if (throwOnError)
            {
                throw new HttpRequestException(
                    "Supabase rejected the profile list request.", null, response.StatusCode);
            }
            return Array.Empty<Profile>();
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        var profiles = JsonSerializer.Deserialize<List<Profile>>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return profiles ?? new List<Profile>();
    }

    public async Task<IEnumerable<Profile>> GetTechniciansAsync(CancellationToken cancellationToken = default)
    {
        return await GetProfilesByRoleAsync("Technician", cancellationToken);
    }

    public async Task<IEnumerable<Profile>> GetProfilesByRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        return await GetProfilesByRoleAsync(role, null, cancellationToken);
    }

    public async Task<IEnumerable<Profile>> GetProfilesByRoleAsync(
        string role,
        string? accessToken,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(role))
        {
            return Array.Empty<Profile>();
        }

        var requestUri = $"rest/v1/profiles?role=eq.{Uri.EscapeDataString(role)}&select=*";
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Clear();
        request.Headers.Add("apikey", _settings.ApiKey);
        var token = string.IsNullOrWhiteSpace(accessToken) ? WriteBearerToken : accessToken;
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Supabase profile role lookup failed. Status: {StatusCode}", response.StatusCode);
            return Array.Empty<Profile>();
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        var profiles = JsonSerializer.Deserialize<List<Profile>>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return profiles ?? new List<Profile>();
    }

    public async Task<IEnumerable<ServiceCatalogItem>> GetActiveServiceCatalogAsync(
        string? accessToken = null,
        CancellationToken cancellationToken = default,
        bool throwOnError = false)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("Service catalog lookup skipped because Supabase is not configured.");
            if (throwOnError)
            {
                throw new InvalidOperationException("Supabase is not configured for service catalog reads.");
            }
            return Array.Empty<ServiceCatalogItem>();
        }

        var requestUri = "rest/v1/service_catalog?is_active=eq.true&select=*";
        _logger.LogInformation("Service catalog request: Endpoint={Endpoint}, HasAccessToken={HasAccessToken}, ApiKeyConfigured={ApiKeyConfigured}",
            requestUri, !string.IsNullOrWhiteSpace(accessToken), !string.IsNullOrWhiteSpace(_settings.ApiKey));
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Clear();
        request.Headers.Add("apikey", _settings.ApiKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", !string.IsNullOrWhiteSpace(accessToken) ? accessToken : _settings.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogInformation("Service catalog response: StatusCode={StatusCode}, Body={Body}", response.StatusCode, payload);
        if (response.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrWhiteSpace(accessToken))
        {
            throw new UnauthorizedAccessException("The Supabase session used to load the service catalog has expired.");
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Supabase service catalog request failed. Status: {StatusCode}", response.StatusCode);
            if (throwOnError)
            {
                throw new HttpRequestException(
                    "Supabase rejected the service catalog request.", null, response.StatusCode);
            }
            return Array.Empty<ServiceCatalogItem>();
        }

        var services = JsonSerializer.Deserialize<List<ServiceCatalogItem>>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var activeServices = services?.Where(service => service.IsActive).ToList() ?? new List<ServiceCatalogItem>();
        _logger.LogInformation("Service catalog records returned: Count={Count}", activeServices.Count);
        return activeServices;
    }

    public async Task<bool> CreateProfileAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        if (profile is null || !IsConfigured)
        {
            return false;
        }

        var normalizedRole = NormalizeRole(profile.Role);
        var normalizedStatus = NormalizeStatus(profile.Status);
        var profilePayload = new Dictionary<string, object?>
        {
            ["id"] = profile.Id,
            ["user_id"] = profile.UserId,
            ["full_name"] = profile.FullName,
            ["email"] = profile.Email,
            ["phone"] = profile.Phone,
            ["role"] = normalizedRole,
            ["department_id"] = profile.DepartmentId,
            ["position"] = profile.Position,
            ["profile_image"] = profile.ProfileImage,
            ["email_verified"] = profile.EmailVerified,
            ["status"] = normalizedStatus,
            ["last_login_at"] = profile.LastLoginAt,
            ["created_at"] = profile.CreatedAt,
            ["updated_at"] = profile.UpdatedAt ?? DateTime.UtcNow
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "rest/v1/profiles")
        {
            Content = new StringContent(JsonSerializer.Serialize(profilePayload, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            }), Encoding.UTF8, "application/json")
        };

        request.Headers.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("apikey", _settings.ApiKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", WriteBearerToken);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        request.Headers.Add("Prefer", "return=representation");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public Task<bool> UpdateProfileAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        return UpdateProfileAsync(profile, accessToken: null, cancellationToken);
    }

    public async Task<bool> UpdateProfileAsync(
        Profile profile,
        string? accessToken,
        CancellationToken cancellationToken = default)
    {
        if (profile is null || profile.Id == Guid.Empty || !IsConfigured)
        {
            return false;
        }

        var normalizedRole = NormalizeRole(profile.Role);
        var normalizedStatus = NormalizeStatus(profile.Status);
        var payload = new Dictionary<string, object?>
        {
            ["full_name"] = profile.FullName,
            ["email"] = profile.Email,
            ["phone"] = profile.Phone,
            ["role"] = normalizedRole,
            ["department_id"] = profile.DepartmentId,
            ["position"] = profile.Position,
            ["profile_image"] = profile.ProfileImage,
            ["status"] = normalizedStatus,
            ["email_verified"] = profile.EmailVerified,
            ["last_login_at"] = profile.LastLoginAt,
            ["updated_at"] = profile.UpdatedAt ?? DateTime.UtcNow
        };

        using var request = new HttpRequestMessage(HttpMethod.Patch, $"rest/v1/profiles?id=eq.{Uri.EscapeDataString(profile.Id.ToString())}")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            }), Encoding.UTF8, "application/json")
        };

        request.Headers.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("apikey", _settings.ApiKey);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer", string.IsNullOrWhiteSpace(accessToken) ? WriteBearerToken : accessToken);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        request.Headers.Add("Prefer", "return=representation");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = string.IsNullOrWhiteSpace(responseBody)
                ? "Supabase rejected the profile update."
                : responseBody;

            _logger.LogWarning(
                "Supabase profile update failed. Status: {StatusCode}. Response: {Response}",
                response.StatusCode,
                responseBody);
            throw new InvalidOperationException(errorMessage);
        }

        return true;
    }

    private static string NormalizeRole(string? role)
    {
        return role?.Trim().ToLowerInvariant() switch
        {
            "admin" => "Admin",
            "technician" => "Technician",
            "client" => "Client",
            _ => "Unknown"
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

    public async Task<bool> DeleteProfileAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty || !IsConfigured)
        {
            return false;
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"rest/v1/profiles?id=eq.{Uri.EscapeDataString(id.ToString())}");
        request.Headers.Add("apikey", _settings.ApiKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", WriteBearerToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(
        Guid profileId,
        CancellationToken cancellationToken)
    {
        return await GetUserNotificationsAsync(profileId, null, cancellationToken);
    }

    public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(
        Guid profileId,
        string? accessToken = null,
        CancellationToken cancellationToken = default)
    {
        if (profileId == Guid.Empty || !IsConfigured)
        {
            return Array.Empty<Notification>();
        }

        var requestUri = $"rest/v1/notifications?user_id=eq.{Uri.EscapeDataString(profileId.ToString())}&select=*&order=created_at.desc";
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Clear();
        request.Headers.Add("apikey", _settings.ApiKey);
        var token = string.IsNullOrWhiteSpace(accessToken) ? ReadBearerToken : accessToken;
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Supabase notifications request failed. Status: {StatusCode}", response.StatusCode);
            return Array.Empty<Notification>();
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        var notifications = JsonSerializer.Deserialize<List<Notification>>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return notifications ?? new List<Notification>();
    }

    public async Task<IEnumerable<Notification>> GetNotificationsForUserAsync(
        Guid profileId,
        int limit,
        CancellationToken cancellationToken)
    {
        return await GetNotificationsForUserAsync(profileId, limit, null, cancellationToken);
    }

    public async Task<IEnumerable<Notification>> GetNotificationsForUserAsync(
        Guid profileId,
        int limit,
        string? accessToken = null,
        CancellationToken cancellationToken = default)
    {
        if (profileId == Guid.Empty || !IsConfigured || limit <= 0)
        {
            return Array.Empty<Notification>();
        }

        var requestUri = $"rest/v1/notifications?user_id=eq.{Uri.EscapeDataString(profileId.ToString())}&select=*&order=created_at.desc&limit={limit}";
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Clear();
        request.Headers.Add("apikey", _settings.ApiKey);
        var token = string.IsNullOrWhiteSpace(accessToken) ? ReadBearerToken : accessToken;
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Supabase recent notifications request failed. Status: {StatusCode}", response.StatusCode);
            return Array.Empty<Notification>();
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        var notifications = JsonSerializer.Deserialize<List<Notification>>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return notifications ?? new List<Notification>();
    }

    public async Task<IEnumerable<Notification>> GetUnreadNotificationsForUserAsync(
        Guid profileId,
        CancellationToken cancellationToken)
    {
        return await GetUnreadNotificationsForUserAsync(profileId, null, cancellationToken);
    }

    public async Task<IEnumerable<Notification>> GetUnreadNotificationsForUserAsync(
        Guid profileId,
        string? accessToken = null,
        CancellationToken cancellationToken = default)
    {
        if (profileId == Guid.Empty || !IsConfigured)
        {
            return Array.Empty<Notification>();
        }

        var requestUri = $"rest/v1/notifications?user_id=eq.{Uri.EscapeDataString(profileId.ToString())}&is_read=eq.false&select=*&order=created_at.desc";
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Clear();
        request.Headers.Add("apikey", _settings.ApiKey);
        var token = string.IsNullOrWhiteSpace(accessToken) ? ReadBearerToken : accessToken;
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Supabase unread notifications request failed. Status: {StatusCode}", response.StatusCode);
            return Array.Empty<Notification>();
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        var notifications = JsonSerializer.Deserialize<List<Notification>>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return notifications ?? new List<Notification>();
    }

    public async Task<int> GetUnreadNotificationCountAsync(
        Guid profileId,
        CancellationToken cancellationToken)
    {
        return await GetUnreadNotificationCountAsync(profileId, null, cancellationToken);
    }

    public async Task<int> GetUnreadNotificationCountAsync(
        Guid profileId,
        string? accessToken = null,
        CancellationToken cancellationToken = default)
    {
        if (profileId == Guid.Empty || !IsConfigured)
        {
            return 0;
        }

        var requestUri = $"rest/v1/notifications?user_id=eq.{Uri.EscapeDataString(profileId.ToString())}&is_read=eq.false&select=id";
        _logger.LogInformation("SupabaseService.GetUnreadNotificationCountAsync requestUri={RequestUri}", requestUri);
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Clear();
        request.Headers.Add("apikey", _settings.ApiKey);
        var token = string.IsNullOrWhiteSpace(accessToken) ? ReadBearerToken : accessToken;
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Supabase unread count request failed. Status: {StatusCode}; requestUri={RequestUri}", response.StatusCode, requestUri);
            return 0;
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        var notifications = JsonSerializer.Deserialize<List<Notification>>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var count = notifications?.Count ?? 0;
        _logger.LogInformation("SupabaseService.GetUnreadNotificationCountAsync returned {Count} for profileId {ProfileId}", count, profileId);
        return count;
    }

    public async Task<bool> CreateNotificationAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        if (notification is null || notification.user_id == Guid.Empty || !IsConfigured)
        {
            return false;
        }

        var json = JsonSerializer.Serialize(notification, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, PropertyNameCaseInsensitive = true });
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, "rest/v1/notifications") { Content = content };
        request.Headers.Clear();
        request.Headers.Add("apikey", _settings.ApiKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", WriteBearerToken);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        request.Headers.Add("Prefer", "return=representation");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Supabase notification insert failed. Status: {StatusCode}, Body: {Body}", response.StatusCode, responseBody);
            throw new HttpRequestException(
                $"Supabase notification insert failed with status {(int)response.StatusCode} ({response.StatusCode}): {responseBody}",
                null,
                response.StatusCode);
        }

        return true;
    }

    public async Task<bool> NotificationExistsAsync(
        Guid userId,
        string title,
        string link,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(link) || !IsConfigured)
        {
            return false;
        }

        var query = $"rest/v1/notifications?user_id=eq.{Uri.EscapeDataString(userId.ToString())}" +
                    $"&title=eq.{Uri.EscapeDataString(title)}&link=eq.{Uri.EscapeDataString(link)}&select=id&limit=1";
        using var request = new HttpRequestMessage(HttpMethod.Get, query);
        request.Headers.Add("apikey", _settings.ApiKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ReadBearerToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Supabase notification lookup failed with status {(int)response.StatusCode}: {body}",
                null,
                response.StatusCode);
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(payload);
        return document.RootElement.ValueKind == JsonValueKind.Array && document.RootElement.GetArrayLength() > 0;
    }

    public async Task<bool> MarkNotificationAsReadAsync(Guid id, Guid profileId, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty || profileId == Guid.Empty || !IsConfigured)
        {
            return false;
        }

        using var request = new HttpRequestMessage(HttpMethod.Patch, $"rest/v1/notifications?id=eq.{Uri.EscapeDataString(id.ToString())}&user_id=eq.{Uri.EscapeDataString(profileId.ToString())}")
        {
            Content = new StringContent(JsonSerializer.Serialize(new { is_read = true }), Encoding.UTF8, "application/json")
        };
        request.Headers.Clear();
        request.Headers.Add("apikey", _settings.ApiKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", WriteBearerToken);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        request.Headers.Add("Prefer", "return=representation");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> MarkAllNotificationsAsReadAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        if (profileId == Guid.Empty || !IsConfigured)
        {
            return false;
        }

        using var request = new HttpRequestMessage(HttpMethod.Patch, $"rest/v1/notifications?user_id=eq.{Uri.EscapeDataString(profileId.ToString())}&is_read=eq.false")
        {
            Content = new StringContent(JsonSerializer.Serialize(new { is_read = true }), Encoding.UTF8, "application/json")
        };
        request.Headers.Clear();
        request.Headers.Add("apikey", _settings.ApiKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", WriteBearerToken);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        request.Headers.Add("Prefer", "return=representation");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<IEnumerable<Maintenance>> GetAllMaintenanceAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return Array.Empty<Maintenance>();
        }

        var requestUri = "rest/v1/maintenance?select=*";
        using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Supabase maintenance list request failed. Status: {StatusCode}", response.StatusCode);
            return Array.Empty<Maintenance>();
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        var maintenance = JsonSerializer.Deserialize<List<Maintenance>>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return maintenance ?? new List<Maintenance>();
    }

    public async Task<Maintenance?> GetMaintenanceByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty || !IsConfigured)
        {
            return null;
        }

        var requestUri = $"rest/v1/maintenance?id=eq.{Uri.EscapeDataString(id.ToString())}&select=*";
        using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Supabase maintenance lookup failed. Status: {StatusCode}", response.StatusCode);
            return null;
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        var maintenance = JsonSerializer.Deserialize<List<Maintenance>>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return maintenance?.FirstOrDefault();
    }

    public async Task<bool> CreateMaintenanceAsync(Maintenance maintenance, CancellationToken cancellationToken = default)
    {
        if (maintenance is null || !IsConfigured)
        {
            return false;
        }

        // Build payload with snake_case keys to match Supabase columns
        var payload = new Dictionary<string, object?>
        {
            ["id"] = maintenance.id,
            ["asset_id"] = maintenance.asset_id,
            ["technician_id"] = maintenance.technician_id,
            ["problem"] = maintenance.problem,
            ["solution"] = maintenance.solution,
            ["maintenance_date"] = maintenance.maintenance_date,
            ["next_service_date"] = maintenance.next_service_date,
            ["maintenance_cost"] = maintenance.maintenance_cost,
            ["status"] = maintenance.status,
            ["created_at"] = maintenance.created_at ?? DateTime.UtcNow,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "rest/v1/maintenance")
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
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", WriteBearerToken);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        request.Headers.Add("Prefer", "return=representation");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Supabase maintenance insert failed. Status: {StatusCode}. Response: {Response}",
                response.StatusCode, responseBody);
            return false;
        }

        return true;
    }

    public async Task<bool> UpdateMaintenanceAsync(Maintenance maintenance, CancellationToken cancellationToken = default)
    {
        if (maintenance is null || maintenance.id == Guid.Empty || !IsConfigured)
        {
            return false;
        }

        var payload = new Dictionary<string, object?>
        {
            ["asset_id"] = maintenance.asset_id,
            ["technician_id"] = maintenance.technician_id,
            ["problem"] = maintenance.problem,
            ["solution"] = maintenance.solution,
            ["maintenance_date"] = maintenance.maintenance_date,
            ["next_service_date"] = maintenance.next_service_date,
            ["maintenance_cost"] = maintenance.maintenance_cost,
            ["status"] = maintenance.status,
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"rest/v1/maintenance?id=eq.{Uri.EscapeDataString(maintenance.id.ToString())}")
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
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", WriteBearerToken);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        request.Headers.Add("Prefer", "return=representation");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Supabase maintenance update failed. Status: {StatusCode}. Response: {Response}",
                response.StatusCode, responseBody);
            return false;
        }

        return true;
    }

    public async Task<bool> DeleteMaintenanceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty || !IsConfigured)
        {
            return false;
        }

        using var response = await _httpClient.DeleteAsync($"rest/v1/maintenance?id=eq.{Uri.EscapeDataString(id.ToString())}", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> SendPasswordResetAsync( string email, string redirectTo, CancellationToken cancellationToken = default)
    {
    if (string.IsNullOrWhiteSpace(email) ||
        string.IsNullOrWhiteSpace(redirectTo) ||
        !IsConfigured)
    {
        return false;
    }

    var url = $"auth/v1/recover?redirect_to={Uri.EscapeDataString(redirectTo)}";
    var payload = new { email };

    using var request = new HttpRequestMessage(HttpMethod.Post, url)
    {
        Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json")
    };

    request.Headers.Clear();
    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    request.Headers.Add("apikey", _settings.ApiKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

    using var response = await _httpClient.SendAsync(request, cancellationToken);
    var body = await response.Content.ReadAsStringAsync(cancellationToken);

    if (!response.IsSuccessStatusCode)
    {
        _logger.LogWarning(
            "Supabase password reset request failed. Status: {StatusCode}. Response: {Response}",
            response.StatusCode, body);
        return false;
    }

    return true;
}

    public async Task<bool> ResetPasswordAsync(
    string recoveryToken,
    string newPassword,
    CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(recoveryToken) ||
        string.IsNullOrWhiteSpace(newPassword) ||
        !IsConfigured)
    {
        return false;
    }

    // Step 1: exchange the recovery token for a session
    var verifyUrl = $"auth/v1/verify?type=recovery&token={Uri.EscapeDataString(recoveryToken)}";
    using var verifyRequest = new HttpRequestMessage(HttpMethod.Post, verifyUrl)
    {
        Content = new StringContent("{}", Encoding.UTF8, "application/json")
    };
    verifyRequest.Headers.Clear();
    verifyRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    verifyRequest.Headers.Add("apikey", _settings.ApiKey);
    verifyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

    using var verifyResponse = await _httpClient.SendAsync(verifyRequest, cancellationToken);
    var verifyBody = await verifyResponse.Content.ReadAsStringAsync(cancellationToken);
    if (!verifyResponse.IsSuccessStatusCode)
    {
        _logger.LogWarning(
            "Supabase recovery token verification failed. Status: {StatusCode}. Response: {Response}",
            verifyResponse.StatusCode, verifyBody);
        return false;
    }

    var verifyData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
        verifyBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    var accessToken = verifyData?.GetValueOrDefault("access_token").GetString();
    if (string.IsNullOrWhiteSpace(accessToken))
    {
        _logger.LogWarning("Recovery verification succeeded but returned no access token.");
        return false;
    }

    // Step 2: update the password using the session
    var updatePayload = JsonSerializer.Serialize(new { password = newPassword });
    using var updateRequest = new HttpRequestMessage(HttpMethod.Put, "auth/v1/user")
    {
        Content = new StringContent(updatePayload, Encoding.UTF8, "application/json")
    };
    updateRequest.Headers.Clear();
    updateRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    updateRequest.Headers.Add("apikey", _settings.ApiKey);
    updateRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    using var updateResponse = await _httpClient.SendAsync(updateRequest, cancellationToken);
    var updateBody = await updateResponse.Content.ReadAsStringAsync(cancellationToken);
    if (!updateResponse.IsSuccessStatusCode)
    {
        _logger.LogWarning(
            "Supabase password update failed. Status: {StatusCode}. Response: {Response}",
            updateResponse.StatusCode, updateBody);
        return false;
    }

    return true;
}
    public async Task<Guid?> GetUserIdFromAccessTokenAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken) || !IsConfigured)
            return null;

        using var request = new HttpRequestMessage(HttpMethod.Get, "auth/v1/user");
        request.Headers.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("apikey", _settings.ApiKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var idText = data?.GetValueOrDefault("id").GetString();
        return Guid.TryParse(idText, out var id) ? id : null;
    }

    public async Task<bool> MarkProfileConfirmedAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        if (profileId == Guid.Empty || !IsConfigured)
            return false;

        var payload = new Dictionary<string, object?>
        {
            ["email_verified"] = true,
            ["status"] = "Active",
            ["updated_at"] = DateTime.UtcNow
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"rest/v1/profiles?id=eq.{Uri.EscapeDataString(profileId.ToString())}")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json")
        };

        request.Headers.Add("apikey", _settings.ApiKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", WriteBearerToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("Prefer", "return=representation");
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "MarkProfileConfirmedAsync failed. Status: {StatusCode}. Body: {Body}",
                response.StatusCode, body);
            return false;
        }
        return true;
    }

    public async Task<bool> SetRoleAndStatusAsync(
        Guid profileId,
        string role,
        string status,
        bool profileCompleted,
        CancellationToken cancellationToken = default)
    {
        if (profileId == Guid.Empty || !IsConfigured)
            return false;

        var payload = new Dictionary<string, object?>
        {
            ["role"] = NormalizeRole(role),
            ["status"] = NormalizeStatus(status),
            ["profile_completed"] = profileCompleted,
            ["updated_at"] = DateTime.UtcNow
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"rest/v1/profiles?id=eq.{Uri.EscapeDataString(profileId.ToString())}")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json")
        };

        request.Headers.Add("apikey", _settings.ApiKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", WriteBearerToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("Prefer", "return=representation");
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "SetRoleAndStatusAsync failed. Status: {StatusCode}. Body: {Body}",
                response.StatusCode, body);
            return false;
        }
        return true;
    }
}
