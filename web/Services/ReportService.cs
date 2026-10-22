using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Options;
using ServiceHub_IT.Models;

namespace ServiceHub_IT.Services;

public sealed class ReportService
{
    private const int PageSize = 500;
    private const int MaximumRows = 5000;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private readonly SupabaseSettings _settings;
    private readonly ILogger<ReportService> _logger;

    public ReportService(HttpClient httpClient, IOptions<SupabaseSettings> options, ILogger<ReportService> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<ReportFilterViewModel> GenerateReportAsync(
        ReportFilterViewModel filters,
        string? accessToken,
        string role,
        string? profileId,
        CancellationToken cancellationToken = default)
    {
        filters.ReportType = NormalizeReportType(filters.ReportType);
        filters.Rows.Clear();
        filters.Summary.Clear();
        filters.ErrorMessage = null;
        filters.Notice = null;
        filters.TotalRecords = 0;
        filters.RecordsReturned = 0;
        filters.IsTruncated = false;
        filters.GeneratedAtUtc = DateTimeOffset.UtcNow;

        if (string.IsNullOrWhiteSpace(_settings.Url) || string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            filters.ErrorMessage = "Reports are unavailable because the Supabase URL or API key is not configured.";
            return filters;
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            filters.ErrorMessage = "Your session does not contain a Supabase access token. Sign in again and retry.";
            return filters;
        }

        var isTechnician = string.Equals(role, "Technician", StringComparison.OrdinalIgnoreCase);
        if (isTechnician && (filters.ReportType is "Tickets" or "Technician Performance"))
        {
            if (!Guid.TryParse(profileId, out var technicianId) || technicianId == Guid.Empty)
            {
                filters.ErrorMessage = "Your technician profile could not be verified. Contact an administrator.";
                return filters;
            }

            filters.TechnicianId = technicianId;
        }

        try
        {
            var (table, dateColumn, columns) = GetReportDefinition(filters.ReportType);
            var records = await FetchRecordsAsync(
                table,
                dateColumn,
                columns,
                filters,
                accessToken,
                cancellationToken);

            filters.TotalRecords = records.TotalRecords;
            filters.RecordsReturned = records.Items.Count;
            filters.IsTruncated = records.IsTruncated;
            filters.Rows = filters.ReportType == "Technician Performance"
                ? BuildTechnicianPerformanceRows(records.Items)
                : records.Items.Select(item => BuildReportRow(filters.ReportType, item)).ToList();
            BuildSummary(filters, records.Items);

            if (filters.IsTruncated)
            {
                filters.Notice = $"Showing the first {filters.RecordsReturned:N0} of {filters.TotalRecords:N0} matching records. Narrow the date range or filters to see a complete report.";
            }
            else if (filters.Rows.Count == 0)
            {
                filters.Notice = "No records matched the selected filters.";
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Unable to retrieve report data from Supabase.");
            filters.ErrorMessage = "The report data source could not be reached. Try again later.";
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Supabase returned an invalid report payload.");
            filters.ErrorMessage = "The report data source returned data in an unexpected format.";
        }
        catch (UriFormatException ex)
        {
            _logger.LogWarning(ex, "The Supabase URL could not be used for a report request.");
            filters.ErrorMessage = "The report data source is not configured with a valid URL.";
        }

        return filters;
    }

    public string BuildCsv(ReportFilterViewModel report)
    {
        var builder = new StringBuilder();
        AppendCsvRow(builder, new[] { "Report", report.ReportType });
        AppendCsvRow(builder, new[] { "Generated (UTC)", report.GeneratedAtUtc?.ToString("u", CultureInfo.InvariantCulture) ?? string.Empty });
        foreach (var filter in report.GetAppliedFilters().Where(filter => filter.Key != "Report"))
        {
            AppendCsvRow(builder, new[] { filter.Key, filter.Value });
        }

        foreach (var summary in report.Summary)
        {
            AppendCsvRow(builder, new[] { summary.Label, summary.Value });
        }

        AppendCsvRow(builder, Array.Empty<string>());
        var columns = GetDisplayColumns(report.ReportType);
        AppendCsvRow(builder, columns);
        foreach (var row in report.Rows)
        {
            AppendCsvRow(builder, columns.Select(column => row.Values.TryGetValue(column, out var value) ? value : string.Empty));
        }

        return builder.ToString();
    }

    public MemoryStream GenerateExcel(ReportFilterViewModel report)
    {
        var stream = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook, true))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new Worksheet(sheetData);

            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
            sheets.AppendChild(new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = GetSheetName(report.ReportType)
            });

