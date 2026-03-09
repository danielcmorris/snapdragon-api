using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnapdragonApi.DTOs;
using SnapdragonApi.Models;
using SnapdragonApi.Services;

namespace SnapdragonApi.Controllers.Admin;

[ApiController]
[Route("api/tax-region")]
[Authorize]
public class TaxRegionController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IUserSessionService _session;

    public TaxRegionController(AppDbContext db, IUserSessionService session)
    {
        _db = db;
        _session = session;
    }

    [HttpGet]
    public async Task<ActionResult<List<TaxRegionDto>>> List()
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var regions = await _db.TaxRegion
            .Where(r => r.StatusId == 1)
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Name)
            .Select(r => ToDto(r))
            .ToListAsync();

        return Ok(regions);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TaxRegionDto>> Get(int id)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var region = await _db.TaxRegion.FindAsync(id);
        if (region == null || region.StatusId != 1) return NotFound();

        return Ok(ToDto(region));
    }

    [HttpPost]
    public async Task<ActionResult<TaxRegionDto>> Create([FromBody] CreateTaxRegionRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 3) return StatusCode(403, new { error = "Insufficient permissions" });

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Name is required" });

        var now = DateTime.UtcNow;
        var region = new TaxRegion
        {
            Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim().ToUpper(),
            Name = request.Name.Trim(),
            SalesTax = request.SalesTax,
            LaborTax = request.LaborTax,
            SortOrder = request.SortOrder,
            StatusId = 1,
            CreatedById = ctx.UserId,
            UpdatedById = ctx.UserId,
            CreatedDate = now,
            UpdatedDate = now,
        };

        _db.TaxRegion.Add(region);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = region.Id }, ToDto(region));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TaxRegionDto>> Update(int id, [FromBody] UpdateTaxRegionRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 3) return StatusCode(403, new { error = "Insufficient permissions" });

        var region = await _db.TaxRegion.FindAsync(id);
        if (region == null || region.StatusId != 1) return NotFound();

        if (request.Code != null) region.Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim().ToUpper();
        if (request.Name != null) region.Name = request.Name.Trim();
        if (request.SalesTax.HasValue) region.SalesTax = request.SalesTax.Value;
        if (request.LaborTax.HasValue) region.LaborTax = request.LaborTax.Value;
        if (request.SortOrder.HasValue) region.SortOrder = request.SortOrder.Value;
        if (request.StatusId.HasValue) region.StatusId = request.StatusId.Value;
        region.UpdatedById = ctx.UserId;
        region.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ToDto(region));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 3) return StatusCode(403, new { error = "Insufficient permissions" });

        var region = await _db.TaxRegion.FindAsync(id);
        if (region == null) return NotFound();

        region.StatusId = 2;
        region.UpdatedById = ctx.UserId;
        region.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static TaxRegionDto ToDto(TaxRegion r) => new()
    {
        Id = r.Id,
        Code = r.Code,
        Name = r.Name,
        SalesTax = r.SalesTax,
        LaborTax = r.LaborTax,
        SortOrder = r.SortOrder,
        StatusId = r.StatusId,
    };

    private async Task<UserSessionContext?> GetSessionAsync()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email)) return null;
        return await _session.GetOrLoadAsync(email);
    }
}
