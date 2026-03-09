namespace SnapdragonApi.DTOs;

public class ProductDto
{
    public Guid Id { get; set; }
    public int? Vid { get; set; }
    public Guid MasterProductId { get; set; }
    public string? SerialNumber { get; set; }
    public string? Barcode { get; set; }
    public string? Condition { get; set; }
    public string? Notes { get; set; }
    public bool IsAvailable { get; set; }
    public int StatusId { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTime CreatedDate { get; set; }
    public Guid? UpdatedById { get; set; }
    public DateTime UpdatedDate { get; set; }
}

public class CreateProductRequest
{
    public int? Vid { get; set; }
    public Guid MasterProductId { get; set; }
    public string? SerialNumber { get; set; }
    public string? Barcode { get; set; }
    public string? Condition { get; set; }
    public string? Notes { get; set; }
}

public class UpdateProductRequest
{
    public int? Vid { get; set; }
    public string? SerialNumber { get; set; }
    public string? Barcode { get; set; }
    public string? Condition { get; set; }
    public string? Notes { get; set; }
    public bool? IsAvailable { get; set; }
    public int? StatusId { get; set; }
}

public class ProductListResponse
{
    public List<ProductDto> Products { get; set; } = new();
    public int TotalCount { get; set; }
}
