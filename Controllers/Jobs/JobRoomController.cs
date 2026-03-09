using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnapdragonApi.DTOs;
using SnapdragonApi.Models;
using SnapdragonApi.Services;

namespace SnapdragonApi.Controllers.Jobs;

[ApiController]
[Route("api/job/{jobId:guid}/room")]
[Authorize]
public class JobRoomController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<JobRoomController> _logger;
    private readonly IUserSessionService _session;

    public JobRoomController(AppDbContext db, ILogger<JobRoomController> logger, IUserSessionService session)
    {
        _db = db;
        _logger = logger;
        _session = session;
    }

    [HttpGet]
    public async Task<ActionResult<JobRoomListResponse>> GetRooms(Guid jobId)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var job = await _db.Job.FindAsync(jobId);
        if (job == null || job.CompanyId != ctx.CompanyId) return NotFound();

        var rooms = await _db.JobRoom
            .Where(r => r.JobId == jobId && r.StatusId == 1)
            .OrderBy(r => r.SetupDatetime)
            .ThenBy(r => r.SortOrder)
            .Select(r => ToDto(r))
            .ToListAsync();

        return Ok(new JobRoomListResponse { Rooms = rooms, TotalCount = rooms.Count });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobRoomDto>> GetRoom(Guid jobId, Guid id)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var job = await _db.Job.FindAsync(jobId);
        if (job == null || job.CompanyId != ctx.CompanyId) return NotFound();

        var room = await _db.JobRoom.FindAsync(id);
        if (room == null || room.JobId != jobId) return NotFound();

        return Ok(ToDto(room));
    }

    [HttpPost]
    public async Task<ActionResult<JobRoomDto>> CreateRoom(Guid jobId, [FromBody] CreateJobRoomRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var job = await _db.Job.FindAsync(jobId);
        if (job == null || job.CompanyId != ctx.CompanyId) return NotFound();

        if (request.ClientRoomId.HasValue)
        {
            var cr = await _db.ClientRoom.FindAsync(request.ClientRoomId.Value);
            if (cr == null || cr.ClientId != job.ClientId)
                return BadRequest(new { error = "Invalid client room" });
        }

        var now = DateTime.UtcNow;
        var room = new JobRoom
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            ClientRoomId = request.ClientRoomId,
            Name = request.Name,
            Description = request.Description,
            SetupDatetime = Utc(request.SetupDatetime),
            EventDatetime = Utc(request.EventDatetime),
            StrikeDatetime = Utc(request.StrikeDatetime),
            SortOrder = request.SortOrder,
            StatusId = 1,
            CreatedById = ctx.UserId,
            UpdatedById = ctx.UserId,
            CreatedDate = now,
            UpdatedDate = now
        };

        _db.JobRoom.Add(room);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created job room {RoomId} for job {JobId}", room.Id, jobId);

        return CreatedAtAction(nameof(GetRoom), new { jobId, id = room.Id }, ToDto(room));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<JobRoomDto>> UpdateRoom(Guid jobId, Guid id, [FromBody] UpdateJobRoomRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var job = await _db.Job.FindAsync(jobId);
        if (job == null || job.CompanyId != ctx.CompanyId) return NotFound();

        var room = await _db.JobRoom.FindAsync(id);
        if (room == null || room.JobId != jobId) return NotFound();

        if (request.ClientRoomId.HasValue)
        {
            var cr = await _db.ClientRoom.FindAsync(request.ClientRoomId.Value);
            if (cr == null || cr.ClientId != job.ClientId)
                return BadRequest(new { error = "Invalid client room" });
            room.ClientRoomId = request.ClientRoomId;
        }

        if (request.Name != null) room.Name = request.Name;
        if (request.Description != null) room.Description = request.Description;
        if (request.SetupDatetime.HasValue) room.SetupDatetime = Utc(request.SetupDatetime);
        if (request.EventDatetime.HasValue) room.EventDatetime = Utc(request.EventDatetime);
        if (request.StrikeDatetime.HasValue) room.StrikeDatetime = Utc(request.StrikeDatetime);
        if (request.SortOrder.HasValue) room.SortOrder = request.SortOrder.Value;
        if (request.StatusId.HasValue) room.StatusId = request.StatusId.Value;

        room.UpdatedById = ctx.UserId;
        room.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(ToDto(room));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteRoom(Guid jobId, Guid id)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var job = await _db.Job.FindAsync(jobId);
        if (job == null || job.CompanyId != ctx.CompanyId) return NotFound();

        var room = await _db.JobRoom.FindAsync(id);
        if (room == null || room.JobId != jobId) return NotFound();

        // Cascade delete all products in this room
        var products = await _db.JobRoomProduct.Where(p => p.JobRoomId == id).ToListAsync();
        _db.JobRoomProduct.RemoveRange(products);

        _db.JobRoom.Remove(room);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted job room {RoomId} from job {JobId}", id, jobId);

        return NoContent();
    }

    private static DateTime? Utc(DateTime? dt) =>
        dt.HasValue ? DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc) : null;

    private async Task<UserSessionContext?> GetSessionAsync()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email)) return null;
        return await _session.GetOrLoadAsync(email);
    }

    private static JobRoomDto ToDto(JobRoom r) => new()
    {
        Id = r.Id,
        JobId = r.JobId,
        ClientRoomId = r.ClientRoomId,
        Name = r.Name,
        Description = r.Description,
        SetupDatetime = r.SetupDatetime,
        EventDatetime = r.EventDatetime,
        StrikeDatetime = r.StrikeDatetime,
        SortOrder = r.SortOrder,
        StatusId = r.StatusId,
        CreatedById = r.CreatedById,
        CreatedDate = r.CreatedDate,
        UpdatedById = r.UpdatedById,
        UpdatedDate = r.UpdatedDate
    };

}
