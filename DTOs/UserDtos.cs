namespace SnapdragonApi.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public int? Vid { get; set; }
    public Guid OfficeId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? JobTitle { get; set; }
    public int UserLevel { get; set; }
    public Guid? DefaultWarehouseId { get; set; }
    public int StatusId { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTime CreatedDate { get; set; }
    public Guid? UpdatedById { get; set; }
    public DateTime UpdatedDate { get; set; }
}

public class CreateUserRequest
{
    public int? Vid { get; set; }
    public Guid OfficeId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? JobTitle { get; set; }
    public int UserLevel { get; set; } = 1;
}

public class UpdateUserRequest
{
    public int? Vid { get; set; }
    public Guid? OfficeId { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? JobTitle { get; set; }
    public int? UserLevel { get; set; }
    public int? StatusId { get; set; }
}

public class UserListResponse
{
    public List<UserDto> Users { get; set; } = new();
    public int TotalCount { get; set; }
}
