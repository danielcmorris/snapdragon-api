using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnapdragonApi.Models;

[Table("supplier")]
public class Supplier
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

    [MaxLength(50)]
    [Column("code")]
    public string? Code { get; set; }

    [MaxLength(500)]
    [Column("address")]
    public string? Address { get; set; }

    [MaxLength(50)]
    [Column("phone")]
    public string? Phone { get; set; }

    [MaxLength(255)]
    [Column("website")]
    public string? Website { get; set; }

    [MaxLength(100)]
    [Column("category")]
    public string? Category { get; set; }

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
