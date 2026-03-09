using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnapdragonApi.Models;

[Table("stock")]
public class Stock
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("vid")]
    public int? Vid { get; set; }

    [Required]
    [Column("product_id")]
    public Guid ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }

    [Column("warehouse_id")]
    public Guid? WarehouseId { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; } = 0;

    [Column("reserved_quantity")]
    public int ReservedQuantity { get; set; } = 0;

    [MaxLength(50)]
    [Column("location")]
    public string? Location { get; set; }

    [Column("last_counted_at")]
    public DateTime? LastCountedAt { get; set; }

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
