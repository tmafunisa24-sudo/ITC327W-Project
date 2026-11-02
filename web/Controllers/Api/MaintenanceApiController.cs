// Controllers/Api/MaintenanceApiController.cs

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceHub_IT.DTOs.Api;
using ServiceHub_IT.Interfaces;
using ServiceHub_IT.Models;
using ServiceHub_IT.Services;

namespace ServiceHub_IT.Controllers.Api;

[ApiController]
[Route("api/maintenance")]
[Authorize(Roles = "Admin,Technician")]
[Produces("application/json")]
public sealed class MaintenanceApiController : ControllerBase
{
    private readonly ISupabaseService _supabaseService;
    private readonly AssetService _assetService;
    private readonly ILogger<MaintenanceApiController> _logger;

    public MaintenanceApiController(
        ISupabaseService supabaseService,
        AssetService assetService,
        ILogger<MaintenanceApiController> logger)
    {
        _supabaseService = supabaseService;
        _assetService = assetService;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────
    // GET /api/maintenance
    // Admin = all; Technician = only their assigned records.
    // ────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var all = (await _supabaseService.GetAllMaintenanceAsync(cancellationToken)).ToList();

            if (User.IsInRole("Technician") && !User.IsInRole("Admin"))
            {
                var profileId = await ResolveCurrentProfileIdAsync(cancellationToken);
                if (profileId == Guid.Empty)
                {
                    return Ok(Array.Empty<object>());
                }

                all = all.Where(m => m.technician_id == profileId).ToList();
            }

            // Enrich with asset + technician names
            var assets = (await _assetService.GetAssetsAsync(new AssetSearchViewModel(), cancellationToken))
                .Where(a => !string.IsNullOrWhiteSpace(a.id) && Guid.TryParse(a.id, out _))
                .ToDictionary(a => Guid.Parse(a.id!), a => a.asset_name);

            var profiles = (await _supabaseService.GetAllProfilesAsync(cancellationToken))
                .ToDictionary(
                    p => p.Id,
                    p => string.IsNullOrWhiteSpace(p.FullName) ? p.Email : p.FullName);

            var payload = all
                .OrderByDescending(m => m.maintenance_date)
                .Select(m => new
                {
                    id = m.id,
                    assetId = m.asset_id,
                    assetName = assets.GetValueOrDefault(m.asset_id, "Unknown asset"),
                    technicianId = m.technician_id,
                    technicianName = profiles.GetValueOrDefault(m.technician_id, "Unknown technician"),
                    problem = m.problem,
                    solution = m.solution,
                    maintenanceDate = m.maintenance_date,
                    nextServiceDate = m.next_service_date,
                    maintenanceCost = m.maintenance_cost,
                    status = m.status,
                    createdAt = m.created_at,
                })
                .ToList();

            return Ok(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to load maintenance for API.");
            return StatusCode(500, new ApiErrorDto
            {
                Error = "Unable to load maintenance records right now."
            });
        }
    }

    // ────────────────────────────────────────────────────────
    // GET /api/maintenance/{id}
    // ────────────────────────────────────────────────────────
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var record = await _supabaseService.GetMaintenanceByIdAsync(id, cancellationToken);
            if (record is null)
            {
                return NotFound(new ApiErrorDto { Error = "Maintenance record not found." });
            }

            // Technician access check
            if (User.IsInRole("Technician") && !User.IsInRole("Admin"))
            {
                var profileId = await ResolveCurrentProfileIdAsync(cancellationToken);
                if (profileId == Guid.Empty || record.technician_id != profileId)
                {
                    return StatusCode(403, new ApiErrorDto
                    {
                        Error = "You can only view your own maintenance records."
                    });
                }
            }

            var asset = await _assetService.GetAssetAsync(record.asset_id.ToString(), cancellationToken);
            var technician = await _supabaseService.GetProfileByIdAsync(
                record.technician_id,
                User.FindFirst("supabase_access_token")?.Value ?? string.Empty,
                cancellationToken);

            return Ok(new
            {
                id = record.id,
                assetId = record.asset_id,
                assetName = asset?.asset_name ?? "Unknown asset",
                technicianId = record.technician_id,
                technicianName = technician is null
                    ? "Unknown technician"
                    : (string.IsNullOrWhiteSpace(technician.FullName) ? technician.Email : technician.FullName),
                problem = record.problem,
                solution = record.solution,
                maintenanceDate = record.maintenance_date,
                nextServiceDate = record.next_service_date,
                maintenanceCost = record.maintenance_cost,
                status = record.status,
                createdAt = record.created_at,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to load maintenance {Id}.", id);
            return StatusCode(500, new ApiErrorDto
            {
                Error = "Unable to load the maintenance record right now."
            });
        }
    }

