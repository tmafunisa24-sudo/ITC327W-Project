// Summary: Transfers dashboard data between application layers.
namespace ServiceHub_IT.DTOs;

public class DashboardDto
{
    public int OpenTickets { get; set; }
    public int AssetsManaged { get; set; }
    public int ResolvedToday { get; set; }
}
