using System.Text;
using System.Net.Http.Headers;
using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Options;
using ServiceHub_IT.Models;

namespace ServiceHub_IT.Services;

public class ReportService
{
    private readonly HttpClient _httpClient;
    private readonly SupabaseSettings _settings;
    private readonly ILogger<ReportService> _logger;

    public ReportService(HttpClient httpClient, IOptions<SupabaseSettings> options, ILogger<ReportService> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;
    }

    public Task<MemoryStream> GenerateExcelAsync(string reportType, IEnumerable<Dictionary<string, object?>> rows, CancellationToken cancellationToken = default)
    {
        var reportRows = rows?.ToList() ?? new List<Dictionary<string, object?>>();
        var columns = reportRows
            .SelectMany(row => row.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
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
                Name = GetWorksheetName(reportType)
            });

            var headerRow = new Row();
            foreach (var column in columns)
            {
                headerRow.AppendChild(CreateInlineTextCell(column));
            }
            sheetData.AppendChild(headerRow);

            foreach (var row in reportRows)
            {
                var dataRow = new Row();
                foreach (var column in columns)
                {
                    var value = row.FirstOrDefault(pair =>
                        string.Equals(pair.Key, column, StringComparison.OrdinalIgnoreCase)).Value;
                    var cellValue = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                    dataRow.AppendChild(CreateInlineTextCell(cellValue));
                }
                sheetData.AppendChild(dataRow);
            }

            worksheetPart.Worksheet.Save();
            workbookPart.Workbook.Save();
        }

        stream.Position = 0;
        return Task.FromResult(stream);
    }

    private static Cell CreateInlineTextCell(string value) => new(
        new InlineString(new Text(value) { Space = SpaceProcessingModeValues.Preserve }))
    {
        DataType = CellValues.InlineString
    };

    private static string GetWorksheetName(string? reportType)
    {
        var name = string.IsNullOrWhiteSpace(reportType) ? "Report" : reportType.Trim();
        foreach (var invalidCharacter in new[] { '\\', '/', '*', '?', ':', '[', ']' })
        {
            name = name.Replace(invalidCharacter, '_');
        }

        return name.Length > 31 ? name[..31] : name;
    }

    public string BuildCsv(IEnumerable<Dictionary<string, object?>> rows)
    {
        if (rows is null || !rows.Any())
        {
            return string.Empty;
        }

        var columns = rows.First().Keys.ToList();
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", columns.Select(QuoteCsv)));
        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(",", columns.Select(c => QuoteCsv(row.TryGetValue(c, out var value) ? value?.ToString() ?? string.Empty : string.Empty))));
        }
        return builder.ToString();
    }


    private static string QuoteCsv(string value) => value.Contains(',') || value.Contains('"') || value.Contains('\n') ? $"\"{value.Replace("\"", "\"")}" : value;

    public async Task<List<Dictionary<string, object?>>> FetchRowsAsync(
    string reportType,
    DateTime? startDate,
    DateTime? endDate,
    string? accessToken = null,
    CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(_settings.Url) || string.IsNullOrWhiteSpace(_settings.ApiKey))
    {
        throw new InvalidOperationException("Supabase is not configured for report generation.");
    }

    var table = reportType.ToLowerInvariant() switch
    {
        "assets" => "assets",
        "clients" or "users" => "profiles",
        "tickets" => "tickets",
        "departments" => "departments",
        "maintenance" => "maintenance",
        "technician performance" => "tickets",
        _ => "assets"
    };

    var query = new List<string> { "select=*" };
    if (startDate is not null)
    {
        query.Add($"created_at=gte.{startDate:yyyy-MM-dd}");
    }
    if (endDate is not null)
    {
        query.Add($"created_at=lte.{endDate:yyyy-MM-dd}");
    }

    var endpoint = $"{_settings.Url.TrimEnd('/')}/rest/v1/{table}?{string.Join('&', query)}";
    var bearerToken = !string.IsNullOrWhiteSpace(accessToken)
        ? accessToken
        : _settings.ServiceRoleKey;
    if (string.IsNullOrWhiteSpace(bearerToken))
    {
        throw new InvalidOperationException("An authenticated Supabase token or Service Role Key is required to generate reports.");
    }

    using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
    request.Headers.Clear();
    request.Headers.Add("apikey", _settings.ApiKey);
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
    request.Headers.Accept.Add(
        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

    using var response = await _httpClient.SendAsync(request, cancellationToken);
    if (!response.IsSuccessStatusCode)
    {
        _logger.LogWarning("Supabase report fetch failed for {Type}. Status: {Status}",
            reportType, response.StatusCode);
        throw new HttpRequestException(
            $"Supabase report fetch failed with status {(int)response.StatusCode}.",
            null,
            response.StatusCode);
    }

    var payload = await response.Content.ReadAsStringAsync(cancellationToken);
    var data = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, System.Text.Json.JsonElement>>>(
        payload,
        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        ?? new List<Dictionary<string, System.Text.Json.JsonElement>>();

    return data.Select(item => item.ToDictionary(
        kvp => kvp.Key,
        kvp => (object?)(kvp.Value.ValueKind == System.Text.Json.JsonValueKind.String
            ? kvp.Value.GetString()
            : kvp.Value.ToString())
    )).ToList();
}

}
