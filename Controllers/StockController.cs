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
public class StockController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<StockController> _logger;
    private readonly IUserSessionService _session;

    public StockController(AppDbContext db, ILogger<StockController> logger, IUserSessionService session)
    {
        _db = db;
        _logger = logger;
        _session = session;
    }

    [HttpGet]
    public async Task<ActionResult<StockListResponse>> GetStocks(
        [FromQuery] Guid? productId = null,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] string? location = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var query = _db.Stock.AsQueryable();

        if (productId.HasValue)
            query = query.Where(s => s.ProductId == productId.Value);
        if (warehouseId.HasValue)
            query = query.Where(s => s.WarehouseId == warehouseId.Value);
        if (!string.IsNullOrWhiteSpace(location))
            query = query.Where(s => s.Location != null && s.Location.Contains(location));

        var totalCount = await query.CountAsync();
        var stocks = await query
            .OrderBy(s => s.Location)
            .Skip(skip)
            .Take(take)
            .Select(s => ToDto(s))
            .ToListAsync();

        return Ok(new StockListResponse { Stocks = stocks, TotalCount = totalCount });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StockDto>> GetStock(Guid id)
    {
        var stock = await _db.Stock.FindAsync(id);
        if (stock == null)
            return NotFound();

        return Ok(ToDto(stock));
    }

    [HttpPost]
    public async Task<ActionResult<StockDto>> CreateStock([FromBody] CreateStockRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        var now = DateTime.UtcNow;
        var stock = new Stock
        {
            Id = Guid.NewGuid(),
            Vid = request.Vid,
            ProductId = request.ProductId,
            WarehouseId = request.WarehouseId,
            Quantity = request.Quantity,
            ReservedQuantity = request.ReservedQuantity,
            Location = request.Location,
            StatusId = 1,
            CreatedById = ctx.UserId,
            UpdatedById = ctx.UserId,
            CreatedDate = now,
            UpdatedDate = now
        };

        _db.Stock.Add(stock);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created stock {StockId}", stock.Id);

        return CreatedAtAction(nameof(GetStock), new { id = stock.Id }, ToDto(stock));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StockDto>> UpdateStock(Guid id, [FromBody] UpdateStockRequest request)
    {
        var stock = await _db.Stock.FindAsync(id);
        if (stock == null)
            return NotFound();

        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        if (request.Vid.HasValue) stock.Vid = request.Vid;
        if (request.WarehouseId.HasValue) stock.WarehouseId = request.WarehouseId;
        if (request.Quantity.HasValue) stock.Quantity = request.Quantity.Value;
        if (request.ReservedQuantity.HasValue) stock.ReservedQuantity = request.ReservedQuantity.Value;
        if (request.Location != null) stock.Location = request.Location;
        if (request.LastCountedAt.HasValue) stock.LastCountedAt = request.LastCountedAt;

        stock.UpdatedById = ctx.UserId;
        stock.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated stock {StockId}", stock.Id);

        return Ok(ToDto(stock));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteStock(Guid id)
    {
        var stock = await _db.Stock.FindAsync(id);
        if (stock == null)
            return NotFound();

        _db.Stock.Remove(stock);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted stock {StockId}", id);

        return NoContent();
    }

    private async Task<UserSessionContext?> GetSessionAsync()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email)) return null;
        return await _session.GetOrLoadAsync(email);
    }

    private static StockDto ToDto(Stock s) => new()
    {
        Id = s.Id,
        Vid = s.Vid,
        ProductId = s.ProductId,
        WarehouseId = s.WarehouseId,
        Quantity = s.Quantity,
        ReservedQuantity = s.ReservedQuantity,
        Location = s.Location,
        LastCountedAt = s.LastCountedAt,
        StatusId = s.StatusId,
        CreatedById = s.CreatedById,
        CreatedDate = s.CreatedDate,
        UpdatedById = s.UpdatedById,
        UpdatedDate = s.UpdatedDate
    };
}
