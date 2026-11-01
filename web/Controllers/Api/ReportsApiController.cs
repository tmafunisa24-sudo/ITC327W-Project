// Controllers/Api/ReportsApiController.cs

using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceHub_IT.DTOs.Api;
using ServiceHub_IT.Services;

namespace ServiceHub_IT.Controllers.Api;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public sealed class ReportsApiController : ControllerBase
{
    private readonly ReportService _reportService;
    private readonly ILogger<ReportsApiController> _logger;

    public ReportsApiController(
        ReportService reportService,
        ILogger<ReportsApiController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────
    // GET /api/reports/preview
    // Returns rows as JSON for the mobile preview table.
    // ────────────────────────────────────────────────────────
    [HttpGet("preview")]
    public async Task<IActionResult> Preview(
        [FromQuery] string reportType,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reportType))
        {
            return BadRequest(new ApiErrorDto { Error = "Report type is required." });
        }

        try
        {
            var rows = await FetchRowsAsync(reportType, startDate, endDate, cancellationToken);

            // Convert to a friendly shape for JSON: array of { column: value }
            var payload = new
            {
                reportType,
                startDate,
                endDate,
                rowCount = rows.Count,
                rows,
            };

            return Ok(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to generate report preview for {Type}.", reportType);
            return StatusCode(500, new ApiErrorDto
            {
                Error = "Unable to generate the report right now."
            });
        }
    }

    // ────────────────────────────────────────────────────────
    // GET /api/reports/export/csv
    // ────────────────────────────────────────────────────────
    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] string reportType,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reportType))
        {
            return BadRequest(new ApiErrorDto { Error = "Report type is required." });
        }

        try
        {
            var rows = await FetchRowsAsync(reportType, startDate, endDate, cancellationToken);
            var csv = _reportService.BuildCsv(rows);
            var bytes = Encoding.UTF8.GetBytes(csv);
            var filename = $"{reportType.ToLowerInvariant().Replace(' ', '-')}-report.csv";

            return File(bytes, "text/csv", filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to export CSV for {Type}.", reportType);
            return StatusCode(500, new ApiErrorDto
            {
                Error = "Unable to export the CSV right now."
            });
        }
    }

    // ────────────────────────────────────────────────────────
    // GET /api/reports/export/excel
    // ────────────────────────────────────────────────────────
    [HttpGet("export/excel")]
    public async Task<IActionResult> ExportExcel(
        [FromQuery] string reportType,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reportType))
        {
            return BadRequest(new ApiErrorDto { Error = "Report type is required." });
        }

        try
        {
            var rows = await FetchRowsAsync(reportType, startDate, endDate, cancellationToken);
            var stream = await _reportService.GenerateExcelAsync(reportType, rows, cancellationToken);
            var filename = $"{reportType.ToLowerInvariant().Replace(' ', '-')}-report.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to export Excel for {Type}.", reportType);
            return StatusCode(500, new ApiErrorDto
            {
                Error = "Unable to export the Excel file right now."
            });
        }
    }

    // ────────────────────────────────────────────────────────
    // Helper: fetch rows from Supabase, mirroring MVC logic.
    // ────────────────────────────────────────────────────────
    private async Task<List<Dictionary<string, object?>>> FetchRowsAsync(
        string reportType,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var rows = await _reportService.FetchRowsAsync(
            reportType, startDate, endDate, cancellationToken);
        return rows;
    }
}