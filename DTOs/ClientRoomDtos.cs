namespace SnapdragonApi.DTOs;

public class ClientRoomDto
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public int StatusId { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTime CreatedDate { get; set; }
    public Guid? UpdatedById { get; set; }
    public DateTime UpdatedDate { get; set; }
}

public class CreateClientRoomRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; } = 0;
}

public class UpdateClientRoomRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? SortOrder { get; set; }
    public int? StatusId { get; set; }
}

public class ClientRoomListResponse
{
    public List<ClientRoomDto> Rooms { get; set; } = new();
    public int TotalCount { get; set; }
}
