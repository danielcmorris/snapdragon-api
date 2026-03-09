using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnapdragonApi.DTOs;
using SnapdragonApi.Models;
using SnapdragonApi.Services;

namespace SnapdragonApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AllocationController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<AllocationController> _logger;
    private readonly IUserSessionService _session;

    public AllocationController(AppDbContext db, ILogger<AllocationController> logger, IUserSessionService session)
    {
        _db = db;
        _logger = logger;
        _session = session;
    }

    [HttpGet]
    public async Task<ActionResult<AllocationListResponse>> GetAllocations(
        [FromQuery] Guid? stockId = null,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var query = _db.Allocation.AsQueryable();

        if (stockId.HasValue)
            query = query.Where(a => a.StockId == stockId.Value);
        if (warehouseId.HasValue)
            query = query.Where(a => a.WarehouseId == warehouseId.Value);
        if (startDate.HasValue)
            query = query.Where(a => a.EndDate >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(a => a.StartDate <= endDate.Value);

        var totalCount = await query.CountAsync();
        var allocations = await query
            .OrderBy(a => a.StartDate)
            .Skip(skip)
            .Take(take)
            .Select(a => ToDto(a))
            .ToListAsync();

        return Ok(new AllocationListResponse { Allocations = allocations, TotalCount = totalCount });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AllocationDto>> GetAllocation(Guid id)
    {
        var allocation = await _db.Allocation.FindAsync(id);
        if (allocation == null)
            return NotFound();

        return Ok(ToDto(allocation));
    }

    [HttpPost]
    public async Task<ActionResult<AllocationDto>> CreateAllocation([FromBody] CreateAllocationRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        var now = DateTime.UtcNow;
        var allocation = new Allocation
        {
            Id = Guid.NewGuid(),
            Vid = request.Vid,
            StockId = request.StockId,
            WarehouseId = request.WarehouseId,
            Quantity = request.Quantity,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            StatusId = 1,
            CreatedById = ctx.UserId,
            UpdatedById = ctx.UserId,
            CreatedDate = now,
            UpdatedDate = now
        };

        _db.Allocation.Add(allocation);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created allocation {AllocationId}", allocation.Id);

        return CreatedAtAction(nameof(GetAllocation), new { id = allocation.Id }, ToDto(allocation));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AllocationDto>> UpdateAllocation(Guid id, [FromBody] UpdateAllocationRequest request)
    {
        var allocation = await _db.Allocation.FindAsync(id);
        if (allocation == null)
            return NotFound();

        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        if (request.Vid.HasValue) allocation.Vid = request.Vid;
        if (request.WarehouseId.HasValue) allocation.WarehouseId = request.WarehouseId;
        if (request.Quantity.HasValue) allocation.Quantity = request.Quantity.Value;
        if (request.StartDate.HasValue) allocation.StartDate = request.StartDate.Value;
        if (request.EndDate.HasValue) allocation.EndDate = request.EndDate.Value;

        allocation.UpdatedById = ctx.UserId;
        allocation.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated allocation {AllocationId}", allocation.Id);

        return Ok(ToDto(allocation));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAllocation(Guid id)
    {
        var allocation = await _db.Allocation.FindAsync(id);
        if (allocation == null)
            return NotFound();

        _db.Allocation.Remove(allocation);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted allocation {AllocationId}", id);

        return NoContent();
    }

    private async Task<UserSessionContext?> GetSessionAsync()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email)) return null;
        return await _session.GetOrLoadAsync(email);
    }

    private static AllocationDto ToDto(Allocation a) => new()
    {
        Id = a.Id,
        Vid = a.Vid,
        StockId = a.StockId,
        WarehouseId = a.WarehouseId,
        Quantity = a.Quantity,
        StartDate = a.StartDate,
        EndDate = a.EndDate,
        StatusId = a.StatusId,
        CreatedById = a.CreatedById,
        CreatedDate = a.CreatedDate,
        UpdatedById = a.UpdatedById,
        UpdatedDate = a.UpdatedDate
    };
}
