using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ServiceHub_IT.Interfaces;

namespace ServiceHub_IT.Filters;

public sealed class StatusAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly ISupabaseService _supabaseService;

    public StatusAuthorizationFilter(ISupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var email = context.HttpContext.User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var profile = await _supabaseService.GetProfileByEmailAsync(email, context.HttpContext.RequestAborted);
        if (profile is null)
        {
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            context.HttpContext.Response.Cookies.Delete("ServiceHubITAuth");
            context.Result = new RedirectToActionResult("Login", "Auth", new
            {
                returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString,
                message = "Profile not found."
            });
            return;
        }

        if (string.Equals(profile.Status, "Disabled", StringComparison.OrdinalIgnoreCase))
        {
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            context.HttpContext.Response.Cookies.Delete("ServiceHubITAuth");
            context.Result = new RedirectToActionResult("Login", "Auth", new
            {
                returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString,
                message = "Your account has been disabled. Please contact an administrator."
            });
        }
    }
}
