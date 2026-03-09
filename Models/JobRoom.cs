using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnapdragonApi.Models;

[Table("job_room")]
public class JobRoom
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("job_id")]
    public Guid JobId { get; set; }

    [ForeignKey(nameof(JobId))]
    public Job? Job { get; set; }

    [Column("client_room_id")]
    public Guid? ClientRoomId { get; set; }

    [ForeignKey(nameof(ClientRoomId))]
    public ClientRoom? ClientRoom { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    [Column("setup_datetime")]
    public DateTime? SetupDatetime { get; set; }

    [Column("event_datetime")]
    public DateTime? EventDatetime { get; set; }

    [Column("strike_datetime")]
    public DateTime? StrikeDatetime { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; } = 0;

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
