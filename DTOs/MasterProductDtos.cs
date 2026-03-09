namespace SnapdragonApi.DTOs;

public class MasterProductDto
{
    public Guid Id { get; set; }
    public int? Vid { get; set; }
    public Guid CompanyId { get; set; }
    public string? ProductId { get; set; }
    public string? PartNumber { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? FriendlyName { get; set; }
    public string? ProductType { get; set; }
    public decimal? DefaultCost { get; set; }
    public decimal? DefaultPrice { get; set; }
    public string? Status { get; set; }
    public decimal? PriceLevelA { get; set; }
    public decimal? PriceLevelB { get; set; }
    public decimal? PriceLevelC { get; set; }
    public decimal? PriceLevelD { get; set; }
    public decimal? PriceLevelE { get; set; }
    public string? Manufacturer { get; set; }
    public DateTime? DateAdded { get; set; }
    public bool Taxable { get; set; }
    public string? TaxType { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Subcategory { get; set; }
    public string? UnitOfMeasure { get; set; }
    public int StatusId { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTime CreatedDate { get; set; }
    public Guid? UpdatedById { get; set; }
    public DateTime UpdatedDate { get; set; }
}

public class CreateMasterProductRequest
{
    public int? Vid { get; set; }
    public string? ProductId { get; set; }
    public string? PartNumber { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? FriendlyName { get; set; }
    public string? ProductType { get; set; }
    public decimal? DefaultCost { get; set; }
    public decimal? DefaultPrice { get; set; }
    public string? Status { get; set; }
    public decimal? PriceLevelA { get; set; }
    public decimal? PriceLevelB { get; set; }
    public decimal? PriceLevelC { get; set; }
    public decimal? PriceLevelD { get; set; }
    public decimal? PriceLevelE { get; set; }
    public string? Manufacturer { get; set; }
    public DateTime? DateAdded { get; set; }
    public bool Taxable { get; set; } = true;
    public string? TaxType { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Subcategory { get; set; }
    public string? UnitOfMeasure { get; set; }
}

public class UpdateMasterProductRequest
{
    public int? Vid { get; set; }
    public string? ProductId { get; set; }
    public string? PartNumber { get; set; }
    public string? ProductName { get; set; }
    public string? FriendlyName { get; set; }
    public string? ProductType { get; set; }
    public decimal? DefaultCost { get; set; }
    public decimal? DefaultPrice { get; set; }
    public string? Status { get; set; }
    public decimal? PriceLevelA { get; set; }
    public decimal? PriceLevelB { get; set; }
    public decimal? PriceLevelC { get; set; }
    public decimal? PriceLevelD { get; set; }
    public decimal? PriceLevelE { get; set; }
    public string? Manufacturer { get; set; }
    public DateTime? DateAdded { get; set; }
    public bool? Taxable { get; set; }
    public string? TaxType { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Subcategory { get; set; }
    public string? UnitOfMeasure { get; set; }
    public int? StatusId { get; set; }
}

public class MasterProductListResponse
{
    public List<MasterProductDto> MasterProducts { get; set; } = new();
    public int TotalCount { get; set; }
}
