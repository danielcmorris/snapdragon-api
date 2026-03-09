using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnapdragonApi.Models;

[Table("master_product")]
public class MasterProduct
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("vid")]
    public int? Vid { get; set; }

    [Required]
    [Column("company_id")]
    public Guid CompanyId { get; set; }

    [ForeignKey(nameof(CompanyId))]
    public Company? Company { get; set; }

    [MaxLength(100)]
    [Column("product_id")]
    public string? ProductId { get; set; }

    [MaxLength(100)]
    [Column("part_number")]
    public string? PartNumber { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("product_name")]
    public string ProductName { get; set; } = string.Empty;

    [MaxLength(200)]
    [Column("friendly_name")]
    public string? FriendlyName { get; set; }

    [MaxLength(100)]
    [Column("product_type")]
    public string? ProductType { get; set; }

    [Column("default_cost", TypeName = "decimal(18,4)")]
    public decimal? DefaultCost { get; set; }

    [Column("default_price", TypeName = "decimal(18,4)")]
    public decimal? DefaultPrice { get; set; }

    [MaxLength(50)]
    [Column("status")]
    public string? Status { get; set; }

    [Column("price_level_a", TypeName = "decimal(18,4)")]
    public decimal? PriceLevelA { get; set; }

    [Column("price_level_b", TypeName = "decimal(18,4)")]
    public decimal? PriceLevelB { get; set; }

    [Column("price_level_c", TypeName = "decimal(18,4)")]
    public decimal? PriceLevelC { get; set; }

    [Column("price_level_d", TypeName = "decimal(18,4)")]
    public decimal? PriceLevelD { get; set; }

    [Column("price_level_e", TypeName = "decimal(18,4)")]
    public decimal? PriceLevelE { get; set; }

    [MaxLength(100)]
    [Column("manufacturer")]
    public string? Manufacturer { get; set; }

    [Column("date_added")]
    public DateTime? DateAdded { get; set; }

    [Column("taxable")]
    public bool Taxable { get; set; } = true;

    [MaxLength(50)]
    [Column("tax_type")]
    public string? TaxType { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [MaxLength(100)]
    [Column("category")]
    public string? Category { get; set; }

    [MaxLength(100)]
    [Column("subcategory")]
    public string? Subcategory { get; set; }

    [MaxLength(50)]
    [Column("unit_of_measure")]
    public string? UnitOfMeasure { get; set; }

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
