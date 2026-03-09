namespace SnapdragonApi.DTOs;

public class PackageDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public int SortOrder { get; set; }
    public int StatusId { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTime CreatedDate { get; set; }
    public Guid? UpdatedById { get; set; }
    public DateTime UpdatedDate { get; set; }
}

public class PackageProductDto
{
    public Guid Id { get; set; }
    public Guid PackageId { get; set; }
    public Guid MasterProductId { get; set; }
    public string? ProductName { get; set; }
    public string? PartNumber { get; set; }
    public decimal Quantity { get; set; }
    public int SortOrder { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CreatePackageRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public int SortOrder { get; set; } = 0;
}

public class UpdatePackageRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public int? SortOrder { get; set; }
    public int? StatusId { get; set; }
}

public class AddPackageProductRequest
{
    public Guid MasterProductId { get; set; }
    public decimal Quantity { get; set; } = 1;
    public int SortOrder { get; set; } = 0;
}

public class PackageListResponse
{
    public List<PackageDto> Packages { get; set; } = new();
    public int TotalCount { get; set; }
}
