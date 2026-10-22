namespace ServiceHub_IT.DTOs;

public class TechnicianPerformanceRow
{
    public string TechnicianName { get; set; } = string.Empty;
    public int AssignedTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public double AverageResolutionHours { get; set; }
    public string RatingLabel { get; set; } = string.Empty;
}

public class TechnicianPerformanceViewModel
{
    public string TopTechnicianName { get; set; } = string.Empty;
    public int TopTechnicianResolvedTickets { get; set; }
    public double TopTechnicianAverageResolutionHours { get; set; }
    public string TopTechnicianRatingLabel { get; set; } = string.Empty;
    public IReadOnlyList<TechnicianPerformanceRow> Leaderboard { get; set; } = Array.Empty<TechnicianPerformanceRow>();
    public IReadOnlyList<string> ChartLabels { get; set; } = Array.Empty<string>();
    public IReadOnlyList<int> ResolvedTicketCounts { get; set; } = Array.Empty<int>();
    public IReadOnlyList<double> AverageHours { get; set; } = Array.Empty<double>();
}
