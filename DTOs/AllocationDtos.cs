namespace SnapdragonApi.DTOs;

public class AllocationDto
{
    public Guid Id { get; set; }
    public int? Vid { get; set; }
    public Guid StockId { get; set; }
    public Guid? WarehouseId { get; set; }
    public int Quantity { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int StatusId { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTime CreatedDate { get; set; }
    public Guid? UpdatedById { get; set; }
    public DateTime UpdatedDate { get; set; }
}

public class CreateAllocationRequest
{
    public int? Vid { get; set; }
    public Guid StockId { get; set; }
    public Guid? WarehouseId { get; set; }
    public int Quantity { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class UpdateAllocationRequest
{
    public int? Vid { get; set; }
    public Guid? WarehouseId { get; set; }
    public int? Quantity { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class AllocationListResponse
{
    public List<AllocationDto> Allocations { get; set; } = new();
    public int TotalCount { get; set; }
}
