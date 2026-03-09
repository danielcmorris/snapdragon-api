using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnapdragonApi.Models;

[Table("office")]
public class Office
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

    [Required]
    [MaxLength(200)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("address")]
    public string? Address { get; set; }

    [MaxLength(100)]
    [Column("city")]
    public string? City { get; set; }

    [MaxLength(50)]
    [Column("state")]
    public string? State { get; set; }

    [MaxLength(20)]
    [Column("zip_code")]
    public string? ZipCode { get; set; }

    [MaxLength(100)]
    [Column("country")]
    public string? Country { get; set; }

    [MaxLength(50)]
    [Column("phone")]
    public string? Phone { get; set; }

    [MaxLength(255)]
    [Column("email")]
    public string? Email { get; set; }

    [MaxLength(20)]
    [Column("office_number")]
    public string? OfficeNumber { get; set; }

    [MaxLength(200)]
    [Column("contact_name")]
    public string? ContactName { get; set; }

    [MaxLength(50)]
    [Column("fax")]
    public string? Fax { get; set; }

    [MaxLength(500)]
    [Column("address2")]
    public string? Address2 { get; set; }

    [MaxLength(500)]
    [Column("address3")]
    public string? Address3 { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; } = 99;

    [Column("tax_region_id")]
    public int? TaxRegionId { get; set; }

    [Column("default_warehouse_id")]
    public Guid? DefaultWarehouseId { get; set; }

    [Column("show_on_contact_list")]
    public bool ShowOnContactList { get; set; } = true;

    [MaxLength(200)]
    [Column("service_charge_label")]
    public string? ServiceChargeLabel { get; set; }

    [Column("discount_threshold")]
    public decimal DiscountThreshold { get; set; } = 0;

    [Column("feature_flags")]
    public string? FeatureFlags { get; set; }

    [MaxLength(200)]
    [Column("quickbooks_class")]
    public string? QuickbooksClass { get; set; }

    [Column("ns_location_id")]
    public int NsLocationId { get; set; } = 0;

    [Column("ns_entity_id")]
    public int NsEntityId { get; set; } = 0;

    [Column("is_headquarters")]
    public bool IsHeadquarters { get; set; } = false;

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
