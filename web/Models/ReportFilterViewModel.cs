using System.ComponentModel.DataAnnotations;

namespace ServiceHub_IT.Models;

public sealed class ReportFilterViewModel : IValidatableObject
{
    private static readonly HashSet<string> SupportedReportTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Tickets",
        "Assets",
        "Maintenance",
        "Technician Performance"
    };

    [Required]
    public string ReportType { get; set; } = "Tickets";

    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    [StringLength(64)]
    public string? Status { get; set; }

    [StringLength(64)]
    public string? Priority { get; set; }

    [StringLength(64)]
    public string? DeviceType { get; set; }

    public Guid? TechnicianId { get; set; }

    public List<ReportSummaryItem> Summary { get; set; } = new();

    public List<ReportRow> Rows { get; set; } = new();

    public int TotalRecords { get; set; }

    public int RecordsReturned { get; set; }

    public bool IsTruncated { get; set; }

    public DateTimeOffset? GeneratedAtUtc { get; set; }

    public string? ErrorMessage { get; set; }

    public string? Notice { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!SupportedReportTypes.Contains(ReportType))
        {
            yield return new ValidationResult("Select a supported report type.", new[] { nameof(ReportType) });
        }

        if (StartDate.HasValue && EndDate.HasValue && StartDate.Value.Date > EndDate.Value.Date)
        {
            yield return new ValidationResult("The start date must be on or before the end date.", new[] { nameof(StartDate), nameof(EndDate) });
        }

        if (EndDate?.Date == DateTime.MaxValue.Date)
        {
            yield return new ValidationResult("The end date is outside the supported range.", new[] { nameof(EndDate) });
        }

        if (TechnicianId == Guid.Empty)
        {
            yield return new ValidationResult("Select a valid technician.", new[] { nameof(TechnicianId) });
        }

        var usesTicketData = string.Equals(ReportType, "Tickets", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ReportType, "Technician Performance", StringComparison.OrdinalIgnoreCase);
        if (!usesTicketData && (!string.IsNullOrWhiteSpace(Priority) || !string.IsNullOrWhiteSpace(DeviceType) || TechnicianId.HasValue))
        {
            yield return new ValidationResult("Priority, device type, and technician filters are available only for ticket reports.", new[] { nameof(ReportType) });
        }
    }

    public IReadOnlyList<KeyValuePair<string, string>> GetAppliedFilters()
    {
        var filters = new List<KeyValuePair<string, string>>
        {
            new("Report", ReportType)
        };

        if (StartDate.HasValue)
        {
            filters.Add(new("Start date", StartDate.Value.ToString("yyyy-MM-dd")));
        }

        if (EndDate.HasValue)
        {
            filters.Add(new("End date", EndDate.Value.ToString("yyyy-MM-dd")));
        }

        if (!string.IsNullOrWhiteSpace(Status))
        {
            filters.Add(new("Status", Status.Trim()));
        }

        var usesTicketData = string.Equals(ReportType, "Tickets", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ReportType, "Technician Performance", StringComparison.OrdinalIgnoreCase);
        if (usesTicketData && !string.IsNullOrWhiteSpace(Priority))
        {
            filters.Add(new("Priority", Priority.Trim()));
        }

        if (usesTicketData && !string.IsNullOrWhiteSpace(DeviceType))
        {
            filters.Add(new("Device type", DeviceType.Trim()));
        }

        if (usesTicketData && TechnicianId.HasValue)
        {
            filters.Add(new("Technician ID", TechnicianId.Value.ToString()));
        }

        return filters;
    }
}

public sealed class ReportSummaryItem
{
    public string Label { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;
}

public sealed class ReportRow
{
    public Dictionary<string, string> Values { get; init; } = new();
}
