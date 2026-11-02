using ServiceHub_IT.Models;

namespace ServiceHub_IT.DTOs;

public class TechnicianDashboardViewModel
{
    public IReadOnlyList<Ticket> Tickets { get; set; } = Array.Empty<Ticket>();
    public int DueSoonCount { get; set; }
    public int CompletedCount { get; set; }
}