    // ────────────────────────────────────────────────────────
    // POST /api/maintenance
    // Admin only.
    // ────────────────────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(
        [FromBody] MaintenanceRequest request,
        CancellationToken cancellationToken)
    {
        var validation = ValidateRequest(request);
        if (validation is not null)
        {
            return BadRequest(new ApiErrorDto { Error = validation });
        }

        try
        {
            var record = new Maintenance
            {
                id = Guid.NewGuid(),
                asset_id = request.AssetId!.Value,
                technician_id = request.TechnicianId!.Value,
                problem = request.Problem!.Trim(),
                solution = string.IsNullOrWhiteSpace(request.Solution) ? null : request.Solution.Trim(),
                maintenance_date = request.MaintenanceDate,
                next_service_date = request.NextServiceDate,
                maintenance_cost = request.MaintenanceCost,
                status = NormalizeStatus(request.Status) ?? "Open",
                created_at = DateTime.UtcNow,
            };

            var created = await _supabaseService.CreateMaintenanceAsync(record, cancellationToken);
            if (!created)
            {
                _logger.LogWarning(
                    "CreateMaintenanceAsync returned false for asset {AssetId}, technician {TecjId}",
                    record.asset_id, record.technician_id
                    );

                return StatusCode(500, new ApiErrorDto
                {
                    Error = "Unable to create the maintenance record. Check the backend logs for details."
                });
            }

            _logger.LogInformation("Maintenance {Id} created for asset {AssetId}.", record.id, record.asset_id);
            return StatusCode(201, new { id = record.id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to create maintenance.");
            return StatusCode(500, new ApiErrorDto
            {
                Error = "Unable to create the maintenance record right now."
            });
        }
    }

    // ────────────────────────────────────────────────────────
    // PATCH /api/maintenance/{id}
    // Admin or the assigned technician.
    // ────────────────────────────────────────────────────────
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] MaintenanceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var record = await _supabaseService.GetMaintenanceByIdAsync(id, cancellationToken);
            if (record is null)
            {
                return NotFound(new ApiErrorDto { Error = "Maintenance record not found." });
            }

            // Role check
            if (User.IsInRole("Technician") && !User.IsInRole("Admin"))
            {
                var profileId = await ResolveCurrentProfileIdAsync(cancellationToken);
                if (profileId == Guid.Empty || record.technician_id != profileId)
                {
                    return StatusCode(403, new ApiErrorDto
                    {
                        Error = "You can only update your own maintenance records."
                    });
                }
            }

            // Apply partial updates
            if (request.AssetId.HasValue) record.asset_id = request.AssetId.Value;
            if (request.TechnicianId.HasValue) record.technician_id = request.TechnicianId.Value;
            if (!string.IsNullOrWhiteSpace(request.Problem)) record.problem = request.Problem.Trim();
            if (request.Solution is not null)
                record.solution = string.IsNullOrWhiteSpace(request.Solution) ? null : request.Solution.Trim();
            if (request.MaintenanceDate.HasValue) record.maintenance_date = request.MaintenanceDate;
            if (request.NextServiceDate.HasValue) record.next_service_date = request.NextServiceDate;
            if (request.MaintenanceCost.HasValue) record.maintenance_cost = request.MaintenanceCost;
            if (!string.IsNullOrWhiteSpace(request.Status))
                record.status = NormalizeStatus(request.Status) ?? record.status;

            var updated = await _supabaseService.UpdateMaintenanceAsync(record, cancellationToken);
            if (!updated)
            {
                return StatusCode(500, new ApiErrorDto
                {
                    Error = "Unable to update the maintenance record."
                });
            }

            return Ok(new { id = record.id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to update maintenance {Id}.", id);
            return StatusCode(500, new ApiErrorDto
            {
                Error = "Unable to update the maintenance record right now."
            });
        }
    }

    // ────────────────────────────────────────────────────────
    // DELETE /api/maintenance/{id}
    // Admin only.
    // ────────────────────────────────────────────────────────
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _supabaseService.DeleteMaintenanceAsync(id, cancellationToken);
            if (!deleted)
            {
                return StatusCode(500, new ApiErrorDto
                {
                    Error = "Unable to delete the maintenance record."
                });
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to delete maintenance {Id}.", id);
            return StatusCode(500, new ApiErrorDto
            {
                Error = "Unable to delete the maintenance record right now."
            });
        }
    }

    // ────────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────────
    private static string? ValidateRequest(MaintenanceRequest request)
    {
        if (request is null) return "Request body is required.";
        if (!request.AssetId.HasValue || request.AssetId == Guid.Empty)
            return "Asset is required.";
        if (!request.TechnicianId.HasValue || request.TechnicianId == Guid.Empty)
            return "Technician is required.";
        if (string.IsNullOrWhiteSpace(request.Problem))
            return "Problem description is required.";
        return null;
    }

    private static string? NormalizeStatus(string? raw)
    {
        return raw?.Trim().ToLowerInvariant() switch
        {
            "pending" or "open" => "Pending",
            "in progress" => "In Progress",
            "completed" or "closed" => "Completed",
            "cancelled" or "canceled" => "Cancelled",
            _ => null,
        };
    }

    private async Task<Guid> ResolveCurrentProfileIdAsync(CancellationToken ct)
    {
        var claim = User.FindFirst("profile_id")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (Guid.TryParse(claim, out var id) && id != Guid.Empty)
        {
            return id;
        }

        var email = User.Identity?.Name ?? User.FindFirst(ClaimTypes.Email)?.Value;
        if (!string.IsNullOrWhiteSpace(email))
        {
            var profile = await _supabaseService.GetProfileByEmailAsync(email, ct);
            if (profile is not null) return profile.Id;
        }

        return Guid.Empty;
    }
}