namespace ServiceHub_IT.Models;

using System.Text.Json.Serialization;

public class Ticket
{
    public string? id { get; set; }
    public string? ticket_number { get; set; }
    [JsonPropertyName("employee_id")]
    public Guid? employee_id { get; set; }
    public string? device_type { get; set; }
    public string? problem_category { get; set; }
    public string? problem_type { get; set; }
    public long? service_catalog_id { get; set; }
    public string title { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public string requester { get; set; } = string.Empty;
    public string priority { get; set; } = "Medium";
    public string status { get; set; } = "Open";
    [JsonPropertyName("technician_id")]
    public Guid? technician_id { get; set; }
    // Kept for compatibility with older ticket rows that stored the technician email.
    public string? assigned_technician { get; set; }
    public string category { get; set; } = string.Empty;
    public string? payment_method { get; set; }
    public string payment_status { get; set; } = "Pending";
    public string? screenshot_url { get; set; }
    public string? image_url { get; set; }
    public string? comments { get; set; }
    public DateTime? due_date { get; set; }
    public DateTime? closed_at { get; set; }
    public DateTime? created_at { get; set; } = DateTime.UtcNow;
    public DateTime? updated_at { get; set; } = DateTime.UtcNow;
}
