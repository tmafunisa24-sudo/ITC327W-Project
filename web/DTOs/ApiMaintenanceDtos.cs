// Models/DTOs/ApiMaintenanceDtos.cs

namespace ServiceHub_IT.DTOs.Api;

public sealed class MaintenanceRequest
{
    public Guid? AssetId { get; set; }
    public Guid? TechnicianId { get; set; }
    public string? Problem { get; set; }
    public string? Solution { get; set; }
    public DateTime? MaintenanceDate { get; set; }
    public DateTime? NextServiceDate { get; set; }
    public decimal? MaintenanceCost { get; set; }
    public string? Status { get; set; }
}