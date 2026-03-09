using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnapdragonApi.Models;

[Table("product")]
public class Product
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("vid")]
    public int? Vid { get; set; }

    [Required]
    [Column("master_product_id")]
    public Guid MasterProductId { get; set; }

    [ForeignKey(nameof(MasterProductId))]
    public MasterProduct? MasterProduct { get; set; }

    [MaxLength(100)]
    [Column("serial_number")]
    public string? SerialNumber { get; set; }

    [MaxLength(100)]
    [Column("barcode")]
    public string? Barcode { get; set; }

    [MaxLength(50)]
    [Column("condition")]
    public string? Condition { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("is_available")]
    public bool IsAvailable { get; set; } = true;

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
