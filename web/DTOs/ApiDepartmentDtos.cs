// Models/DTOs/ApiDepartmentDtos.cs

namespace ServiceHub_IT.DTOs.Api;

public sealed class DepartmentRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}