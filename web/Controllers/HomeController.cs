using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ServiceHub_IT.Interfaces;
using ServiceHub_IT.Models;
using ServiceHub_IT.Services;

namespace ServiceHub_IT.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly DashboardService _dashboardService;
    private readonly ISupabaseService _supabaseService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        DashboardService dashboardService,
        ISupabaseService supabaseService,
        ILogger<HomeController> logger)
    {
        _dashboardService = dashboardService;
        _supabaseService = supabaseService;
        _logger = logger;
    }

    public IActionResult Index() => View();

    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var accessToken = User.FindFirst("supabase_access_token")?.Value ?? string.Empty;
        var metrics = await _dashboardService.GetMetricsAsync(accessToken, cancellationToken);
        return View(metrics);
    }

    public IActionResult Privacy() => View();

    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        var profileClaim = User.FindFirst("profile_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var accessToken = User.FindFirst("supabase_access_token")?.Value ?? string.Empty;
        Profile? profile = null;

        if (Guid.TryParse(profileClaim, out var profileId))
        {
            profile = await _supabaseService.GetProfileByIdAsync(profileId, accessToken, cancellationToken);
        }

        profile ??= new Profile
        {
            Id = Guid.TryParse(profileClaim, out var fallbackId) ? fallbackId : Guid.Empty,
            FullName = User.FindFirst("full_name")?.Value ?? User.Identity?.Name ?? string.Empty,
            Email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value ?? string.Empty,
            Role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Client"
        };

        return View(profile);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(CancellationToken cancellationToken)
    {
        var profile = await GetCurrentProfileAsync(cancellationToken);
        if (profile is null)
        {
            return NotFound();
        }

        return View(new ProfileEditViewModel
        {
            FullName = profile.FullName,
            Phone = profile.Phone,
            ProfileImage = profile.ProfileImage,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        ProfileEditViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var profile = await GetCurrentProfileAsync(cancellationToken);
        if (profile is null)
        {
            return NotFound();
        }

        profile.FullName = model.FullName.Trim();
        profile.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
        profile.ProfileImage = string.IsNullOrWhiteSpace(model.ProfileImage)
            ? null
            : model.ProfileImage.Trim();
        profile.UpdatedAt = DateTime.UtcNow;

        try
        {
            var updated = await _supabaseService.UpdateProfileAsync(
                profile,
                User.FindFirst("supabase_access_token")?.Value,
                cancellationToken);
            if (!updated)
            {
                ModelState.AddModelError(string.Empty, "Unable to update your profile right now.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Your profile has been updated.";
            return RedirectToAction(nameof(Profile));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to update profile {ProfileId}.", profile.Id);
            ModelState.AddModelError(string.Empty, "Unable to update your profile right now.");
            return View(model);
        }
    }

    private async Task<Profile?> GetCurrentProfileAsync(CancellationToken cancellationToken)
    {
        var profileIdClaim = User.FindFirst("profile_id")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(profileIdClaim, out var profileId) || profileId == Guid.Empty)
        {
            return null;
        }

        var accessToken = User.FindFirst("supabase_access_token")?.Value;
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        return await _supabaseService.GetProfileByIdAsync(profileId, accessToken, cancellationToken);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
