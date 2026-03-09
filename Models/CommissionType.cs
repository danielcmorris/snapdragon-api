using System.ComponentModel.DataAnnotations.Schema;

namespace SnapdragonApi.Models;

[Table("commission_type")]
public class CommissionType
{
    [Column("id")] public int Id { get; set; }
    [Column("name")] public string Name { get; set; } = "";
    [Column("default_percent")] public decimal DefaultPercent { get; set; } = 0;
    [Column("sort_order")] public int SortOrder { get; set; } = 99;
    [Column("status_id")] public int StatusId { get; set; } = 1;
    [Column("created_by_id")] public Guid? CreatedById { get; set; }
    [Column("created_date")] public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    [Column("updated_by_id")] public Guid? UpdatedById { get; set; }
    [Column("updated_date")] public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
}
