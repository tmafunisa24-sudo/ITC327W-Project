namespace ServiceHub_IT.DTOs;

public class DashboardMetricsDto
{
    public int TotalAssets { get; set; }
    public int AssignedAssets { get; set; }
    public int AvailableAssets { get; set; }
    public int Employees { get; set; }
    public int Technicians { get; set; }
    public int OpenTickets { get; set; }
    public int ClosedTickets { get; set; }
    public int MaintenanceDue { get; set; }
}
