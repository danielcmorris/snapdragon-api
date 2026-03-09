using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnapdragonApi.DTOs;
using SnapdragonApi.Models;
using SnapdragonApi.Services;

namespace SnapdragonApi.Controllers.Jobs;

[ApiController]
[Route("api/package")]
[Authorize]
public class PackageController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<PackageController> _logger;
    private readonly IUserSessionService _session;

    public PackageController(AppDbContext db, ILogger<PackageController> logger, IUserSessionService session)
    {
        _db = db;
        _logger = logger;
        _session = session;
    }

    [HttpGet]
    public async Task<ActionResult<PackageListResponse>> GetPackages(
        [FromQuery] string? search = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var query = _db.Package.Where(p => p.CompanyId == ctx.CompanyId && p.StatusId == 1);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search) || (p.Description != null && p.Description.Contains(search)));

        var totalCount = await query.CountAsync();
        var packages = await query
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .Skip(skip)
            .Take(take)
            .Select(p => ToDto(p))
            .ToListAsync();

        return Ok(new PackageListResponse { Packages = packages, TotalCount = totalCount });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PackageDto>> GetPackage(Guid id)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var pkg = await _db.Package.FindAsync(id);
        if (pkg == null || pkg.CompanyId != ctx.CompanyId) return NotFound();

        return Ok(ToDto(pkg));
    }

    [HttpPost]
    public async Task<ActionResult<PackageDto>> CreatePackage([FromBody] CreatePackageRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var now = DateTime.UtcNow;
        var pkg = new Package
        {
            Id = Guid.NewGuid(),
            CompanyId = ctx.CompanyId,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            SortOrder = request.SortOrder,
            StatusId = 1,
            CreatedById = ctx.UserId,
            UpdatedById = ctx.UserId,
            CreatedDate = now,
            UpdatedDate = now
        };

        _db.Package.Add(pkg);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created package {PackageId}", pkg.Id);

        return CreatedAtAction(nameof(GetPackage), new { id = pkg.Id }, ToDto(pkg));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PackageDto>> UpdatePackage(Guid id, [FromBody] UpdatePackageRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var pkg = await _db.Package.FindAsync(id);
        if (pkg == null || pkg.CompanyId != ctx.CompanyId) return NotFound();

        if (request.Name != null) pkg.Name = request.Name;
        if (request.Description != null) pkg.Description = request.Description;
        if (request.Price.HasValue) pkg.Price = request.Price;
        if (request.SortOrder.HasValue) pkg.SortOrder = request.SortOrder.Value;
        if (request.StatusId.HasValue) pkg.StatusId = request.StatusId.Value;

        pkg.UpdatedById = ctx.UserId;
        pkg.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(ToDto(pkg));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeletePackage(Guid id)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var pkg = await _db.Package.FindAsync(id);
        if (pkg == null || pkg.CompanyId != ctx.CompanyId) return NotFound();

        _db.Package.Remove(pkg);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{id:guid}/product")]
    public async Task<ActionResult<List<PackageProductDto>>> GetPackageProducts(Guid id)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var pkg = await _db.Package.FindAsync(id);
        if (pkg == null || pkg.CompanyId != ctx.CompanyId) return NotFound();

        var items = await _db.PackageProduct
            .Where(pp => pp.PackageId == id)
            .Include(pp => pp.MasterProduct)
            .OrderBy(pp => pp.SortOrder)
            .Select(pp => new PackageProductDto
            {
                Id = pp.Id,
                PackageId = pp.PackageId,
                MasterProductId = pp.MasterProductId,
                ProductName = pp.MasterProduct != null ? pp.MasterProduct.ProductName : null,
                PartNumber = pp.MasterProduct != null ? pp.MasterProduct.PartNumber : null,
                Quantity = pp.Quantity,
                SortOrder = pp.SortOrder,
                CreatedById = pp.CreatedById,
                CreatedDate = pp.CreatedDate
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost("{id:guid}/product")]
    public async Task<ActionResult<PackageProductDto>> AddPackageProduct(Guid id, [FromBody] AddPackageProductRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var pkg = await _db.Package.FindAsync(id);
        if (pkg == null || pkg.CompanyId != ctx.CompanyId) return NotFound();

        var mp = await _db.MasterProduct.FindAsync(request.MasterProductId);
        if (mp == null || mp.CompanyId != ctx.CompanyId)
            return BadRequest(new { error = "Invalid product" });

        var item = new PackageProduct
        {
            Id = Guid.NewGuid(),
            PackageId = id,
            MasterProductId = request.MasterProductId,
            Quantity = request.Quantity,
            SortOrder = request.SortOrder,
            CreatedById = ctx.UserId,
            CreatedDate = DateTime.UtcNow
        };

        _db.PackageProduct.Add(item);
        await _db.SaveChangesAsync();

        return Ok(new PackageProductDto
        {
            Id = item.Id,
            PackageId = item.PackageId,
            MasterProductId = item.MasterProductId,
            ProductName = mp.ProductName,
            PartNumber = mp.PartNumber,
            Quantity = item.Quantity,
            SortOrder = item.SortOrder,
            CreatedById = item.CreatedById,
            CreatedDate = item.CreatedDate
        });
    }

    [HttpDelete("{id:guid}/product/{productId:guid}")]
    public async Task<IActionResult> RemovePackageProduct(Guid id, Guid productId)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var pkg = await _db.Package.FindAsync(id);
        if (pkg == null || pkg.CompanyId != ctx.CompanyId) return NotFound();

        var item = await _db.PackageProduct.FindAsync(productId);
        if (item == null || item.PackageId != id) return NotFound();

        _db.PackageProduct.Remove(item);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private async Task<UserSessionContext?> GetSessionAsync()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email)) return null;
        return await _session.GetOrLoadAsync(email);
    }

    private static PackageDto ToDto(Package p) => new()
    {
        Id = p.Id,
        CompanyId = p.CompanyId,
        Name = p.Name,
        Description = p.Description,
        Price = p.Price,
        SortOrder = p.SortOrder,
        StatusId = p.StatusId,
        CreatedById = p.CreatedById,
        CreatedDate = p.CreatedDate,
        UpdatedById = p.UpdatedById,
        UpdatedDate = p.UpdatedDate
    };
}
