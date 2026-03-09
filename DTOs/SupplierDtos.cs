namespace SnapdragonApi.DTOs;

public class SupplierDto
{
    public Guid Id { get; set; }
    public int? Vid { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public string? Category { get; set; }
    public int StatusId { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTime CreatedDate { get; set; }
    public Guid? UpdatedById { get; set; }
    public DateTime UpdatedDate { get; set; }
}

public class CreateSupplierRequest
{
    public int? Vid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public string? Category { get; set; }
}

public class UpdateSupplierRequest
{
    public int? Vid { get; set; }
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public string? Category { get; set; }
    public int? StatusId { get; set; }
}

public class SupplierListResponse
{
    public List<SupplierDto> Suppliers { get; set; } = new();
    public int TotalCount { get; set; }
}
