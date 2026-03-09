using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnapdragonApi.DTOs;
using SnapdragonApi.Models;
using SnapdragonApi.Services;

namespace SnapdragonApi.Controllers.Admin;

[ApiController]
[Route("api/commission-type")]
[Authorize]
public class CommissionTypeController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IUserSessionService _session;

    public CommissionTypeController(AppDbContext db, IUserSessionService session)
    {
        _db = db;
        _session = session;
    }

    [HttpGet]
    public async Task<ActionResult<List<CommissionTypeDto>>> List()
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var types = await _db.CommissionType
            .Where(ct => ct.StatusId == 1)
            .OrderBy(ct => ct.SortOrder)
            .ThenBy(ct => ct.Name)
            .Select(ct => ToDto(ct))
            .ToListAsync();

        return Ok(types);
    }

    [HttpPost]
    public async Task<ActionResult<CommissionTypeDto>> Create([FromBody] CreateCommissionTypeRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 3) return StatusCode(403, new { error = "Insufficient permissions" });

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Name is required" });

        var now = DateTime.UtcNow;
        var ct = new CommissionType
        {
            Name = request.Name.Trim(),
            DefaultPercent = request.DefaultPercent,
            SortOrder = request.SortOrder,
            StatusId = 1,
            CreatedById = ctx.UserId,
            UpdatedById = ctx.UserId,
            CreatedDate = now,
            UpdatedDate = now,
        };

        _db.CommissionType.Add(ct);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(List), ToDto(ct));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CommissionTypeDto>> Update(int id, [FromBody] UpdateCommissionTypeRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 3) return StatusCode(403, new { error = "Insufficient permissions" });

        var ct = await _db.CommissionType.FindAsync(id);
        if (ct == null || ct.StatusId != 1) return NotFound();

        if (request.Name != null) ct.Name = request.Name.Trim();
        if (request.DefaultPercent.HasValue) ct.DefaultPercent = request.DefaultPercent.Value;
        if (request.SortOrder.HasValue) ct.SortOrder = request.SortOrder.Value;
        if (request.StatusId.HasValue) ct.StatusId = request.StatusId.Value;
        ct.UpdatedById = ctx.UserId;
        ct.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ToDto(ct));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 3) return StatusCode(403, new { error = "Insufficient permissions" });

        var ct = await _db.CommissionType.FindAsync(id);
        if (ct == null) return NotFound();

        // Soft-delete
        ct.StatusId = 2;
        ct.UpdatedById = ctx.UserId;
        ct.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static CommissionTypeDto ToDto(CommissionType ct) => new()
    {
        Id = ct.Id,
        Name = ct.Name,
        DefaultPercent = ct.DefaultPercent,
        SortOrder = ct.SortOrder,
        StatusId = ct.StatusId,
    };

    private async Task<UserSessionContext?> GetSessionAsync()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email)) return null;
        return await _session.GetOrLoadAsync(email);
    }
}
