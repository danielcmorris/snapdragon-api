using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnapdragonApi.DTOs;
using SnapdragonApi.Models;
using SnapdragonApi.Services;

namespace SnapdragonApi.Controllers;

[ApiController]
[Route("api/client/{clientId:guid}/room")]
[Authorize]
public class ClientRoomController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<ClientRoomController> _logger;
    private readonly IUserSessionService _session;

    public ClientRoomController(AppDbContext db, ILogger<ClientRoomController> logger, IUserSessionService session)
    {
        _db = db;
        _logger = logger;
        _session = session;
    }

    [HttpGet]
    public async Task<ActionResult<ClientRoomListResponse>> GetRooms(Guid clientId)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var client = await _db.Client.FindAsync(clientId);
        if (client == null || client.CompanyId != ctx.CompanyId) return NotFound();

        var rooms = await _db.ClientRoom
            .Where(r => r.ClientId == clientId && r.StatusId == 1)
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Name)
            .Select(r => ToDto(r))
            .ToListAsync();

        return Ok(new ClientRoomListResponse { Rooms = rooms, TotalCount = rooms.Count });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClientRoomDto>> GetRoom(Guid clientId, Guid id)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var client = await _db.Client.FindAsync(clientId);
        if (client == null || client.CompanyId != ctx.CompanyId) return NotFound();

        var room = await _db.ClientRoom.FindAsync(id);
        if (room == null || room.ClientId != clientId) return NotFound();

        return Ok(ToDto(room));
    }

    [HttpPost]
    public async Task<ActionResult<ClientRoomDto>> CreateRoom(Guid clientId, [FromBody] CreateClientRoomRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var client = await _db.Client.FindAsync(clientId);
        if (client == null || client.CompanyId != ctx.CompanyId) return NotFound();

        var now = DateTime.UtcNow;
        var room = new ClientRoom
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            Name = request.Name,
            Description = request.Description,
            SortOrder = request.SortOrder,
            StatusId = 1,
            CreatedById = ctx.UserId,
            UpdatedById = ctx.UserId,
            CreatedDate = now,
            UpdatedDate = now
        };

        _db.ClientRoom.Add(room);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created client room {RoomId} for client {ClientId}", room.Id, clientId);

        return CreatedAtAction(nameof(GetRoom), new { clientId, id = room.Id }, ToDto(room));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ClientRoomDto>> UpdateRoom(Guid clientId, Guid id, [FromBody] UpdateClientRoomRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var client = await _db.Client.FindAsync(clientId);
        if (client == null || client.CompanyId != ctx.CompanyId) return NotFound();

        var room = await _db.ClientRoom.FindAsync(id);
        if (room == null || room.ClientId != clientId) return NotFound();

        if (request.Name != null) room.Name = request.Name;
        if (request.Description != null) room.Description = request.Description;
        if (request.SortOrder.HasValue) room.SortOrder = request.SortOrder.Value;
        if (request.StatusId.HasValue) room.StatusId = request.StatusId.Value;

        room.UpdatedById = ctx.UserId;
        room.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(ToDto(room));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteRoom(Guid clientId, Guid id)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var client = await _db.Client.FindAsync(clientId);
        if (client == null || client.CompanyId != ctx.CompanyId) return NotFound();

        var room = await _db.ClientRoom.FindAsync(id);
        if (room == null || room.ClientId != clientId) return NotFound();

        _db.ClientRoom.Remove(room);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private async Task<UserSessionContext?> GetSessionAsync()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email)) return null;
        return await _session.GetOrLoadAsync(email);
    }

    private static ClientRoomDto ToDto(ClientRoom r) => new()
    {
        Id = r.Id,
        ClientId = r.ClientId,
        Name = r.Name,
        Description = r.Description,
        SortOrder = r.SortOrder,
        StatusId = r.StatusId,
        CreatedById = r.CreatedById,
        CreatedDate = r.CreatedDate,
        UpdatedById = r.UpdatedById,
        UpdatedDate = r.UpdatedDate
    };
}
