using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnapdragonApi.Models;

[Table("job_room_product")]
public class JobRoomProduct
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("job_room_id")]
    public Guid JobRoomId { get; set; }

    [ForeignKey(nameof(JobRoomId))]
    public JobRoom? JobRoom { get; set; }

    [Column("parent_id")]
    public Guid? ParentId { get; set; }

    [ForeignKey(nameof(ParentId))]
    public JobRoomProduct? Parent { get; set; }

    [Column("package_id")]
    public Guid? PackageId { get; set; }

    [ForeignKey(nameof(PackageId))]
    public Package? Package { get; set; }

    [Column("master_product_id")]
    public Guid? MasterProductId { get; set; }

    [ForeignKey(nameof(MasterProductId))]
    public MasterProduct? MasterProduct { get; set; }

    [Column("quantity", TypeName = "decimal(10,2)")]
    public decimal Quantity { get; set; } = 1;

    [Column("days", TypeName = "decimal(5,2)")]
    public decimal? Days { get; set; }

    [Column("price", TypeName = "decimal(18,2)")]
    public decimal? Price { get; set; }

    [Column("discount_percent", TypeName = "decimal(5,2)")]
    public decimal? DiscountPercent { get; set; }

    [Column("discount_fixed", TypeName = "decimal(18,2)")]
    public decimal? DiscountFixed { get; set; }

    [Column("service_charge_percent", TypeName = "decimal(5,2)")]
    public decimal? ServiceChargePercent { get; set; }

    [Column("service_charge_before_discount")]
    public bool ServiceChargeBeforeDiscount { get; set; } = false;

    [Column("tax_rate", TypeName = "decimal(10,4)")]
    public decimal? TaxRate { get; set; }

    [Column("commission_percent", TypeName = "decimal(5,2)")]
    public decimal? CommissionPercent { get; set; }

    [MaxLength(500)]
    [Column("notes")]
    public string? Notes { get; set; }

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
