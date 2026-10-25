using System.ComponentModel.DataAnnotations;

namespace ServiceHub_IT.Models;

/// <summary>Data entered on the sign in form. Validation attributes provide the error messages shown to the user.</summary>
public class LoginViewModel
{
    // The email address is the user's login name.
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    // Password as typed; it is only passed on to Supabase for verification.
    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    // Ticked = stay signed in for 30 days instead of 7.
    public bool RememberMe { get; set; }
    // Page the user originally tried to open; used to send them back after signing in.
    public string? ReturnUrl { get; set; }
}

/// <summary>Data entered on the registration form.</summary>
public class SignUpViewModel
{
    // Must be a valid email address; it becomes the account's login name.
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    // Password rule: between 8 and 100 characters.
    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
    public string Password { get; set; } = string.Empty;

    // Must match Password exactly (enforced by [Compare]).
    [Required(ErrorMessage = "Please confirm your password.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>Data entered on the forgot password form (the email that should receive a reset link).</summary>
public class ForgotPasswordViewModel
{
    // Email address of the account to recover.
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>Data posted when a user chooses a new password.</summary>
public class ResetPasswordViewModel
{
    // One-time token from the reset email link; carried in a hidden form field.
    [Required(ErrorMessage = "A reset token is required.")]
    public string Token { get; set; } = string.Empty;

    // The new password: between 8 and 100 characters.
    [Required(ErrorMessage = "New password is required.")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
    public string Password { get; set; } = string.Empty;

    // Must match the new password exactly (enforced by [Compare]).
    [Required(ErrorMessage = "Please confirm your password.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}