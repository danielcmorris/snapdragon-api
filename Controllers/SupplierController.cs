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
public class SupplierController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<SupplierController> _logger;
    private readonly IUserSessionService _session;

    public SupplierController(AppDbContext db, ILogger<SupplierController> logger, IUserSessionService session)
    {
        _db = db;
        _logger = logger;
        _session = session;
    }

    [HttpGet]
    public async Task<ActionResult<SupplierListResponse>> GetSuppliers(
        [FromQuery] Guid? companyId = null,
        [FromQuery] int? statusId = null,
        [FromQuery] string? category = null,
        [FromQuery] string? search = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        if (ctx.UserLevel < 2)
            return StatusCode(403, new { error = "Insufficient permissions. Manager role or higher required." });

        var query = _db.Supplier.Where(s => s.CompanyId == ctx.CompanyId);

        if (statusId.HasValue)
            query = query.Where(s => s.StatusId == statusId.Value);
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(s => s.Category == category);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s => s.Name.Contains(search) ||
                (s.Code != null && s.Code.Contains(search)));

        var totalCount = await query.CountAsync();
        var suppliers = await query
            .OrderBy(s => s.Name)
            .Skip(skip)
            .Take(take)
            .Select(s => ToDto(s))
            .ToListAsync();

        return Ok(new SupplierListResponse { Suppliers = suppliers, TotalCount = totalCount });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SupplierDto>> GetSupplier(Guid id)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        if (ctx.UserLevel < 2)
            return StatusCode(403, new { error = "Insufficient permissions. Manager role or higher required." });

        var supplier = await _db.Supplier.FindAsync(id);
        if (supplier == null || supplier.CompanyId != ctx.CompanyId)
            return NotFound();

        return Ok(ToDto(supplier));
    }

    [HttpPost]
    public async Task<ActionResult<SupplierDto>> CreateSupplier([FromBody] CreateSupplierRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Name is required" });

        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        if (ctx.UserLevel < 2)
            return StatusCode(403, new { error = "Insufficient permissions. Manager role or higher required." });

        var now = DateTime.UtcNow;
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Vid = request.Vid,
            CompanyId = ctx.CompanyId,
            Name = request.Name,
            Code = request.Code,
            Address = request.Address,
            Phone = request.Phone,
            Website = request.Website,
            Category = request.Category,
            StatusId = 1,
            CreatedById = ctx.UserId,
            UpdatedById = ctx.UserId,
            CreatedDate = now,
            UpdatedDate = now
        };

        _db.Supplier.Add(supplier);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created supplier {SupplierId} - {SupplierName}", supplier.Id, supplier.Name);

        return CreatedAtAction(nameof(GetSupplier), new { id = supplier.Id }, ToDto(supplier));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SupplierDto>> UpdateSupplier(Guid id, [FromBody] UpdateSupplierRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        if (ctx.UserLevel < 2)
            return StatusCode(403, new { error = "Insufficient permissions. Manager role or higher required." });

        var supplier = await _db.Supplier.FindAsync(id);
        if (supplier == null || supplier.CompanyId != ctx.CompanyId)
            return NotFound();

        if (request.Vid.HasValue) supplier.Vid = request.Vid;
        if (request.Name != null) supplier.Name = request.Name;
        if (request.Code != null) supplier.Code = request.Code;
        if (request.Address != null) supplier.Address = request.Address;
        if (request.Phone != null) supplier.Phone = request.Phone;
        if (request.Website != null) supplier.Website = request.Website;
        if (request.Category != null) supplier.Category = request.Category;
        if (request.StatusId.HasValue) supplier.StatusId = request.StatusId.Value;

        supplier.UpdatedById = ctx.UserId;
        supplier.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated supplier {SupplierId}", supplier.Id);

        return Ok(ToDto(supplier));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteSupplier(Guid id)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        if (ctx.UserLevel < 2)
            return StatusCode(403, new { error = "Insufficient permissions. Manager role or higher required." });

        var supplier = await _db.Supplier.FindAsync(id);
        if (supplier == null || supplier.CompanyId != ctx.CompanyId)
            return NotFound();

        _db.Supplier.Remove(supplier);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted supplier {SupplierId}", id);

        return NoContent();
    }

    private async Task<UserSessionContext?> GetSessionAsync()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email)) return null;
        return await _session.GetOrLoadAsync(email);
    }

    private static SupplierDto ToDto(Supplier s) => new()
    {
        Id = s.Id,
        Vid = s.Vid,
        CompanyId = s.CompanyId,
        Name = s.Name,
        Code = s.Code,
        Address = s.Address,
        Phone = s.Phone,
        Website = s.Website,
        Category = s.Category,
        StatusId = s.StatusId,
        CreatedById = s.CreatedById,
        CreatedDate = s.CreatedDate,
        UpdatedById = s.UpdatedById,
        UpdatedDate = s.UpdatedDate
    };
}
