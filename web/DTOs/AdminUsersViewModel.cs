using ServiceHub_IT.Models;

namespace ServiceHub_IT.DTOs;

public class AdminUsersViewModel
{
    public int TotalUsers { get; set; }
    public int PendingApprovals { get; set; }
    public int Employees { get; set; }
    public int Technicians { get; set; }
    public int Administrators { get; set; }
    public IReadOnlyList<Profile> Profiles { get; set; } = Array.Empty<Profile>();
}
