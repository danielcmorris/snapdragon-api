namespace SnapdragonApi.DTOs;

public class WarehouseDto
{
    public Guid Id { get; set; }
    public int? Vid { get; set; }
    public Guid CompanyId { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
    public string? Phone { get; set; }
    public int StatusId { get; set; }
    public bool IsActive { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTime CreatedDate { get; set; }
    public Guid? UpdatedById { get; set; }
    public DateTime UpdatedDate { get; set; }
}

public class CreateWarehouseRequest
{
    public int? Vid { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; } = 0;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
    public string? Phone { get; set; }
}

public class UpdateWarehouseRequest
{
    public int? Vid { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public int? SortOrder { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
    public string? Phone { get; set; }
    public int? StatusId { get; set; }
    public bool? IsActive { get; set; }
}

public class WarehouseListResponse
{
    public List<WarehouseDto> Warehouses { get; set; } = new();
    public int TotalCount { get; set; }
}
