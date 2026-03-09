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
public class ProductController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<ProductController> _logger;
    private readonly IUserSessionService _session;

    public ProductController(AppDbContext db, ILogger<ProductController> logger, IUserSessionService session)
    {
        _db = db;
        _logger = logger;
        _session = session;
    }

    [HttpGet]
    public async Task<ActionResult<ProductListResponse>> GetProducts(
        [FromQuery] Guid? masterProductId = null,
        [FromQuery] int? statusId = null,
        [FromQuery] bool? isAvailable = null,
        [FromQuery] string? search = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var query = _db.Product.AsQueryable();

        if (masterProductId.HasValue)
            query = query.Where(p => p.MasterProductId == masterProductId.Value);
        if (statusId.HasValue)
            query = query.Where(p => p.StatusId == statusId.Value);
        if (isAvailable.HasValue)
            query = query.Where(p => p.IsAvailable == isAvailable.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => (p.SerialNumber != null && p.SerialNumber.Contains(search)) ||
                (p.Barcode != null && p.Barcode.Contains(search)));

        var totalCount = await query.CountAsync();
        var products = await query
            .OrderBy(p => p.SerialNumber)
            .Skip(skip)
            .Take(take)
            .Select(p => ToDto(p))
            .ToListAsync();

        return Ok(new ProductListResponse { Products = products, TotalCount = totalCount });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetProduct(Guid id)
    {
        var product = await _db.Product.FindAsync(id);
        if (product == null)
            return NotFound();

        return Ok(ToDto(product));
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        var now = DateTime.UtcNow;
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Vid = request.Vid,
            MasterProductId = request.MasterProductId,
            SerialNumber = request.SerialNumber,
            Barcode = request.Barcode,
            Condition = request.Condition,
            Notes = request.Notes,
            IsAvailable = true,
            StatusId = 1,
            CreatedById = ctx.UserId,
            UpdatedById = ctx.UserId,
            CreatedDate = now,
            UpdatedDate = now
        };

        _db.Product.Add(product);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created product {ProductId}", product.Id);

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, ToDto(product));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductDto>> UpdateProduct(Guid id, [FromBody] UpdateProductRequest request)
    {
        var product = await _db.Product.FindAsync(id);
        if (product == null)
            return NotFound();

        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        if (request.Vid.HasValue) product.Vid = request.Vid;
        if (request.SerialNumber != null) product.SerialNumber = request.SerialNumber;
        if (request.Barcode != null) product.Barcode = request.Barcode;
        if (request.Condition != null) product.Condition = request.Condition;
        if (request.Notes != null) product.Notes = request.Notes;
        if (request.IsAvailable.HasValue) product.IsAvailable = request.IsAvailable.Value;
        if (request.StatusId.HasValue) product.StatusId = request.StatusId.Value;

        product.UpdatedById = ctx.UserId;
        product.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated product {ProductId}", product.Id);

        return Ok(ToDto(product));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var product = await _db.Product.FindAsync(id);
        if (product == null)
            return NotFound();

        _db.Product.Remove(product);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted product {ProductId}", id);

        return NoContent();
    }

    private async Task<UserSessionContext?> GetSessionAsync()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email)) return null;
        return await _session.GetOrLoadAsync(email);
    }

    private static ProductDto ToDto(Product p) => new()
    {
        Id = p.Id,
        Vid = p.Vid,
        MasterProductId = p.MasterProductId,
        SerialNumber = p.SerialNumber,
        Barcode = p.Barcode,
        Condition = p.Condition,
        Notes = p.Notes,
        IsAvailable = p.IsAvailable,
        StatusId = p.StatusId,
        CreatedById = p.CreatedById,
        CreatedDate = p.CreatedDate,
        UpdatedById = p.UpdatedById,
        UpdatedDate = p.UpdatedDate
    };
}
