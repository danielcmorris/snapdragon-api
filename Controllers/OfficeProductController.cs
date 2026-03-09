using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnapdragonApi.DTOs;
using SnapdragonApi.Models;
using SnapdragonApi.Services;

namespace SnapdragonApi.Controllers;

[ApiController]
[Route("api/office/{officeId:guid}/product-list")]
[Authorize]
public class OfficeProductController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<OfficeProductController> _logger;
    private readonly IUserSessionService _session;

    public OfficeProductController(AppDbContext db, ILogger<OfficeProductController> logger, IUserSessionService session)
    {
        _db = db;
        _logger = logger;
        _session = session;
    }

    [HttpGet]
    public async Task<ActionResult<OfficeProductListResponse>> GetProducts(
        Guid officeId,
        [FromQuery] string? search = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var office = await _db.Office.FindAsync(officeId);
        if (office == null || office.CompanyId != ctx.CompanyId) return NotFound();

        var query = _db.OfficeProduct
            .Where(op => op.OfficeId == officeId)
            .Include(op => op.MasterProduct)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(op =>
                (op.MasterProduct != null && op.MasterProduct.ProductName.Contains(search)) ||
                (op.MasterProduct != null && op.MasterProduct.FriendlyName != null && op.MasterProduct.FriendlyName.Contains(search)) ||
                (op.MasterProduct != null && op.MasterProduct.PartNumber != null && op.MasterProduct.PartNumber.Contains(search)));

        var totalCount = await query.CountAsync();
        var products = await query
            .OrderBy(op => op.SortOrder)
            .ThenBy(op => op.MasterProduct!.ProductName)
            .Skip(skip)
            .Take(take)
            .Select(op => new OfficeProductDto
            {
                Id = op.Id,
                OfficeId = op.OfficeId,
                MasterProductId = op.MasterProductId,
                ProductName = op.MasterProduct != null ? op.MasterProduct.ProductName : null,
                FriendlyName = op.MasterProduct != null ? op.MasterProduct.FriendlyName : null,
                PartNumber = op.MasterProduct != null ? op.MasterProduct.PartNumber : null,
                Category = op.MasterProduct != null ? op.MasterProduct.Category : null,
                DefaultPrice = op.MasterProduct != null ? op.MasterProduct.DefaultPrice : null,
                SortOrder = op.SortOrder,
                CreatedById = op.CreatedById,
                CreatedDate = op.CreatedDate
            })
            .ToListAsync();

        return Ok(new OfficeProductListResponse { Products = products, TotalCount = totalCount });
    }

    [HttpPost]
    public async Task<ActionResult<OfficeProductDto>> AddProduct(Guid officeId, [FromBody] AddOfficeProductRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var office = await _db.Office.FindAsync(officeId);
        if (office == null || office.CompanyId != ctx.CompanyId) return NotFound();

        var mp = await _db.MasterProduct.FindAsync(request.MasterProductId);
        if (mp == null || mp.CompanyId != ctx.CompanyId)
            return BadRequest(new { error = "Invalid product" });

        var existing = await _db.OfficeProduct
            .FirstOrDefaultAsync(op => op.OfficeId == officeId && op.MasterProductId == request.MasterProductId);
        if (existing != null)
            return Conflict(new { error = "Product already in office list" });

        var item = new OfficeProduct
        {
            Id = Guid.NewGuid(),
            OfficeId = officeId,
            MasterProductId = request.MasterProductId,
            SortOrder = request.SortOrder,
            CreatedById = ctx.UserId,
            CreatedDate = DateTime.UtcNow
        };

        _db.OfficeProduct.Add(item);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Added product {ProductId} to office {OfficeId} list", request.MasterProductId, officeId);

        return Ok(new OfficeProductDto
        {
            Id = item.Id,
            OfficeId = item.OfficeId,
            MasterProductId = item.MasterProductId,
            ProductName = mp.ProductName,
            FriendlyName = mp.FriendlyName,
            PartNumber = mp.PartNumber,
            Category = mp.Category,
            DefaultPrice = mp.DefaultPrice,
            SortOrder = item.SortOrder,
            CreatedById = item.CreatedById,
            CreatedDate = item.CreatedDate
        });
    }

    [HttpDelete("{masterProductId:guid}")]
    public async Task<IActionResult> RemoveProduct(Guid officeId, Guid masterProductId)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var office = await _db.Office.FindAsync(officeId);
        if (office == null || office.CompanyId != ctx.CompanyId) return NotFound();

        var item = await _db.OfficeProduct
            .FirstOrDefaultAsync(op => op.OfficeId == officeId && op.MasterProductId == masterProductId);
        if (item == null) return NotFound();

        _db.OfficeProduct.Remove(item);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private async Task<UserSessionContext?> GetSessionAsync()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email)) return null;
        return await _session.GetOrLoadAsync(email);
    }
}
