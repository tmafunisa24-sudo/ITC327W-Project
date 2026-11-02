// Models/DTOs/ApiUserDtos.cs

namespace ServiceHub_IT.DTOs.Api;

public sealed class UpdateUserRequest
{
    public string? Role { get; set; }
    public string? Status { get; set; }
}