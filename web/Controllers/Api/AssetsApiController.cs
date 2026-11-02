// Controllers/Api/AssetsApiController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceHub_IT.DTOs.Api;
using ServiceHub_IT.Models;
using ServiceHub_IT.Services;

namespace ServiceHub_IT.Controllers.Api;

[ApiController]
[Route("api/assets")]
[Authorize]                              // ← uses the cookie scheme
[Produces("application/json")]
public sealed class AssetsApiController : ControllerBase
{
    private readonly AssetService _assetService;
    private readonly ILogger<AssetsApiController> _logger;

    public AssetsApiController(
        AssetService assetService,
        ILogger<AssetsApiController> logger)
    {
        _assetService = assetService;
        _logger = logger;
    }

    // GET /api/assets?searchTerm=&category=&status=
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? searchTerm,
        [FromQuery] string? category,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var search = new AssetSearchViewModel
        {
            SearchTerm = searchTerm,
            Category = category,
            Status = status,
        };

        var assets = await _assetService.GetAssetsAsync(search, cancellationToken);
        return Ok(assets);
    }

    // GET /api/assets/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(
        string id,
        CancellationToken cancellationToken)
    {
        var asset = await _assetService.GetAssetAsync(id, cancellationToken);
        if (asset is null)
        {
            return NotFound(new ApiErrorDto { Error = "Asset not found." });
        }
        return Ok(asset);
    }

    // POST /api/assets   (multipart/form-data for file uploads)
    [HttpPost]
    [RequestSizeLimit(50 * 1024 * 1024)]   // 50 MB
    public async Task<IActionResult> Create(
        [FromForm] AssetFormViewModel form,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiErrorDto
            {
                Error = "Invalid form data.",
                Detail = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage))
            });
        }

        var accessToken = User.FindFirst("supabase_access_token")?.Value;

        var asset = new Asset
        {
            asset_tag = form.AssetTag,
            asset_name = form.AssetName,
            brand = form.Brand,
            model = form.Model,
            serial_number = form.SerialNumber,
            category = form.Category,
            purchase_date = form.PurchaseDate,
            warranty_expiry = form.WarrantyExpiry,
            department = form.Department,
            assigned_employee = form.AssignedEmployee,
            location = form.Location,
            status = form.Status,
        };

        var created = await _assetService.CreateAssetAsync(asset, cancellationToken);
        if (!created)
        {
            return StatusCode(500, new ApiErrorDto { Error = "Unable to create asset." });
        }

        // Optional image upload
        if (form.ImageFile is not null && form.ImageFile.Length > 0 && !string.IsNullOrEmpty(asset.id))
        {
            var url = await _assetService.UploadAssetImageAsync(
                form.ImageFile, asset.id, accessToken, cancellationToken);
            await _assetService.UpdateAssetImageAsync(asset.id, url, cancellationToken);
            asset.image_url = url;
        }

        return CreatedAtAction(nameof(GetById), new { id = asset.id }, asset);
    }

    // PUT /api/assets/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        string id,
        [FromForm] AssetFormViewModel form,
        CancellationToken cancellationToken)
    {
        var existing = await _assetService.GetAssetAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound(new ApiErrorDto { Error = "Asset not found." });
        }

        existing.asset_tag = form.AssetTag;
        existing.asset_name = form.AssetName;
        existing.brand = form.Brand;
        existing.model = form.Model;
        existing.serial_number = form.SerialNumber;
        existing.category = form.Category;
        existing.purchase_date = form.PurchaseDate;
        existing.warranty_expiry = form.WarrantyExpiry;
        existing.department = form.Department;
        existing.assigned_employee = form.AssignedEmployee;
        existing.location = form.Location;
        existing.status = form.Status;

        var updated = await _assetService.UpdateAssetAsync(existing, cancellationToken);
        if (!updated)
        {
            return StatusCode(500, new ApiErrorDto { Error = "Unable to update asset." });
        }

        return Ok(existing);
    }

    // DELETE /api/assets/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(
        string id,
        CancellationToken cancellationToken)
    {
        var deleted = await _assetService.DeleteAssetAsync(id, cancellationToken);
        if (!deleted)
        {
            return StatusCode(500, new ApiErrorDto { Error = "Unable to delete asset." });
        }
        return NoContent();
    }
}