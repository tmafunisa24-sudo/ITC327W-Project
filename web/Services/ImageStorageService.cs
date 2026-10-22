using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using ServiceHub_IT.Models;

namespace ServiceHub_IT.Services;

public sealed class ImageStorageService
{
    public const long MaxFileSize = 5 * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, string> AllowedContentTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif"
    };

    private readonly HttpClient _httpClient;
    private readonly SupabaseSettings _settings;
    private readonly ILogger<ImageStorageService> _logger;

    public ImageStorageService(HttpClient httpClient, IOptions<SupabaseSettings> options, ILogger<ImageStorageService> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;
    }

    public static bool IsAllowed(IFormFile file, out string error)
    {
        if (file.Length <= 0)
        {
            error = "The selected image is empty.";
            return false;
        }

        if (file.Length > MaxFileSize)
        {
            error = "Images must be 5 MB or smaller.";
            return false;
        }

        if (!AllowedContentTypes.ContainsKey(file.ContentType))
        {
            error = "Only JPG, PNG, WEBP, and GIF images are supported.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static string CreateObjectName(IFormFile file)
    {
        if (!AllowedContentTypes.TryGetValue(file.ContentType, out var extension))
        {
            throw new InvalidDataException("Only JPG, PNG, WEBP, and GIF images are supported.");
        }

        return $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{extension}";
    }

    public async Task<string> UploadAsync(IFormFile file, string path, string? accessToken, CancellationToken cancellationToken = default)
    {
        if (!IsAllowed(file, out var error))
        {
            throw new InvalidDataException(error);
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("An authenticated Supabase session is required to upload an image.");
        }

        var encodedPath = string.Join('/', path.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.EscapeDataString));
        var endpoint = $"{_settings.Url.TrimEnd('/')}/storage/v1/object/images/{encodedPath}";
        var publicUrl = $"{_settings.Url.TrimEnd('/')}/storage/v1/object/public/images/{encodedPath}";

        try
        {
            using var stream = file.OpenReadStream();
            using var content = new StreamContent(stream);
            content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = content };
            request.Headers.Add("apikey", _settings.ApiKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Add("x-upsert", "false");

            _logger.LogInformation("Supabase Storage request. Method={Method}, Endpoint={Endpoint}, Path={Path}", HttpMethod.Post, endpoint, path);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("Supabase Storage response. Method={Method}, Endpoint={Endpoint}, Status={StatusCode}, Response={Response}", HttpMethod.Post, endpoint, response.StatusCode, responseBody);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Image upload failed. Status={StatusCode}, Path={Path}, Response={Response}", response.StatusCode, path, responseBody);
                throw new HttpRequestException("Supabase Storage rejected the image upload.");
            }

            return publicUrl;
        }
        catch (Exception ex) when (ex is not InvalidDataException && ex is not InvalidOperationException && ex is not HttpRequestException)
        {
            _logger.LogError(ex, "Image upload failed for storage path {Path}.", path);
            throw new HttpRequestException("The image could not be uploaded.", ex);
        }
    }
}