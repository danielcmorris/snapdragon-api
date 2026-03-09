namespace SnapdragonApi.DTOs;

public class OfficeProductDto
{
    public Guid Id { get; set; }
    public Guid OfficeId { get; set; }
    public Guid MasterProductId { get; set; }
    public string? ProductName { get; set; }
    public string? FriendlyName { get; set; }
    public string? PartNumber { get; set; }
    public string? Category { get; set; }
    public decimal? DefaultPrice { get; set; }
    public int SortOrder { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class AddOfficeProductRequest
{
    public Guid MasterProductId { get; set; }
    public int SortOrder { get; set; } = 0;
}

public class OfficeProductListResponse
{
    public List<OfficeProductDto> Products { get; set; } = new();
    public int TotalCount { get; set; }
}
