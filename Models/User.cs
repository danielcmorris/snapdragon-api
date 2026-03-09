using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnapdragonApi.Models;

[Table("user")]
public class User
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("vid")]
    public int? Vid { get; set; }

    [Required]
    [Column("office_id")]
    public Guid OfficeId { get; set; }

    [ForeignKey(nameof(OfficeId))]
    public Office? Office { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("first_name")]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    [Column("last_name")]
    public string? LastName { get; set; }

    [MaxLength(50)]
    [Column("phone")]
    public string? Phone { get; set; }

    [MaxLength(100)]
    [Column("job_title")]
    public string? JobTitle { get; set; }

    [Column("user_level")]
    public int UserLevel { get; set; } = 1;

    [Column("default_warehouse_id")]
    public Guid? DefaultWarehouseId { get; set; }

    [ForeignKey(nameof(DefaultWarehouseId))]
    public Warehouse? DefaultWarehouse { get; set; }

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
