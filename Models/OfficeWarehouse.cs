using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnapdragonApi.Models;

[Table("office_warehouse")]
public class OfficeWarehouse
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("office_id")]
    public Guid OfficeId { get; set; }

    [ForeignKey(nameof(OfficeId))]
    public Office? Office { get; set; }

    [Required]
    [Column("warehouse_id")]
    public Guid WarehouseId { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }

    [Column("created_date")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
