using System.ComponentModel.DataAnnotations.Schema;

namespace SnapdragonApi.Models;

[Table("client_contact")]
public class ClientContact
{
    [Column("id")] public Guid Id { get; set; }
    [Column("client_id")] public Guid ClientId { get; set; }
    public Client? Client { get; set; }
    [Column("first_name")] public string? FirstName { get; set; }
    [Column("last_name")] public string? LastName { get; set; }
    [Column("email")] public string? Email { get; set; }
    [Column("phone")] public string? Phone { get; set; }
    [Column("notes")] public string? Notes { get; set; }
    [Column("status_id")] public int StatusId { get; set; } = 1;
    [Column("created_by_id")] public Guid? CreatedById { get; set; }
    [Column("created_date")] public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    [Column("updated_by_id")] public Guid? UpdatedById { get; set; }
    [Column("updated_date")] public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
}
