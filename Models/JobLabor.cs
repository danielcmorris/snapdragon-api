using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnapdragonApi.Models;

[Table("job_labor")]
public class JobLabor
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("job_id")]
    public Guid JobId { get; set; }

    [ForeignKey(nameof(JobId))]
    public Job? Job { get; set; }

    [Column("job_room_id")]
    public Guid? JobRoomId { get; set; }

    [ForeignKey(nameof(JobRoomId))]
    public JobRoom? JobRoom { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; } = 1;

    [MaxLength(200)]
    [Column("employee")]
    public string? Employee { get; set; }

    [MaxLength(200)]
    [Column("task")]
    public string? Task { get; set; }

    [Column("hours", TypeName = "decimal(10,2)")]
    public decimal Hours { get; set; } = 0;

    [Column("cost", TypeName = "decimal(18,2)")]
    public decimal Cost { get; set; } = 0;

    [Column("rate", TypeName = "decimal(18,2)")]
    public decimal Rate { get; set; } = 0;

    [Column("commission_percent", TypeName = "decimal(5,2)")]
    public decimal CommissionPercent { get; set; } = 0;

    [Column("tax_rate", TypeName = "decimal(10,4)")]
    public decimal TaxRate { get; set; } = 0;

    [Column("subcontracted")]
    public bool Subcontracted { get; set; } = false;

    [Column("service_charge_override", TypeName = "decimal(18,2)")]
    public decimal ServiceChargeOverride { get; set; } = 0;

    [Column("sort_order")]
    public int SortOrder { get; set; } = 0;

    [Column("status_id")]
    public int StatusId { get; set; } = 1;

    [Column("created_by_id")]
    public Guid? CreatedById { get; set; }

    [Column("created_date")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Column("updated_by_id")]
    public Guid? UpdatedById { get; set; }

    [Column("updated_date")]
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
}
