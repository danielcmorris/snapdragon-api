using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnapdragonApi.Models;

[Table("package_product")]
public class PackageProduct
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("package_id")]
    public Guid PackageId { get; set; }

    [ForeignKey(nameof(PackageId))]
    public Package? Package { get; set; }

    [Required]
    [Column("master_product_id")]
    public Guid MasterProductId { get; set; }

    [ForeignKey(nameof(MasterProductId))]
    public MasterProduct? MasterProduct { get; set; }

    [Column("quantity", TypeName = "decimal(10,2)")]
    public decimal Quantity { get; set; } = 1;

    [Column("sort_order")]
    public int SortOrder { get; set; } = 0;

    [Column("created_by_id")]
    public Guid? CreatedById { get; set; }

    [Column("created_date")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
