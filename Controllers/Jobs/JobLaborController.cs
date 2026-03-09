using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnapdragonApi.DTOs;
using SnapdragonApi.Models;
using SnapdragonApi.Services;

namespace SnapdragonApi.Controllers.Jobs;

[ApiController]
[Route("api/job/{jobId:guid}/labor")]
[Authorize]
public class JobLaborController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IUserSessionService _session;

    public JobLaborController(AppDbContext db, IUserSessionService session)
    {
        _db = db;
        _session = session;
    }

    [HttpGet]
    public async Task<ActionResult<JobLaborListResponse>> List(Guid jobId)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var job = await _db.Job.FindAsync(jobId);
        if (job == null || job.CompanyId != ctx.CompanyId) return NotFound();

        var items = await _db.JobLabor
            .Where(l => l.JobId == jobId)
            .Include(l => l.JobRoom)
            .OrderBy(l => l.SortOrder)
            .ThenBy(l => l.CreatedDate)
            .ToListAsync();

        return Ok(new JobLaborListResponse { Items = items.Select(ToDto).ToList() });
    }

    [HttpPost]
    public async Task<ActionResult<JobLaborDto>> Create(Guid jobId, [FromBody] CreateJobLaborRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var job = await _db.Job.FindAsync(jobId);
        if (job == null || job.CompanyId != ctx.CompanyId) return NotFound();

        if (request.JobRoomId.HasValue)
        {
            var room = await _db.JobRoom.FindAsync(request.JobRoomId.Value);
            if (room == null || room.JobId != jobId) return BadRequest(new { error = "Invalid room" });
        }

        var labor = new JobLabor
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            JobRoomId = request.JobRoomId,
            Quantity = request.Quantity,
            Employee = request.Employee,
            Task = request.Task,
            Hours = request.Hours,
            Cost = request.Cost,
            Rate = request.Rate,
            CommissionPercent = request.CommissionPercent,
            TaxRate = request.TaxRate,
            Subcontracted = request.Subcontracted,
            ServiceChargeOverride = request.ServiceChargeOverride,
            CreatedById = ctx.UserId,
            CreatedDate = DateTime.UtcNow,
            UpdatedById = ctx.UserId,
            UpdatedDate = DateTime.UtcNow,
        };

        _db.JobLabor.Add(labor);
        await _db.SaveChangesAsync();

        await _db.Entry(labor).Reference(l => l.JobRoom).LoadAsync();
        return Ok(ToDto(labor));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<JobLaborDto>> Update(Guid jobId, Guid id, [FromBody] UpdateJobLaborRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var job = await _db.Job.FindAsync(jobId);
        if (job == null || job.CompanyId != ctx.CompanyId) return NotFound();

        var labor = await _db.JobLabor
            .Include(l => l.JobRoom)
            .FirstOrDefaultAsync(l => l.Id == id && l.JobId == jobId);
        if (labor == null) return NotFound();

        if (request.JobRoomId.HasValue)
        {
            var room = await _db.JobRoom.FindAsync(request.JobRoomId.Value);
            if (room == null || room.JobId != jobId) return BadRequest(new { error = "Invalid room" });
            labor.JobRoomId = request.JobRoomId;
            await _db.Entry(labor).Reference(l => l.JobRoom).LoadAsync();
        }

        if (request.Quantity.HasValue) labor.Quantity = request.Quantity.Value;
        if (request.Employee != null) labor.Employee = request.Employee;
        if (request.Task != null) labor.Task = request.Task;
        if (request.Hours.HasValue) labor.Hours = request.Hours.Value;
        if (request.Cost.HasValue) labor.Cost = request.Cost.Value;
        if (request.Rate.HasValue) labor.Rate = request.Rate.Value;
        if (request.CommissionPercent.HasValue) labor.CommissionPercent = request.CommissionPercent.Value;
        if (request.TaxRate.HasValue) labor.TaxRate = request.TaxRate.Value;
        if (request.Subcontracted.HasValue) labor.Subcontracted = request.Subcontracted.Value;
        if (request.ServiceChargeOverride.HasValue) labor.ServiceChargeOverride = request.ServiceChargeOverride.Value;
        labor.UpdatedById = ctx.UserId;
        labor.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ToDto(labor));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid jobId, Guid id)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var job = await _db.Job.FindAsync(jobId);
        if (job == null || job.CompanyId != ctx.CompanyId) return NotFound();

        var labor = await _db.JobLabor.FirstOrDefaultAsync(l => l.Id == id && l.JobId == jobId);
        if (labor == null) return NotFound();

        _db.JobLabor.Remove(labor);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static JobLaborDto ToDto(JobLabor l) => new()
    {
        Id = l.Id,
        JobId = l.JobId,
        JobRoomId = l.JobRoomId,
        RoomName = l.JobRoom?.Name,
        Quantity = l.Quantity,
        Employee = l.Employee,
        Task = l.Task,
        Hours = l.Hours,
        Cost = l.Cost,
        Rate = l.Rate,
        CommissionPercent = l.CommissionPercent,
        TaxRate = l.TaxRate,
        Subcontracted = l.Subcontracted,
        ServiceChargeOverride = l.ServiceChargeOverride,
        SortOrder = l.SortOrder,
        CreatedById = l.CreatedById,
        CreatedDate = l.CreatedDate,
        UpdatedById = l.UpdatedById,
        UpdatedDate = l.UpdatedDate,
    };

    private async Task<UserSessionContext?> GetSessionAsync()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email)) return null;
        return await _session.GetOrLoadAsync(email);
    }
}
