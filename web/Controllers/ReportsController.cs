using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceHub_IT.Models;
using System.Text;
using ServiceHub_IT.Services;

namespace ServiceHub_IT.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly ReportService _reportService;

    public ReportsController(ReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet]
    public IActionResult Index(ReportFilterViewModel model)
    {
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(ReportFilterViewModel model, CancellationToken cancellationToken)
    {
        var rows = await _reportService.FetchRowsAsync(
            model.ReportType,
            model.StartDate,
            model.EndDate,
            User.FindFirst("supabase_access_token")?.Value,
            cancellationToken);
        ViewBag.Rows = rows;
        ViewBag.ReportType = model.ReportType;
        ViewBag.StartDate = model.StartDate;
        ViewBag.EndDate = model.EndDate;
        return View("Preview", model);
    }

    [HttpGet]
    public async Task<IActionResult> Export(string type, string reportType, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
    {
        var rows = await _reportService.FetchRowsAsync(
            reportType,
            startDate,
            endDate,
            User.FindFirst("supabase_access_token")?.Value,
            cancellationToken);

        if (type == "excel")
        {
            var stream = await _reportService.GenerateExcelAsync(reportType, rows, cancellationToken);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{reportType.ToLower()}-report.xlsx");
        }

        if (type == "csv")
        {
            var csv = _reportService.BuildCsv(rows);
            return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"{reportType.ToLower()}-report.csv");
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Print(string reportType, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
    {
        var rows = await _reportService.FetchRowsAsync(
            reportType,
            startDate,
            endDate,
            User.FindFirst("supabase_access_token")?.Value,
            cancellationToken);
        ViewBag.Rows = rows;
        ViewBag.ReportType = reportType;
        return View("Print", rows);
    }

}
