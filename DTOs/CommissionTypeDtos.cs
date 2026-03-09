namespace SnapdragonApi.DTOs;

public class CommissionTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal DefaultPercent { get; set; }
    public int SortOrder { get; set; }
    public int StatusId { get; set; }
}

public class CreateCommissionTypeRequest
{
    public string Name { get; set; } = "";
    public decimal DefaultPercent { get; set; } = 0;
    public int SortOrder { get; set; } = 99;
}

public class UpdateCommissionTypeRequest
{
    public string? Name { get; set; }
    public decimal? DefaultPercent { get; set; }
    public int? SortOrder { get; set; }
    public int? StatusId { get; set; }
}
