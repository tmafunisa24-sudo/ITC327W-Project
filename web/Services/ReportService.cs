using System.Text;
using System.Net.Http.Headers;
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
        var stream = new MemoryStream();
        var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook, true);

        var workbookPart = document.AddWorkbookPart();
        workbookPart.Workbook = new Workbook();

        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        worksheetPart.Worksheet = new Worksheet(new SheetData());

        var sheets = workbookPart.Workbook.AppendChild(new Sheets());
        sheets.AppendChild(new Sheet { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = reportType });

        var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
        if (sheetData is null)
        {
            sheetData = new SheetData();
            worksheetPart.Worksheet.AppendChild(sheetData);
        }

        var headerRow = new Row();
        var columns = rows.FirstOrDefault()?.Keys.ToList() ?? new List<string>();
        foreach (var column in columns)
        {
            headerRow.AppendChild(new Cell(new InlineString(new Text(column))) { DataType = CellValues.InlineString });
        }
        sheetData.AppendChild(headerRow);

        foreach (var row in rows)
        {
            var dataRow = new Row();
            foreach (var column in columns)
            {
                var cellValue = row.TryGetValue(column, out var value) ? value?.ToString() ?? string.Empty : string.Empty;
                dataRow.AppendChild(new Cell(new InlineString(new Text(cellValue))) { DataType = CellValues.InlineString });
            }
            sheetData.AppendChild(dataRow);
        }

        workbookPart.Workbook.Save();
        document.Dispose();
        stream.Position = 0;
        return Task.FromResult(stream);
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
    CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(_settings.Url) || string.IsNullOrWhiteSpace(_settings.ApiKey))
    {
        return new List<Dictionary<string, object?>>();
    }

    var table = reportType.ToLowerInvariant() switch
    {
        "assets" => "assets",
        "clients" => "profiles",
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
    using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
    request.Headers.Clear();
    request.Headers.Add("apikey", _settings.ApiKey);
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
    request.Headers.Accept.Add(
        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

    using var response = await _httpClient.SendAsync(request, cancellationToken);
    if (!response.IsSuccessStatusCode)
    {
        _logger.LogWarning("Supabase report fetch failed for {Type}. Status: {Status}",
            reportType, response.StatusCode);
        return new List<Dictionary<string, object?>>();
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
