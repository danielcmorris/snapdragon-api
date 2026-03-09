namespace SnapdragonApi.DTOs;

public class JobLaborDto
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Guid? JobRoomId { get; set; }
    public string? RoomName { get; set; }
    public int Quantity { get; set; }
    public string? Employee { get; set; }
    public string? Task { get; set; }
    public decimal Hours { get; set; }
    public decimal Cost { get; set; }
    public decimal Rate { get; set; }
    public decimal CommissionPercent { get; set; }
    public decimal TaxRate { get; set; }
    public bool Subcontracted { get; set; }
    public decimal ServiceChargeOverride { get; set; }
    public int SortOrder { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTime CreatedDate { get; set; }
    public Guid? UpdatedById { get; set; }
    public DateTime UpdatedDate { get; set; }
}

public class CreateJobLaborRequest
{
    public Guid? JobRoomId { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Employee { get; set; }
    public string? Task { get; set; }
    public decimal Hours { get; set; } = 0;
    public decimal Cost { get; set; } = 0;
    public decimal Rate { get; set; } = 0;
    public decimal CommissionPercent { get; set; } = 0;
    public decimal TaxRate { get; set; } = 0;
    public bool Subcontracted { get; set; } = false;
    public decimal ServiceChargeOverride { get; set; } = 0;
}

public class UpdateJobLaborRequest
{
    public Guid? JobRoomId { get; set; }
    public int? Quantity { get; set; }
    public string? Employee { get; set; }
    public string? Task { get; set; }
    public decimal? Hours { get; set; }
    public decimal? Cost { get; set; }
    public decimal? Rate { get; set; }
    public decimal? CommissionPercent { get; set; }
    public decimal? TaxRate { get; set; }
    public bool? Subcontracted { get; set; }
    public decimal? ServiceChargeOverride { get; set; }
}

public class JobLaborListResponse
{
    public List<JobLaborDto> Items { get; set; } = new();
}
