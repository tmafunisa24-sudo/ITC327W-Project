using System.ComponentModel.DataAnnotations;

namespace ServiceHub_IT.Models;

public sealed class ProfileEditViewModel
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Enter a valid phone number.")]
    [StringLength(40)]
    public string? Phone { get; set; }

    [StringLength(2048)]
    public string? ProfileImage { get; set; }
}
