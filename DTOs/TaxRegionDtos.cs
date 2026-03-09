namespace SnapdragonApi.DTOs;

public class TaxRegionDto
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = "";
    public decimal SalesTax { get; set; }
    public decimal LaborTax { get; set; }
    public int SortOrder { get; set; }
    public int StatusId { get; set; }
}

public class CreateTaxRegionRequest
{
    public string? Code { get; set; }
    public string Name { get; set; } = "";
    public decimal SalesTax { get; set; } = 0;
    public decimal LaborTax { get; set; } = 0;
    public int SortOrder { get; set; } = 99;
}

public class UpdateTaxRegionRequest
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public decimal? SalesTax { get; set; }
    public decimal? LaborTax { get; set; }
    public int? SortOrder { get; set; }
    public int? StatusId { get; set; }
}
