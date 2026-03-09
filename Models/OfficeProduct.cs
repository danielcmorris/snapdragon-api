using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnapdragonApi.Models;

[Table("office_product")]
public class OfficeProduct
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("office_id")]
    public Guid OfficeId { get; set; }

    [ForeignKey(nameof(OfficeId))]
    public Office? Office { get; set; }

    [Required]
    [Column("master_product_id")]
    public Guid MasterProductId { get; set; }

    [ForeignKey(nameof(MasterProductId))]
    public MasterProduct? MasterProduct { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; } = 0;

    [Column("created_by_id")]
    public Guid? CreatedById { get; set; }

    [Column("created_date")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
