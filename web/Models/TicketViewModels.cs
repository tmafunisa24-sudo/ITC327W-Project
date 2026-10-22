using System.ComponentModel.DataAnnotations;

namespace ServiceHub_IT.Models;

public class CreateTicketViewModel
{
    [Required(ErrorMessage = "Device type is required.")]
    public string DeviceType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Problem category is required.")]
    public string ProblemCategory { get; set; } = string.Empty;

    [Required(ErrorMessage = "Repair problem is required.")]
    public string ProblemType { get; set; } = string.Empty;

    [Range(1, long.MaxValue, ErrorMessage = "A repair problem is required.")]
    public long? ServiceCatalogId { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Priority is required.")]
    public string Priority { get; set; } = "Medium";

    [Required(ErrorMessage = "Category is required.")]
    public string Category { get; set; } = string.Empty;

    public IFormFile? Screenshot { get; set; }

    public List<IFormFile> Images { get; set; } = new();
}

public class UpdateTicketStatusViewModel
{
    public string Status { get; set; } = "Open";
    public string? Comment { get; set; }
    public DateTime? DueDate { get; set; }
}

public class PaymentMethodViewModel
{
    public string? PaymentMethod { get; set; }
}
