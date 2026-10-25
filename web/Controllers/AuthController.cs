using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceHub_IT.Interfaces;
using ServiceHub_IT.Models;
using ServiceHub_IT.Utilities;

namespace ServiceHub_IT.Controllers;

/// <summary>
/// Handles authentication and account management: sign in, sign up, password reset and sign out.
/// Credentials are verified by Supabase; on success this controller creates a local
/// ASP.NET Core cookie session that carries the user's id, email and role.
/// </summary>
/// <remarks>
/// [AllowAnonymous] is needed because visitors are not signed in yet when they reach these pages.
/// </remarks>
[AllowAnonymous]
public class AuthController : Controller
{
    // Wrapper around the Supabase API (sign in, sign up, profile and role lookups).
    private readonly ISupabaseService _supabaseService;
    // Records failed authentication attempts so they can be investigated.
    private readonly ILogger<AuthController> _logger;

    /// <summary>Both dependencies are supplied by ASP.NET Core dependency injection.</summary>
    public AuthController(ISupabaseService supabaseService, ILogger<AuthController> logger)
    {
        _supabaseService = supabaseService;
        _logger = logger;
    }

    /// <summary>
    /// Shows the sign in page. Signed-in users are redirected away, and an optional
    /// <paramref name="message"/> (e.g. from a redirect) is shown as an error banner.
    /// </summary>
    [HttpGet]
    public IActionResult Login(string? returnUrl = null, string? message = null)
    {
        // Already signed in, so there is no need to show the login page again.
        if (User?.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Dashboard", "Home");
        }

        // Show any message passed in the URL as an error banner on the login page.
        if (!string.IsNullOrWhiteSpace(message))
        {
            TempData["ErrorMessage"] = message;
        }

        // Keep returnUrl so the user can be sent back to the page they originally asked for.
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    /// <summary>
    /// Processes the sign in form. Flow: validate form, check Supabase credentials, load the profile,
    /// block disabled or pending accounts, normalise the role, create the cookie session, then redirect.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        // Validation rules live in LoginViewModel; redisplay the form with its error messages.
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Guard: Supabase settings must be present in configuration or login cannot work.
        if (!_supabaseService.IsConfigured)
        {
            ModelState.AddModelError(string.Empty, "Authentication is not configured.");
            return View(model);
        }

        try
        {
            // Step 1: verify the email and password with Supabase.
            var signInResult = await _supabaseService.SignInAsync(model.Email, model.Password, HttpContext.RequestAborted);
            if (!signInResult.IsSuccess || string.IsNullOrWhiteSpace(signInResult.AccessToken))
            {
                ModelState.AddModelError(string.Empty, signInResult.ErrorMessage ?? "Invalid email or password.");
                return View(model);
            }

            // Step 2: load the user's profile record from the database.
            var profile = await _supabaseService.GetProfileByEmailAsync(model.Email, HttpContext.RequestAborted);
            if (profile is null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                Response.Cookies.Delete("ServiceHubITAuth");
                TempData["ErrorMessage"] = "Profile not found.";
                return View(model);
            }

            // Step 3: refuse accounts an administrator has disabled, and clear any leftover cookies.
            if (string.Equals(profile.Status, "Disabled", StringComparison.OrdinalIgnoreCase))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                Response.Cookies.Delete("ServiceHubITAuth");
                TempData["ErrorMessage"] = "Your account has been disabled. Please contact an administrator.";
                return View(model);
            }

            // Step 4: fetch the user's role and normalise its casing to Admin, Technician, Client or Pending.
            var rawRole = await _supabaseService.GetRoleForEmailAsync(model.Email, signInResult.AccessToken, HttpContext.RequestAborted);
            var normalizedRole = rawRole?.Trim() ?? string.Empty;
            normalizedRole = normalizedRole.Equals("admin", StringComparison.OrdinalIgnoreCase)
                ? "Admin"
                : normalizedRole.Equals("technician", StringComparison.OrdinalIgnoreCase)
                    ? "Technician"
                    : normalizedRole.Equals("client", StringComparison.OrdinalIgnoreCase)
                        ? "Client"
                    : normalizedRole.Equals("pending", StringComparison.OrdinalIgnoreCase)
                        ? "Pending"
                        : string.Empty;

            // New accounts stay 'Pending' until an administrator approves them, so they cannot sign in yet.
            if (string.Equals(normalizedRole, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "This account is awaiting administrator approval.";
                return View(model);
            }

            // The role was missing or not one we support, so refuse the sign in.
            if (string.IsNullOrWhiteSpace(normalizedRole))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                Response.Cookies.Delete("ServiceHubITAuth");
                ModelState.AddModelError(string.Empty, "Your account has an unsupported role. Please contact an administrator.");
                return View(model);
            }

            // Step 5: build the identity stored in the cookie. The role claim is what the app uses
            // for role-based access (e.g. [Authorize(Roles = "Admin")]).
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, profile.Id.ToString()),
                new("profile_id", profile.Id.ToString()),
                new(ClaimTypes.Name, model.Email),
                new(ClaimTypes.Email, model.Email),
                new(ClaimTypes.Role, normalizedRole),
                new("supabase_access_token", signInResult.AccessToken)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            // Create the cookie session. 'Remember me' makes the cookie persistent and expires it after
            // 30 days; otherwise the session expires after 7 days.
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddDays(7)
            });

            // Step 6: redirect. Only local URLs are accepted, which prevents open-redirect attacks.
            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl);
            }

            // No return URL, so send the user to the dashboard that matches their role.
            var dashboardController = RoleDashboardRoute.GetControllerName(normalizedRole);
            return RedirectToAction("Index", dashboardController);
        }
        // Unexpected failures (e.g. Supabase unreachable) are logged, and the user only sees a generic
        // message so no internal details are exposed.
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Login failed.");
            ModelState.AddModelError(string.Empty, "Unable to sign in right now. Please try again later.");
            return View(model);
        }
    }

    /// <summary>Shows the account registration page (signed-in users are redirected away).</summary>
    [HttpGet]
    public IActionResult SignUp()
    {
        // Signed-in users do not need to register again.
        if (User?.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Dashboard", "Home");
        }

        return View();
    }

    /// <summary>
    /// Processes the registration form by creating the account in Supabase. New accounts must be
    /// approved by an administrator before they can sign in (see the 'Pending' check in Login).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SignUp(SignUpViewModel model)
    {
        // Check the form rules in SignUpViewModel (valid email, 8+ character password, matching confirmation).
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Guard: registration cannot work without Supabase configuration.
        if (!_supabaseService.IsConfigured)
        {
            ModelState.AddModelError(string.Empty, "Authentication is not configured.");
            return View(model);
        }

        try
        {
            // Ask Supabase to create the account.
            var signUpResult = await _supabaseService.SignUpAsync(model.Email, model.Password, HttpContext.RequestAborted);
            // Success: send the user to the login page with a confirmation message.
            if (signUpResult.IsSuccess)
            {
                TempData["SuccessMessage"] = "Account created successfully. Please sign in to continue.";
                return RedirectToAction(nameof(Login));
            }

            ModelState.AddModelError(string.Empty, signUpResult.ErrorMessage ?? "Unable to create your account. Please try again.");
            return View(model);
        }
        // Log the real error, but show the user a generic message.
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Signup failed.");
            ModelState.AddModelError(string.Empty, "Unable to create your account right now. Please try again later.");
            return View(model);
        }
    }

    /// <summary>Shows the reset password form. The token comes from the link in the reset email.</summary>
    [HttpGet]
    public IActionResult ResetPassword(string token) => View(new ResetPasswordViewModel { Token = token ?? string.Empty });

    /// <summary>Sets the user's new password using the one-time reset token.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            // Supabase checks that the token is valid and not expired, then updates the password.
            var success = await _supabaseService.ResetPasswordAsync(model.Token, model.Password, HttpContext.RequestAborted);
            if (success)
            {
                TempData["SuccessMessage"] = "Your password has been reset successfully.";
                return RedirectToAction(nameof(Login));
            }

            TempData["ErrorMessage"] = "The reset link is invalid or has expired.";
            return View(model);
        }
        // Log the real error, but show the user a generic message.
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Reset password failed.");
            TempData["ErrorMessage"] = "Unable to reset your password right now.";
            return View(model);
        }
    }

    /// <summary>Signs the user out. POST + anti-forgery token stop other sites from logging users out.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        // Remove the local cookie session.
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        // Also delete the auth cookie and the Supabase token cookies so nothing is left in the browser.
        Response.Cookies.Delete("ServiceHubITAuth");
        Response.Cookies.Delete("sb-access-token");
        Response.Cookies.Delete("sb-refresh-token");
        return RedirectToAction(nameof(Login));
    }

}