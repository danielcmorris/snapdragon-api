using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnapdragonApi.Models;

[Table("allocation")]
public class Allocation
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("vid")]
    public int? Vid { get; set; }

    [Required]
    [Column("stock_id")]
    public Guid StockId { get; set; }

    [ForeignKey(nameof(StockId))]
    public Stock? Stock { get; set; }

    [Column("warehouse_id")]
    public Guid? WarehouseId { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("start_date")]
    public DateTime StartDate { get; set; }

    [Column("end_date")]
    public DateTime EndDate { get; set; }

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
