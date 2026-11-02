// Controllers/Api/DepartmentsApiController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceHub_IT.DTOs.Api;
using ServiceHub_IT.Interfaces;
using ServiceHub_IT.Models;
using ServiceHub_IT.Services;

namespace ServiceHub_IT.Controllers.Api;

[ApiController]
[Route("api/departments")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public sealed class DepartmentsApiController : ControllerBase
{
    private readonly ISupabaseService _supabaseService;
    private readonly DepartmentService _departmentService;
    private readonly ILogger<DepartmentsApiController> _logger;

    public DepartmentsApiController(
        ISupabaseService supabaseService,
        DepartmentService departmentService,
        ILogger<DepartmentsApiController> logger)
    {
        _supabaseService = supabaseService;
        _departmentService = departmentService;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────
    // GET /api/departments
    // ────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var departments = (await _departmentService.GetDepartmentsAsync(cancellationToken)).ToList();
            var profiles = (await _supabaseService.GetAllProfilesAsync(cancellationToken)).ToList();

            var payload = departments
                .OrderBy(d => d.department_name)
                .Select(d => new
                {
                    id = d.id,
                    name = d.department_name,  // expose as "name" for the client
                    description = d.description,
                    employeeCount = string.IsNullOrWhiteSpace(d.id)
                        ? 0
                        : profiles.Count(p => p.DepartmentId?.ToString() == d.id),
                    createdAt = d.created_at,
                    updatedAt = d.updated_at,
                })
                .ToList();

            return Ok(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to load departments.");
            return StatusCode(500, new ApiErrorDto
            {
                Error = "Unable to load departments right now."
            });
        }
    }

    // ────────────────────────────────────────────────────────
    // GET /api/departments/{id}
    // ────────────────────────────────────────────────────────
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new ApiErrorDto { Error = "Department id is required." });
        }

        try
        {
            var department = await _departmentService.GetDepartmentAsync(id, cancellationToken);
            if (department is null)
            {
                return NotFound(new ApiErrorDto { Error = "Department not found." });
            }

            var profiles = (await _supabaseService.GetAllProfilesAsync(cancellationToken)).ToList();
            var employeeCount = profiles.Count(p => p.DepartmentId?.ToString() == id);

            return Ok(new
            {
                id = department.id,
                name = department.department_name,
                description = department.description,
                employeeCount,
                createdAt = department.created_at,
                updatedAt = department.updated_at,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to load department {Id}.", id);
            return StatusCode(500, new ApiErrorDto
            {
                Error = "Unable to load the department right now."
            });
        }
    }

    // ────────────────────────────────────────────────────────
    // POST /api/departments
    // ────────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] DepartmentRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new ApiErrorDto { Error = "Department name is required." });
        }

        try
        {
            var department = new Department
            {
                department_name = request.Name.Trim(),
                description = string.IsNullOrWhiteSpace(request.Description)
                    ? null
                    : request.Description.Trim(),
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow,
            };

            var created = await _departmentService.CreateDepartmentAsync(department, cancellationToken);
            if (!created)
            {
                return StatusCode(500, new ApiErrorDto
                {
                    Error = "Unable to create the department."
                });
            }

            _logger.LogInformation("Department '{Name}' created.", department.department_name);
            return StatusCode(201, new { message = "Department created." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to create department.");
            return StatusCode(500, new ApiErrorDto
            {
                Error = "Unable to create the department right now."
            });
        }
    }

    // ────────────────────────────────────────────────────────
    // PATCH /api/departments/{id}
    // ────────────────────────────────────────────────────────
    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] DepartmentRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new ApiErrorDto { Error = "Department id is required." });
        }

        if (request is null || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new ApiErrorDto { Error = "Department name is required." });
        }

        try
        {
            var department = await _departmentService.GetDepartmentAsync(id, cancellationToken);
            if (department is null)
            {
                return NotFound(new ApiErrorDto { Error = "Department not found." });
            }

            department.department_name = request.Name.Trim();
            department.description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim();
            department.updated_at = DateTime.UtcNow;

            var updated = await _departmentService.UpdateDepartmentAsync(department, cancellationToken);
            if (!updated)
            {
                return StatusCode(500, new ApiErrorDto
                {
                    Error = "Unable to update the department."
                });
            }

            return Ok(new { message = "Department updated." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to update department {Id}.", id);
            return StatusCode(500, new ApiErrorDto
            {
                Error = "Unable to update the department right now."
            });
        }
    }

    // ────────────────────────────────────────────────────────
    // DELETE /api/departments/{id}
    // ────────────────────────────────────────────────────────
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new ApiErrorDto { Error = "Department id is required." });
        }

        try
        {
            var deleted = await _departmentService.DeleteDepartmentAsync(id, cancellationToken);
            if (!deleted)
            {
                return StatusCode(500, new ApiErrorDto
                {
                    Error = "Unable to delete the department."
                });
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to delete department {Id}.", id);
            return StatusCode(500, new ApiErrorDto
            {
                Error = "Unable to delete the department right now."
            });
        }
    }
}