namespace SnapdragonApi.DTOs;

public class OfficeDto
{
    public Guid Id { get; set; }
    public int? Vid { get; set; }
    public Guid CompanyId { get; set; }
    public string? OfficeNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Fax { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Address2 { get; set; }
    public string? Address3 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
    public int SortOrder { get; set; }
    public int? TaxRegionId { get; set; }
    public Guid? DefaultWarehouseId { get; set; }
    public bool ShowOnContactList { get; set; }
    public string? ServiceChargeLabel { get; set; }
    public decimal DiscountThreshold { get; set; }
    public string? FeatureFlags { get; set; }
    public string? QuickbooksClass { get; set; }
    public int NsLocationId { get; set; }
    public int NsEntityId { get; set; }
    public bool IsHeadquarters { get; set; }
    public int StatusId { get; set; }
    public List<Guid> WarehouseIds { get; set; } = new();
    public Guid? CreatedById { get; set; }
    public DateTime CreatedDate { get; set; }
    public Guid? UpdatedById { get; set; }
    public DateTime UpdatedDate { get; set; }
}

public class CreateOfficeRequest
{
    public int? Vid { get; set; }
    public string? OfficeNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Fax { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Address2 { get; set; }
    public string? Address3 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
    public int SortOrder { get; set; } = 99;
    public int? TaxRegionId { get; set; }
    public Guid? DefaultWarehouseId { get; set; }
    public bool ShowOnContactList { get; set; } = true;
    public string? ServiceChargeLabel { get; set; }
    public decimal DiscountThreshold { get; set; } = 0;
    public string? FeatureFlags { get; set; }
    public string? QuickbooksClass { get; set; }
    public int NsLocationId { get; set; } = 0;
    public int NsEntityId { get; set; } = 0;
    public bool IsHeadquarters { get; set; }
}

public class UpdateOfficeRequest
{
    public int? Vid { get; set; }
    public string? OfficeNumber { get; set; }
    public string? Name { get; set; }
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Fax { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Address2 { get; set; }
    public string? Address3 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
    public int? SortOrder { get; set; }
    public int? TaxRegionId { get; set; }
    public bool? ClearTaxRegion { get; set; }
    public Guid? DefaultWarehouseId { get; set; }
    public bool? ClearDefaultWarehouse { get; set; }
    public bool? ShowOnContactList { get; set; }
    public string? ServiceChargeLabel { get; set; }
    public decimal? DiscountThreshold { get; set; }
    public string? FeatureFlags { get; set; }
    public string? QuickbooksClass { get; set; }
    public int? NsLocationId { get; set; }
    public int? NsEntityId { get; set; }
    public bool? IsHeadquarters { get; set; }
    public int? StatusId { get; set; }
}

public class OfficeListResponse
{
    public List<OfficeDto> Offices { get; set; } = new();
    public int TotalCount { get; set; }
}

public class SetOfficeWarehousesRequest
{
    public List<Guid> WarehouseIds { get; set; } = new();
}
