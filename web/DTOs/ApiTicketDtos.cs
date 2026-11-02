// Models/DTOs/ApiTicketDtos.cs
using Microsoft.AspNetCore.Http;

namespace ServiceHub_IT.DTOs.Api;

public sealed class CreateTicketRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string ProblemCategory { get; set; } = string.Empty;
    public string ProblemType { get; set; } = string.Empty;
    public long? ServiceCatalogId { get; set; }
    public string Priority { get; set; } = "Medium";
    public string Category { get; set; } = string.Empty;
    public List<IFormFile> Images { get; set; } = new();
}

// Models/DTOs/ApiTicketDtos.cs

public sealed class UpdateTicketStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public Guid? AssignedTechnicianId { get; set; }
    public bool AssignmentSubmitted { get; set; }
    public string? Comment { get; set; }
    public DateTime? DueDate { get; set; }
    public string? ScreenshotUrl { get; set; }
}
