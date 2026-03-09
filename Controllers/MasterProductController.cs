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
public class MasterProductController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<MasterProductController> _logger;
    private readonly IUserSessionService _session;
    private readonly IBigQueryService _bigQueryService;

    public MasterProductController(
        AppDbContext db,
        ILogger<MasterProductController> logger,
        IUserSessionService session,
        IBigQueryService bigQueryService)
    {
        _db = db;
        _logger = logger;
        _session = session;
        _bigQueryService = bigQueryService;
    }

    [HttpGet]
    public async Task<ActionResult<MasterProductListResponse>> GetMasterProducts(
        [FromQuery] Guid? companyId = null,
        [FromQuery] int? statusId = null,
        [FromQuery] string? category = null,
        [FromQuery] string? productType = null,
        [FromQuery] string? manufacturer = null,
        [FromQuery] string? search = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var query = _db.MasterProduct.AsQueryable();

        if (companyId.HasValue)
            query = query.Where(m => m.CompanyId == companyId.Value);
        if (statusId.HasValue)
            query = query.Where(m => m.StatusId == statusId.Value);
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(m => m.Category == category);
        if (!string.IsNullOrWhiteSpace(productType))
            query = query.Where(m => m.ProductType == productType);
        if (!string.IsNullOrWhiteSpace(manufacturer))
            query = query.Where(m => m.Manufacturer == manufacturer);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(m => m.ProductName.Contains(search) ||
                (m.PartNumber != null && m.PartNumber.Contains(search)) ||
                (m.ProductId != null && m.ProductId.Contains(search)));

        var totalCount = await query.CountAsync();
        var masterProducts = await query
            .OrderBy(m => m.ProductName)
            .Skip(skip)
            .Take(take)
            .Select(m => ToDto(m))
            .ToListAsync();

        return Ok(new MasterProductListResponse { MasterProducts = masterProducts, TotalCount = totalCount });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MasterProductDto>> GetMasterProduct(Guid id)
    {
        var masterProduct = await _db.MasterProduct.FindAsync(id);
        if (masterProduct == null)
            return NotFound();

        return Ok(ToDto(masterProduct));
    }

    [HttpPost]
    public async Task<ActionResult<MasterProductDto>> CreateMasterProduct([FromBody] CreateMasterProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProductName))
            return BadRequest(new { error = "ProductName is required" });

        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        var now = DateTime.UtcNow;

        var masterProduct = new MasterProduct
        {
            Id = Guid.NewGuid(),
            Vid = request.Vid,
            CompanyId = ctx.CompanyId,
            ProductId = request.ProductId,
            PartNumber = request.PartNumber,
            ProductName = request.ProductName,
            FriendlyName = request.FriendlyName,
            ProductType = request.ProductType,
            DefaultCost = request.DefaultCost,
            DefaultPrice = request.DefaultPrice,
            Status = request.Status,
            PriceLevelA = request.PriceLevelA,
            PriceLevelB = request.PriceLevelB,
            PriceLevelC = request.PriceLevelC,
            PriceLevelD = request.PriceLevelD,
            PriceLevelE = request.PriceLevelE,
            Manufacturer = request.Manufacturer,
            DateAdded = request.DateAdded,
            Taxable = request.Taxable,
            TaxType = request.TaxType,
            Description = request.Description,
            Category = request.Category,
            Subcategory = request.Subcategory,
            UnitOfMeasure = request.UnitOfMeasure,
            StatusId = 1,
            CreatedById = ctx.UserId,
            UpdatedById = ctx.UserId,
            CreatedDate = now,
            UpdatedDate = now
        };

        _db.MasterProduct.Add(masterProduct);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created master product {MasterProductId} - {ProductName}", masterProduct.Id, masterProduct.ProductName);

        return CreatedAtAction(nameof(GetMasterProduct), new { id = masterProduct.Id }, ToDto(masterProduct));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MasterProductDto>> UpdateMasterProduct(Guid id, [FromBody] UpdateMasterProductRequest request)
    {
        var masterProduct = await _db.MasterProduct.FindAsync(id);
        if (masterProduct == null)
            return NotFound();

        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        if (request.Vid.HasValue) masterProduct.Vid = request.Vid;
        if (request.ProductId != null) masterProduct.ProductId = request.ProductId;
        if (request.PartNumber != null) masterProduct.PartNumber = request.PartNumber;
        if (request.ProductName != null) masterProduct.ProductName = request.ProductName;
        if (request.FriendlyName != null) masterProduct.FriendlyName = request.FriendlyName;
        if (request.ProductType != null) masterProduct.ProductType = request.ProductType;
        if (request.DefaultCost.HasValue) masterProduct.DefaultCost = request.DefaultCost;
        if (request.DefaultPrice.HasValue) masterProduct.DefaultPrice = request.DefaultPrice;
        if (request.Status != null) masterProduct.Status = request.Status;
        if (request.PriceLevelA.HasValue) masterProduct.PriceLevelA = request.PriceLevelA;
        if (request.PriceLevelB.HasValue) masterProduct.PriceLevelB = request.PriceLevelB;
        if (request.PriceLevelC.HasValue) masterProduct.PriceLevelC = request.PriceLevelC;
        if (request.PriceLevelD.HasValue) masterProduct.PriceLevelD = request.PriceLevelD;
        if (request.PriceLevelE.HasValue) masterProduct.PriceLevelE = request.PriceLevelE;
        if (request.Manufacturer != null) masterProduct.Manufacturer = request.Manufacturer;
        if (request.DateAdded.HasValue) masterProduct.DateAdded = request.DateAdded;
        if (request.Taxable.HasValue) masterProduct.Taxable = request.Taxable.Value;
        if (request.TaxType != null) masterProduct.TaxType = request.TaxType;
        if (request.Description != null) masterProduct.Description = request.Description;
        if (request.Category != null) masterProduct.Category = request.Category;
        if (request.Subcategory != null) masterProduct.Subcategory = request.Subcategory;
        if (request.UnitOfMeasure != null) masterProduct.UnitOfMeasure = request.UnitOfMeasure;
        if (request.StatusId.HasValue) masterProduct.StatusId = request.StatusId.Value;

        masterProduct.UpdatedById = ctx.UserId;
        masterProduct.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated master product {MasterProductId}", masterProduct.Id);

        return Ok(ToDto(masterProduct));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteMasterProduct(Guid id)
    {
        var masterProduct = await _db.MasterProduct.FindAsync(id);
        if (masterProduct == null)
            return NotFound();

        _db.MasterProduct.Remove(masterProduct);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted master product {MasterProductId}", id);

        return NoContent();
    }

    [HttpPost("sync-to-bigquery")]
    public async Task<IActionResult> SyncToBigQuery()
    {
        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        try
        {
            await _bigQueryService.SyncProductsAsync(ctx.CompanyId);
            return Ok(new { success = true, message = "Products synced to BigQuery successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing products to BigQuery for company {CompanyId}", ctx.CompanyId);
            return StatusCode(500, new { error = "Failed to sync products to BigQuery", message = ex.Message });
        }
    }

    private async Task<UserSessionContext?> GetSessionAsync()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email)) return null;
        return await _session.GetOrLoadAsync(email);
    }

    private static MasterProductDto ToDto(MasterProduct m) => new()
    {
        Id = m.Id,
        Vid = m.Vid,
        CompanyId = m.CompanyId,
        ProductId = m.ProductId,
        PartNumber = m.PartNumber,
        ProductName = m.ProductName,
        FriendlyName = m.FriendlyName,
        ProductType = m.ProductType,
        DefaultCost = m.DefaultCost,
        DefaultPrice = m.DefaultPrice,
        Status = m.Status,
        PriceLevelA = m.PriceLevelA,
        PriceLevelB = m.PriceLevelB,
        PriceLevelC = m.PriceLevelC,
        PriceLevelD = m.PriceLevelD,
        PriceLevelE = m.PriceLevelE,
        Manufacturer = m.Manufacturer,
        DateAdded = m.DateAdded,
        Taxable = m.Taxable,
        TaxType = m.TaxType,
        Description = m.Description,
        Category = m.Category,
        Subcategory = m.Subcategory,
        UnitOfMeasure = m.UnitOfMeasure,
        StatusId = m.StatusId,
        CreatedById = m.CreatedById,
        CreatedDate = m.CreatedDate,
        UpdatedById = m.UpdatedById,
        UpdatedDate = m.UpdatedDate
    };
}
