namespace SnapdragonApi.DTOs;

public class UserSessionContext
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public Guid OfficeId { get; set; }
    public int UserLevel { get; set; }
    public Guid? DefaultWarehouseId { get; set; }
    public List<Guid> AccessibleOfficeIds { get; set; } = new();
    public List<Guid> AccessibleWarehouseIds { get; set; } = new();
    public List<Guid> GroupIds { get; set; } = new();
    public List<string> GroupNames { get; set; } = new();
    public DateTime LoadedAt { get; set; }
}
