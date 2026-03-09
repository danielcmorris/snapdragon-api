namespace SnapdragonApi.DTOs;

public class StockDto
{
    public Guid Id { get; set; }
    public int? Vid { get; set; }
    public Guid ProductId { get; set; }
    public Guid? WarehouseId { get; set; }
    public int Quantity { get; set; }
    public int ReservedQuantity { get; set; }
    public string? Location { get; set; }
    public DateTime? LastCountedAt { get; set; }
    public int StatusId { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTime CreatedDate { get; set; }
    public Guid? UpdatedById { get; set; }
    public DateTime UpdatedDate { get; set; }
}

public class CreateStockRequest
{
    public int? Vid { get; set; }
    public Guid ProductId { get; set; }
    public Guid? WarehouseId { get; set; }
    public int Quantity { get; set; }
    public int ReservedQuantity { get; set; }
    public string? Location { get; set; }
}

public class UpdateStockRequest
{
    public int? Vid { get; set; }
    public Guid? WarehouseId { get; set; }
    public int? Quantity { get; set; }
    public int? ReservedQuantity { get; set; }
    public string? Location { get; set; }
    public DateTime? LastCountedAt { get; set; }
}

public class StockListResponse
{
    public List<StockDto> Stocks { get; set; } = new();
    public int TotalCount { get; set; }
}
