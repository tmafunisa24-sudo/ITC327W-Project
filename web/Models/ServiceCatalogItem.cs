using System.Text.Json.Serialization;

namespace ServiceHub_IT.Models;

public class ServiceCatalogItem
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("device_type")]
    public string DeviceType { get; set; } = string.Empty;

    [JsonPropertyName("problem_category")]
    public string ProblemCategory { get; set; } = string.Empty;

    [JsonPropertyName("problem_type")]
    public string ProblemType { get; set; } = string.Empty;

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;
}