using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceHub_IT.Models;
using ServiceHub_IT.Services;

namespace ServiceHub_IT.Controllers;

[Authorize(Roles = "Admin,Technician")]
public sealed class ReportsController : Controller
{
    private readonly ReportService _reportService;

    public ReportsController(ReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new ReportFilterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(ReportFilterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        var report = await LoadReportAsync(model, cancellationToken);
        return View("Preview", report);
    }

    [HttpGet]
    public async Task<IActionResult> Export(string type, [FromQuery] ReportFilterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || (type != "csv" && type != "excel"))
        {
            ModelState.AddModelError(string.Empty, "The requested export or its filters are invalid.");
            return View("Index", model);
        }

        var report = await LoadReportAsync(model, cancellationToken);
        if (!string.IsNullOrWhiteSpace(report.ErrorMessage))
        {
            return View("Index", report);
        }

        var fileName = $"{GetFileName(report.ReportType)}-report";
        if (type == "csv")
        {
            var csv = _reportService.BuildCsv(report);
            var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(System.Text.Encoding.UTF8.GetBytes(csv)).ToArray();
            return File(bytes, "text/csv; charset=utf-8", $"{fileName}.csv");
        }

        var workbook = _reportService.GenerateExcel(report);
        return File(workbook.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{fileName}.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> Print([FromQuery] ReportFilterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            ModelState.AddModelError(string.Empty, "The requested report filters are invalid.");
            return View("Index", model);
        }

        var report = await LoadReportAsync(model, cancellationToken);
        return View("Print", report);
    }

    private Task<ReportFilterViewModel> LoadReportAsync(ReportFilterViewModel model, CancellationToken cancellationToken)
    {
        var accessToken = User.FindFirstValue("supabase_access_token");
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var profileId = User.FindFirstValue("profile_id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return _reportService.GenerateReportAsync(model, accessToken, role, profileId, cancellationToken);
    }

    private static string GetFileName(string reportType)
    {
        return string.Concat(reportType.ToLowerInvariant().Select(character =>
            char.IsLetterOrDigit(character) ? character : '-')).Trim('-');
    }
}
