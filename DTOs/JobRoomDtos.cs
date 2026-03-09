namespace SnapdragonApi.DTOs;

public class JobRoomDto
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Guid? ClientRoomId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? SetupDatetime { get; set; }
    public DateTime? EventDatetime { get; set; }
    public DateTime? StrikeDatetime { get; set; }
    public int SortOrder { get; set; }
    public int StatusId { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTime CreatedDate { get; set; }
    public Guid? UpdatedById { get; set; }
    public DateTime UpdatedDate { get; set; }
}

public class CreateJobRoomRequest
{
    public Guid? ClientRoomId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? SetupDatetime { get; set; }
    public DateTime? EventDatetime { get; set; }
    public DateTime? StrikeDatetime { get; set; }
    public int SortOrder { get; set; } = 0;
}

public class UpdateJobRoomRequest
{
    public Guid? ClientRoomId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTime? SetupDatetime { get; set; }
    public DateTime? EventDatetime { get; set; }
    public DateTime? StrikeDatetime { get; set; }
    public int? SortOrder { get; set; }
    public int? StatusId { get; set; }
}

public class JobRoomListResponse
{
    public List<JobRoomDto> Rooms { get; set; } = new();
    public int TotalCount { get; set; }
}
