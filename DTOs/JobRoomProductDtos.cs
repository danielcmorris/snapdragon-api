namespace SnapdragonApi.DTOs;

public class JobRoomProductDto
{
    public Guid Id { get; set; }
    public Guid JobRoomId { get; set; }
    public Guid? ParentId { get; set; }
    public Guid? PackageId { get; set; }
    public string? PackageName { get; set; }
    public Guid? MasterProductId { get; set; }
    public string? ProductName { get; set; }
    public decimal Quantity { get; set; }
    public decimal? Days { get; set; }
    public decimal? Price { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? DiscountFixed { get; set; }
    public decimal? ServiceChargePercent { get; set; }
    public bool ServiceChargeBeforeDiscount { get; set; }
    public string? Notes { get; set; }
    public string? PartNumber { get; set; }
    public decimal? TaxRate { get; set; }
    public decimal? CommissionPercent { get; set; }
    public int SortOrder { get; set; }
    public int StatusId { get; set; }
    public List<JobRoomProductDto> SubItems { get; set; } = new();
    public Guid? CreatedById { get; set; }
    public DateTime CreatedDate { get; set; }
    public Guid? UpdatedById { get; set; }
    public DateTime UpdatedDate { get; set; }
}

public class CreateJobRoomProductRequest
{
    public Guid? PackageId { get; set; }
    public Guid? MasterProductId { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal? Days { get; set; }
    public decimal? Price { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? DiscountFixed { get; set; }
    public decimal? ServiceChargePercent { get; set; }
    public bool ServiceChargeBeforeDiscount { get; set; } = false;
    public string? Notes { get; set; }
    public int SortOrder { get; set; } = 0;
}

public class UpdateJobRoomProductRequest
{
    public decimal? Quantity { get; set; }
    public decimal? Days { get; set; }
    public decimal? Price { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? DiscountFixed { get; set; }
    public decimal? ServiceChargePercent { get; set; }
    public bool? ServiceChargeBeforeDiscount { get; set; }
    public string? Notes { get; set; }
    public decimal? TaxRate { get; set; }
    public decimal? CommissionPercent { get; set; }
    public int? SortOrder { get; set; }
    public int? StatusId { get; set; }
}

public class JobRoomProductListResponse
{
    public List<JobRoomProductDto> Products { get; set; } = new();
    public int TotalCount { get; set; }
}

public class ReorderJobRoomProductsRequest
{
    /// <summary>Ordered list of product IDs — new sort order is derived from list position.</summary>
    public List<Guid> ProductIds { get; set; } = new();
}

public class MoveJobRoomProductRequest
{
    public Guid TargetRoomId { get; set; }
    /// <summary>0-based index in the target room after the move.</summary>
    public int TargetIndex { get; set; }
}

public class JobFinanceProductDto : JobRoomProductDto
{
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = "";
}

public class JobFinanceResponse
{
    public List<JobFinanceProductDto> Products { get; set; } = new();
}

public class BulkFinanceItem
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public decimal? Price { get; set; }
    public decimal? TaxRate { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? ServiceChargePercent { get; set; }
    public decimal? CommissionPercent { get; set; }
}

public class BulkUpdateFinanceRequest
{
    public List<BulkFinanceItem> Items { get; set; } = new();
}
