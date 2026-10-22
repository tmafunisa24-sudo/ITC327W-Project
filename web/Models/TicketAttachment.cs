using System.Text.Json.Serialization;

namespace ServiceHub_IT.Models;

public class TicketAttachment
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("ticket_id")]
    public Guid TicketId { get; set; }

    [JsonPropertyName("file_url")]
    public string FileUrl { get; set; } = string.Empty;

    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("file_type")]
    public string FileType { get; set; } = string.Empty;

    [JsonPropertyName("file_size")]
    public long FileSize { get; set; }

    [JsonPropertyName("uploaded_by")]
    public Guid UploadedBy { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}