            AppendExcelRow(sheetData, new[] { $"{report.ReportType} report" });
            AppendExcelRow(sheetData, new[] { $"Generated (UTC): {report.GeneratedAtUtc?.ToString("u", CultureInfo.InvariantCulture)}" });
            foreach (var filter in report.GetAppliedFilters().Where(filter => filter.Key != "Report"))
            {
                AppendExcelRow(sheetData, new[] { $"{filter.Key}: {filter.Value}" });
            }

            foreach (var summary in report.Summary)
            {
                AppendExcelRow(sheetData, new[] { summary.Label, summary.Value });
            }

            AppendExcelRow(sheetData, Array.Empty<string>());
            var columns = GetDisplayColumns(report.ReportType);
            AppendExcelRow(sheetData, columns);
            foreach (var row in report.Rows)
            {
                AppendExcelRow(sheetData, columns.Select(column => row.Values.TryGetValue(column, out var value) ? value : string.Empty));
            }

            worksheetPart.Worksheet.Save();
            workbookPart.Workbook.Save();
        }

        stream.Position = 0;
        return stream;
    }

    private async Task<FetchResult> FetchRecordsAsync(
        string table,
        string dateColumn,
        string[] columns,
        ReportFilterViewModel filters,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var query = new List<string> { $"select={string.Join(",", columns)}" };
        if (filters.StartDate.HasValue)
        {
            var start = DateTime.SpecifyKind(filters.StartDate.Value.Date, DateTimeKind.Utc)
                .ToString("O", CultureInfo.InvariantCulture);
            query.Add($"{dateColumn}=gte.{Uri.EscapeDataString(start)}");
        }

        if (filters.EndDate.HasValue)
        {
            var endExclusive = DateTime.SpecifyKind(filters.EndDate.Value.Date.AddDays(1), DateTimeKind.Utc)
                .ToString("O", CultureInfo.InvariantCulture);
            query.Add($"{dateColumn}=lt.{Uri.EscapeDataString(endExclusive)}");
        }

        AddEqualsFilter(query, "status", filters.Status);
        if (table == "tickets")
        {
            AddEqualsFilter(query, "priority", filters.Priority);
            AddEqualsFilter(query, "device_type", filters.DeviceType);
            if (filters.TechnicianId.HasValue)
            {
                query.Add($"technician_id=eq.{filters.TechnicianId.Value:D}");
            }
        }

        var items = new List<Dictionary<string, JsonElement>>();
        int? totalRecords = null;
        while (items.Count < MaximumRows)
        {
            var pageEnd = Math.Min(items.Count + PageSize, MaximumRows) - 1;
            var requestedCount = pageEnd - items.Count + 1;
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{_settings.Url.TrimEnd('/')}/rest/v1/{table}?{string.Join("&", query)}");
            request.Headers.TryAddWithoutValidation("apikey", _settings.ApiKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.TryAddWithoutValidation("Prefer", "count=exact");
            request.Headers.TryAddWithoutValidation("Range-Unit", "items");
            request.Headers.TryAddWithoutValidation("Range", $"{items.Count}-{pageEnd}");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Supabase report query for {ReportTable} failed with HTTP {StatusCode}.", table, (int)response.StatusCode);
                throw new HttpRequestException($"Supabase returned HTTP {(int)response.StatusCode}.");
            }

            totalRecords ??= ReadTotalCount(response);
            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(contentStream, cancellationToken: cancellationToken);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new JsonException("Expected an array from the report data source.");
            }

            var page = document.RootElement.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.Object)
                .Select(item => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(item.GetRawText(), JsonOptions) ?? new())
                .ToList();
            items.AddRange(page);

            if (page.Count < requestedCount)
            {
                break;
            }
        }

        var returned = items.Count;
        var total = totalRecords ?? returned;
        return new FetchResult(items, total, total > returned || (returned == MaximumRows && totalRecords is null));
    }

    private static int? ReadTotalCount(HttpResponseMessage response)
    {
        if (response.Content.Headers.ContentRange?.Length is long length && length <= int.MaxValue)
        {
            return (int)length;
        }

        return null;
    }

    private static void AddEqualsFilter(List<string> query, string column, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            query.Add($"{column}=eq.{Uri.EscapeDataString(value.Trim())}");
        }
    }

    private static (string Table, string DateColumn, string[] Columns) GetReportDefinition(string reportType)
    {
        return reportType switch
        {
            "Tickets" or "Technician Performance" => (
                "tickets",
                "created_at",
                new[] { "id", "ticket_number", "status", "priority", "device_type", "problem_category", "category", "technician_id", "assigned_technician", "created_at", "closed_at" }),
            "Assets" => (
                "assets",
                "purchase_date",
                new[] { "id", "asset_tag", "asset_name", "category", "status", "purchase_date", "warranty_expiry" }),
            "Maintenance" => (
                "maintenance",
                "maintenance_date",
                new[] { "id", "asset_id", "technician_id", "status", "maintenance_date", "next_service_date", "maintenance_cost" }),
            _ => throw new JsonException("Unsupported report type.")
        };
    }

    private static string NormalizeReportType(string reportType)
    {
        var supported = new[] { "Tickets", "Assets", "Maintenance", "Technician Performance" };
        return supported.FirstOrDefault(type => string.Equals(type, reportType, StringComparison.OrdinalIgnoreCase))
            ?? reportType;
    }

    private static ReportRow BuildReportRow(string reportType, Dictionary<string, JsonElement> item)
    {
        var values = reportType switch
        {
            "Tickets" => new Dictionary<string, string>
            {
                ["Ticket"] = GetValue(item, "ticket_number") ?? GetValue(item, "id") ?? string.Empty,
                ["Status"] = GetValue(item, "status") ?? string.Empty,
                ["Priority"] = GetValue(item, "priority") ?? string.Empty,
                ["Device"] = GetValue(item, "device_type") ?? string.Empty,
                ["Category"] = GetValue(item, "problem_category") ?? GetValue(item, "category") ?? string.Empty,
                ["Technician ID"] = GetValue(item, "technician_id") ?? GetValue(item, "assigned_technician") ?? "Unassigned",
                ["Created"] = FormatDate(GetValue(item, "created_at")),
                ["Closed"] = FormatDate(GetValue(item, "closed_at"))
            },
            "Assets" => new Dictionary<string, string>
            {
                ["Asset tag"] = GetValue(item, "asset_tag") ?? GetValue(item, "id") ?? string.Empty,
                ["Asset"] = GetValue(item, "asset_name") ?? string.Empty,
                ["Category"] = GetValue(item, "category") ?? string.Empty,
                ["Status"] = GetValue(item, "status") ?? string.Empty,
                ["Purchase date"] = FormatDate(GetValue(item, "purchase_date")),
                ["Warranty expiry"] = FormatDate(GetValue(item, "warranty_expiry"))
            },
            "Maintenance" => new Dictionary<string, string>
            {
                ["Record"] = GetValue(item, "id") ?? string.Empty,
                ["Asset ID"] = GetValue(item, "asset_id") ?? string.Empty,
                ["Technician ID"] = GetValue(item, "technician_id") ?? string.Empty,
                ["Status"] = GetValue(item, "status") ?? string.Empty,
                ["Maintenance date"] = FormatDate(GetValue(item, "maintenance_date")),
                ["Next service"] = FormatDate(GetValue(item, "next_service_date")),
                ["Cost"] = GetValue(item, "maintenance_cost") ?? string.Empty
            },
            _ => new Dictionary<string, string>()
        };

        return new ReportRow { Values = values };
    }

    private static List<ReportRow> BuildTechnicianPerformanceRows(List<Dictionary<string, JsonElement>> items)
    {
        return items
            .GroupBy(item => GetValue(item, "technician_id") ?? GetValue(item, "assigned_technician") ?? "Unassigned", StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var resolved = group
                    .Where(IsResolved)
                    .Select(item => (Created: ParseDate(GetValue(item, "created_at")), Closed: ParseDate(GetValue(item, "closed_at"))))
                    .Where(times => times.Created.HasValue && times.Closed.HasValue && times.Closed >= times.Created)
                    .Select(times => (times.Closed!.Value - times.Created!.Value).TotalDays)
                    .ToList();
                var values = new Dictionary<string, string>
                {
                    ["Technician"] = group.Key,
                    ["Assigned"] = group.Count().ToString(CultureInfo.InvariantCulture),
                    ["Resolved / closed"] = group.Count(IsResolved).ToString(CultureInfo.InvariantCulture),
                    ["Outstanding"] = group.Count(item => !IsResolved(item)).ToString(CultureInfo.InvariantCulture),
                    ["Average resolution days"] = resolved.Count == 0
                        ? "Not available"
                        : resolved.Average().ToString("0.0", CultureInfo.InvariantCulture)
                };
                return new ReportRow { Values = values };
            })
            .OrderBy(row => row.Values["Technician"], StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void BuildSummary(ReportFilterViewModel report, List<Dictionary<string, JsonElement>> items)
    {
        report.Summary.Add(new ReportSummaryItem { Label = "Matching records", Value = report.TotalRecords.ToString("N0", CultureInfo.CurrentCulture) });
        if (report.IsTruncated)
        {
            report.Summary.Add(new ReportSummaryItem { Label = "Loaded for calculations", Value = report.RecordsReturned.ToString("N0", CultureInfo.CurrentCulture) });
        }

        if (report.ReportType == "Tickets")
        {
            AddGroupSummary(report, items, "status", "Status");
            AddGroupSummary(report, items, "priority", "Priority");
            AddGroupSummary(report, items, "device_type", "Device type");
            AddGroupSummary(report, items, "problem_category", "Problem category");
            AddMonthlyTrendSummary(report, items, "created_at", "Created");
            AddMonthlyTrendSummary(report, items.Where(IsResolved).ToList(), "closed_at", "Resolved / closed");
        }
        else if (report.ReportType == "Technician Performance")
        {
            report.Summary.Add(new ReportSummaryItem { Label = "Technicians with assigned tickets", Value = report.Rows.Count.ToString("N0", CultureInfo.CurrentCulture) });
            report.Summary.Add(new ReportSummaryItem
            {
                Label = "Resolved / closed tickets",
                Value = items.Count(IsResolved).ToString("N0", CultureInfo.CurrentCulture)
            });
        }
        else if (report.ReportType == "Assets")
        {
            AddGroupSummary(report, items, "status", "Status");
            AddGroupSummary(report, items, "category", "Category");
            var expiryLimit = DateTime.UtcNow.Date.AddDays(30);
            var expiring = items.Count(item =>
            {
                var expiry = ParseDate(GetValue(item, "warranty_expiry"));
                return expiry >= DateTime.UtcNow.Date && expiry < expiryLimit;
            });
            report.Summary.Add(new ReportSummaryItem { Label = "Warranty expires in next 30 days", Value = expiring.ToString("N0", CultureInfo.CurrentCulture) });
        }
        else if (report.ReportType == "Maintenance")
        {
            AddGroupSummary(report, items, "status", "Status");
            var costs = items.Select(item => GetValue(item, "maintenance_cost"))
                .Where(value => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
                .Select(value => decimal.Parse(value!, CultureInfo.InvariantCulture))
                .Sum();
            report.Summary.Add(new ReportSummaryItem { Label = "Recorded maintenance cost", Value = costs.ToString("C", CultureInfo.CurrentCulture) });
        }
    }

    private static void AddGroupSummary(ReportFilterViewModel report, List<Dictionary<string, JsonElement>> items, string field, string label)
    {
        foreach (var group in items
            .Select(item => GetValue(item, field))
            .GroupBy(value => string.IsNullOrWhiteSpace(value) ? "Not specified" : value, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            report.Summary.Add(new ReportSummaryItem
            {
                Label = $"{label}: {group.Key}",
                Value = group.Count().ToString("N0", CultureInfo.CurrentCulture)
            });
        }
    }

    private static void AddMonthlyTrendSummary(
        ReportFilterViewModel report,
        List<Dictionary<string, JsonElement>> items,
        string dateField,
        string label)
    {
        var monthlyCounts = items
            .Select(item => ParseDate(GetValue(item, dateField)))
            .Where(date => date.HasValue)
            .GroupBy(date => date!.Value.ToString("yyyy-MM", CultureInfo.InvariantCulture))
            .OrderBy(group => group.Key, StringComparer.Ordinal);

        foreach (var month in monthlyCounts)
        {
            report.Summary.Add(new ReportSummaryItem
            {
                Label = $"{label} {month.Key}",
                Value = month.Count().ToString("N0", CultureInfo.CurrentCulture)
            });
        }
    }

    private static bool IsResolved(Dictionary<string, JsonElement> item)
    {
        var status = GetValue(item, "status");
        return string.Equals(status, "Resolved", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Closed", StringComparison.OrdinalIgnoreCase);
    }

    private static DateTime? ParseDate(string? value)
    {
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var date)
            ? date
            : null;
    }

    private static string FormatDate(string? value)
    {
        return ParseDate(value)?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? value ?? string.Empty;
    }

    private static string? GetValue(Dictionary<string, JsonElement> item, string field)
    {
        if (!item.TryGetValue(field, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
    }

    private static string[] GetDisplayColumns(string reportType)
    {
        return reportType switch
        {
            "Tickets" => new[] { "Ticket", "Status", "Priority", "Device", "Category", "Technician ID", "Created", "Closed" },
            "Technician Performance" => new[] { "Technician", "Assigned", "Resolved / closed", "Outstanding", "Average resolution days" },
            "Assets" => new[] { "Asset tag", "Asset", "Category", "Status", "Purchase date", "Warranty expiry" },
            "Maintenance" => new[] { "Record", "Asset ID", "Technician ID", "Status", "Maintenance date", "Next service", "Cost" },
            _ => Array.Empty<string>()
        };
    }

    private static void AppendCsvRow(StringBuilder builder, IEnumerable<string> values)
    {
        builder.AppendLine(string.Join(",", values.Select(EscapeCsv)));
    }

    private static string EscapeCsv(string value)
    {
        var safe = value.TrimStart() is { Length: > 0 } trimmed && trimmed[0] is '=' or '+' or '-' or '@'
            ? $"'{value}"
            : value;
        return $"\"{safe.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private static void AppendExcelRow(SheetData sheetData, IEnumerable<string> values)
    {
        var row = new Row();
        foreach (var value in values)
        {
            row.AppendChild(new Cell(new InlineString(new Text(value) { Space = SpaceProcessingModeValues.Preserve }))
            {
                DataType = CellValues.InlineString
            });
        }

        sheetData.AppendChild(row);
    }

    private static string GetSheetName(string reportType)
    {
        var sanitized = string.Concat(reportType.Where(character => !("[]:*?/\\").Contains(character)));
        return sanitized.Length <= 31 ? sanitized : sanitized[..31];
    }

    private sealed record FetchResult(List<Dictionary<string, JsonElement>> Items, int TotalRecords, bool IsTruncated);
}
