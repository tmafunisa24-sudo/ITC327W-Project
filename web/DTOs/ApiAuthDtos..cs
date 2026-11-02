public sealed class ApiRegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

public sealed class ApiRegisterResponse
{
    public bool Success { get; set; }
    public bool RequiresConfirmation { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Message { get; set; }
}

public sealed class ApiForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

public sealed class ApiForgotPasswordResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}