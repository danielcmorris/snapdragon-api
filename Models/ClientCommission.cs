using System.ComponentModel.DataAnnotations.Schema;

namespace SnapdragonApi.Models;

[Table("client_commission")]
public class ClientCommission
{
    [Column("id")] public Guid Id { get; set; }
    [Column("client_id")] public Guid ClientId { get; set; }
    public Client? Client { get; set; }
    [Column("commission_type_id")] public int? CommissionTypeId { get; set; }
    public CommissionType? CommissionType { get; set; }
    [Column("name")] public string Name { get; set; } = "";
    [Column("rate")] public decimal Rate { get; set; } = 0;
    [Column("sort_order")] public int SortOrder { get; set; } = 0;
    [Column("status_id")] public int StatusId { get; set; } = 1;
    [Column("created_by_id")] public Guid? CreatedById { get; set; }
    [Column("created_date")] public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    [Column("updated_by_id")] public Guid? UpdatedById { get; set; }
    [Column("updated_date")] public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
}
