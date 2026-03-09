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
public class WarehouseController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<WarehouseController> _logger;
    private readonly IUserSessionService _session;

    public WarehouseController(AppDbContext db, ILogger<WarehouseController> logger, IUserSessionService session)
    {
        _db = db;
        _logger = logger;
        _session = session;
    }

    [HttpGet]
    public async Task<ActionResult<WarehouseListResponse>> GetWarehouses(
        [FromQuery] Guid? companyId = null,
        [FromQuery] int? statusId = null,
        [FromQuery] string? search = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var query = _db.Warehouse.AsQueryable();

        if (companyId.HasValue)
            query = query.Where(w => w.CompanyId == companyId.Value);
        if (statusId.HasValue)
            query = query.Where(w => w.StatusId == statusId.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(w => w.Name.Contains(search));

        var totalCount = await query.CountAsync();
        var warehouses = await query
            .OrderBy(w => w.SortOrder)
            .ThenBy(w => w.Name)
            .Skip(skip)
            .Take(take)
            .Select(w => ToDto(w))
            .ToListAsync();

        return Ok(new WarehouseListResponse { Warehouses = warehouses, TotalCount = totalCount });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WarehouseDto>> GetWarehouse(Guid id)
    {
        var warehouse = await _db.Warehouse.FindAsync(id);
        if (warehouse == null)
            return NotFound();

        return Ok(ToDto(warehouse));
    }

    [HttpPost]
    public async Task<ActionResult<WarehouseDto>> CreateWarehouse([FromBody] CreateWarehouseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Name is required" });

        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        var now = DateTime.UtcNow;
        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            Vid = request.Vid,
            CompanyId = ctx.CompanyId,
            Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim(),
            Name = request.Name,
            SortOrder = request.SortOrder,
            Address = request.Address,
            City = request.City,
            State = request.State,
            ZipCode = request.ZipCode,
            Country = request.Country,
            Phone = request.Phone,
            StatusId = 1,
            CreatedById = ctx.UserId,
            UpdatedById = ctx.UserId,
            CreatedDate = now,
            UpdatedDate = now
        };

        _db.Warehouse.Add(warehouse);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created warehouse {WarehouseId} - {WarehouseName}", warehouse.Id, warehouse.Name);

        return CreatedAtAction(nameof(GetWarehouse), new { id = warehouse.Id }, ToDto(warehouse));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<WarehouseDto>> UpdateWarehouse(Guid id, [FromBody] UpdateWarehouseRequest request)
    {
        var warehouse = await _db.Warehouse.FindAsync(id);
        if (warehouse == null)
            return NotFound();

        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        if (request.Vid.HasValue) warehouse.Vid = request.Vid;
        if (request.Code != null) warehouse.Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();
        if (request.Name != null) warehouse.Name = request.Name;
        if (request.SortOrder.HasValue) warehouse.SortOrder = request.SortOrder.Value;
        if (request.Address != null) warehouse.Address = request.Address;
        if (request.City != null) warehouse.City = request.City;
        if (request.State != null) warehouse.State = request.State;
        if (request.ZipCode != null) warehouse.ZipCode = request.ZipCode;
        if (request.Country != null) warehouse.Country = request.Country;
        if (request.Phone != null) warehouse.Phone = request.Phone;
        if (request.StatusId.HasValue) warehouse.StatusId = request.StatusId.Value;
        if (request.IsActive.HasValue) warehouse.StatusId = request.IsActive.Value ? 1 : 2;

        warehouse.UpdatedById = ctx.UserId;
        warehouse.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated warehouse {WarehouseId}", warehouse.Id);

        return Ok(ToDto(warehouse));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteWarehouse(Guid id)
    {
        var warehouse = await _db.Warehouse.FindAsync(id);
        if (warehouse == null)
            return NotFound();

        _db.Warehouse.Remove(warehouse);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted warehouse {WarehouseId}", id);

        return NoContent();
    }

    private async Task<UserSessionContext?> GetSessionAsync()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email)) return null;
        return await _session.GetOrLoadAsync(email);
    }

    private static WarehouseDto ToDto(Warehouse w) => new()
    {
        Id = w.Id,
        Vid = w.Vid,
        CompanyId = w.CompanyId,
        Code = w.Code,
        Name = w.Name,
        SortOrder = w.SortOrder,
        Address = w.Address,
        City = w.City,
        State = w.State,
        ZipCode = w.ZipCode,
        Country = w.Country,
        Phone = w.Phone,
        StatusId = w.StatusId,
        IsActive = w.StatusId == 1,
        CreatedById = w.CreatedById,
        CreatedDate = w.CreatedDate,
        UpdatedById = w.UpdatedById,
        UpdatedDate = w.UpdatedDate
    };
}
