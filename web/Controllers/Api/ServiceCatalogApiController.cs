// Controllers/Api/ServiceCatalogApiController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceHub_IT.DTOs.Api;
using ServiceHub_IT.Interfaces;

namespace ServiceHub_IT.Controllers.Api;

[ApiController]
[Route("api/service-catalog")]
[Authorize]
[Produces("application/json")]
public sealed class ServiceCatalogApiController : ControllerBase
{
    private readonly ISupabaseService _supabaseService;
    private readonly ILogger<ServiceCatalogApiController> _logger;

    public ServiceCatalogApiController(
        ISupabaseService supabaseService,
        ILogger<ServiceCatalogApiController> logger)
    {
        _supabaseService = supabaseService;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────
    // GET /api/service-catalog
    // Returns only active catalog items.
    // Requires auth (cookie).
    // ────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        try
        {
            // Don't pass the user's Supabase token — it expires.
            // The anon key is sufficient for this read-only endpoint,
            // and the user is already authenticated via the ASP.NET cookie.
            var items = await _supabaseService.GetActiveServiceCatalogAsync(
            null, cancellationToken);

            return Ok(items);
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(401, new ApiErrorDto
            {
                Error = "Your session has expired. Please log in again."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to load service catalog for API.");
            return StatusCode(500, new ApiErrorDto
            {
                Error = "Unable to load the service catalog right now."
            });
        }
    }
}