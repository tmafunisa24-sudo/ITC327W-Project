// Models/DTOs/ApiAuthDtos.cs

namespace ServiceHub_IT.DTOs.Api;

public sealed class ApiLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}

public sealed class UpdateProfileRequest
{
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Position { get; set; }
}

public sealed class ApiAuthResponse
{
    public ApiUserDto User { get; set; } = new();
}

public sealed class ApiUserDto
{
    public string Id { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? DepartmentId { get; set; }
    public string? Position { get; set; }
    public string? ProfileImage { get; set; }
    public bool EmailVerified { get; set; }

    public static ApiUserDto FromProfile(Models.Profile p, string role) => new()
    {
        Id = p.Id.ToString(),
        UserId = p.UserId?.ToString(),
        Email = p.Email,
        FullName = p.FullName,
        Role = role,
        Status = p.Status,
        Phone = p.Phone,
        DepartmentId = p.DepartmentId?.ToString(),
        Position = p.Position,
        ProfileImage = p.ProfileImage,
        EmailVerified = p.EmailVerified,
    };
}

public sealed class ApiErrorDto
{
    public string Error { get; set; } = string.Empty;
    public string? Detail { get; set; }
}