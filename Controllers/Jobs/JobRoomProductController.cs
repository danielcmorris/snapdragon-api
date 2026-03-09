using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnapdragonApi.DTOs;
using SnapdragonApi.Models;
using SnapdragonApi.Services;

namespace SnapdragonApi.Controllers.Jobs;

[ApiController]
[Route("api/job/{jobId:guid}/room/{roomId:guid}/product")]
[Authorize]
public class JobRoomProductController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<JobRoomProductController> _logger;
    private readonly IUserSessionService _session;

    public JobRoomProductController(AppDbContext db, ILogger<JobRoomProductController> logger, IUserSessionService session)
    {
        _db = db;
        _logger = logger;
        _session = session;
    }

    [HttpGet]
    public async Task<ActionResult<JobRoomProductListResponse>> GetProducts(Guid jobId, Guid roomId)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        if (!await ValidateAccess(jobId, roomId, ctx.CompanyId)) return NotFound();

        var allProducts = await _db.JobRoomProduct
            .Where(p => p.JobRoomId == roomId)
            .Include(p => p.Package)
            .Include(p => p.MasterProduct)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();

        var headers = allProducts.Where(p => p.ParentId == null).ToList();
        var result = headers.Select(h =>
        {
            var dto = ToDto(h);
            dto.SubItems = allProducts
                .Where(s => s.ParentId == h.Id)
                .OrderBy(s => s.SortOrder)
                .Select(s => ToDto(s))
                .ToList();
            return dto;
        }).ToList();

        return Ok(new JobRoomProductListResponse { Products = result, TotalCount = result.Count });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobRoomProductDto>> GetProduct(Guid jobId, Guid roomId, Guid id)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        if (!await ValidateAccess(jobId, roomId, ctx.CompanyId)) return NotFound();

        var product = await _db.JobRoomProduct
            .Include(p => p.Package)
            .Include(p => p.MasterProduct)
            .FirstOrDefaultAsync(p => p.Id == id && p.JobRoomId == roomId);

        if (product == null) return NotFound();

        var dto = ToDto(product);
        if (product.ParentId == null)
        {
            dto.SubItems = await _db.JobRoomProduct
                .Where(s => s.ParentId == id)
                .Include(s => s.MasterProduct)
                .OrderBy(s => s.SortOrder)
                .Select(s => ToDto(s))
                .ToListAsync();
        }

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<JobRoomProductDto>> CreateProduct(Guid jobId, Guid roomId, [FromBody] CreateJobRoomProductRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        if (!await ValidateAccess(jobId, roomId, ctx.CompanyId)) return NotFound();

        if (request.PackageId == null && request.MasterProductId == null)
            return BadRequest(new { error = "Either packageId or masterProductId is required" });

        var now = DateTime.UtcNow;

        // Package expansion: create header + sub-rows atomically
        if (request.PackageId.HasValue)
        {
            var pkg = await _db.Package
                .Include(p => p.Company)
                .FirstOrDefaultAsync(p => p.Id == request.PackageId.Value);

            if (pkg == null || pkg.CompanyId != ctx.CompanyId)
                return BadRequest(new { error = "Invalid package" });

            var packageItems = await _db.PackageProduct
                .Where(pp => pp.PackageId == request.PackageId.Value)
                .Include(pp => pp.MasterProduct)
                .OrderBy(pp => pp.SortOrder)
                .ToListAsync();

            // Header row
            var header = new JobRoomProduct
            {
                Id = Guid.NewGuid(),
                JobRoomId = roomId,
                PackageId = request.PackageId,
                Quantity = request.Quantity,
                Price = request.Price ?? pkg.Price,
                DiscountPercent = request.DiscountPercent,
                DiscountFixed = request.DiscountFixed,
                ServiceChargePercent = request.ServiceChargePercent,
                ServiceChargeBeforeDiscount = request.ServiceChargeBeforeDiscount,
                Notes = request.Notes,
                SortOrder = request.SortOrder,
                StatusId = 1,
                CreatedById = ctx.UserId,
                UpdatedById = ctx.UserId,
                CreatedDate = now,
                UpdatedDate = now
            };
            _db.JobRoomProduct.Add(header);

            // Sub-item rows
            for (int i = 0; i < packageItems.Count; i++)
            {
                var item = packageItems[i];
                var sub = new JobRoomProduct
                {
                    Id = Guid.NewGuid(),
                    JobRoomId = roomId,
                    ParentId = header.Id,
                    MasterProductId = item.MasterProductId,
                    Quantity = item.Quantity,
                    SortOrder = i,
                    StatusId = 1,
                    CreatedById = ctx.UserId,
                    UpdatedById = ctx.UserId,
                    CreatedDate = now,
                    UpdatedDate = now
                };
                _db.JobRoomProduct.Add(sub);
            }

            await _db.SaveChangesAsync();

            // Reload header with navigation props
            var saved = await _db.JobRoomProduct
                .Include(p => p.Package)
                .FirstAsync(p => p.Id == header.Id);

            var headerDto = ToDto(saved);
            headerDto.SubItems = await _db.JobRoomProduct
                .Where(s => s.ParentId == header.Id)
                .Include(s => s.MasterProduct)
                .OrderBy(s => s.SortOrder)
                .Select(s => ToDto(s))
                .ToListAsync();

            _logger.LogInformation("Expanded package {PackageId} into room {RoomId}", request.PackageId, roomId);

            return CreatedAtAction(nameof(GetProduct), new { jobId, roomId, id = header.Id }, headerDto);
        }

        // Individual product
        var mp = await _db.MasterProduct.FindAsync(request.MasterProductId!.Value);
        if (mp == null || mp.CompanyId != ctx.CompanyId)
            return BadRequest(new { error = "Invalid product" });

        var product = new JobRoomProduct
        {
            Id = Guid.NewGuid(),
            JobRoomId = roomId,
            MasterProductId = request.MasterProductId,
            Quantity = request.Quantity,
            Days = request.Days,
            Price = request.Price,
            DiscountPercent = request.DiscountPercent,
            DiscountFixed = request.DiscountFixed,
            ServiceChargePercent = request.ServiceChargePercent,
            ServiceChargeBeforeDiscount = request.ServiceChargeBeforeDiscount,
            Notes = request.Notes,
            SortOrder = request.SortOrder,
            StatusId = 1,
            CreatedById = ctx.UserId,
            UpdatedById = ctx.UserId,
            CreatedDate = now,
            UpdatedDate = now
        };

        _db.JobRoomProduct.Add(product);
        await _db.SaveChangesAsync();

        var savedProduct = await _db.JobRoomProduct
            .Include(p => p.MasterProduct)
            .FirstAsync(p => p.Id == product.Id);

        _logger.LogInformation("Added product {ProductId} to room {RoomId}", product.Id, roomId);

        return CreatedAtAction(nameof(GetProduct), new { jobId, roomId, id = product.Id }, ToDto(savedProduct));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<JobRoomProductDto>> UpdateProduct(Guid jobId, Guid roomId, Guid id, [FromBody] UpdateJobRoomProductRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        if (!await ValidateAccess(jobId, roomId, ctx.CompanyId)) return NotFound();

        var product = await _db.JobRoomProduct.FindAsync(id);
        if (product == null || product.JobRoomId != roomId) return NotFound();

        if (request.Quantity.HasValue) product.Quantity = request.Quantity.Value;
        if (request.Days.HasValue) product.Days = request.Days;
        if (request.Price.HasValue) product.Price = request.Price;
        if (request.DiscountPercent.HasValue) product.DiscountPercent = request.DiscountPercent;
        if (request.DiscountFixed.HasValue) product.DiscountFixed = request.DiscountFixed;
        if (request.ServiceChargePercent.HasValue) product.ServiceChargePercent = request.ServiceChargePercent;
        if (request.ServiceChargeBeforeDiscount.HasValue) product.ServiceChargeBeforeDiscount = request.ServiceChargeBeforeDiscount.Value;
        if (request.Notes != null) product.Notes = request.Notes;
        if (request.TaxRate.HasValue) product.TaxRate = request.TaxRate;
        if (request.CommissionPercent.HasValue) product.CommissionPercent = request.CommissionPercent;
        if (request.SortOrder.HasValue) product.SortOrder = request.SortOrder.Value;
        if (request.StatusId.HasValue) product.StatusId = request.StatusId.Value;

        product.UpdatedById = ctx.UserId;
        product.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var saved = await _db.JobRoomProduct
            .Include(p => p.Package)
            .Include(p => p.MasterProduct)
            .FirstAsync(p => p.Id == id);

        return Ok(ToDto(saved));
    }

    /// <summary>
    /// Reorders all header products in a room.
    /// Accepts an ordered list of product IDs; assigns SortOrder 0, 1, 2… in that order.
    /// Sub-items (package children) are not reordered by this endpoint.
    /// </summary>
    [HttpPost("reorder")]
    public async Task<IActionResult> ReorderProducts(Guid jobId, Guid roomId, [FromBody] ReorderJobRoomProductsRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        if (!await ValidateAccess(jobId, roomId, ctx.CompanyId)) return NotFound();

        var products = await _db.JobRoomProduct
            .Where(p => p.JobRoomId == roomId && p.ParentId == null)
            .ToListAsync();

        var now = DateTime.UtcNow;
        for (int i = 0; i < request.ProductIds.Count; i++)
        {
            var product = products.FirstOrDefault(p => p.Id == request.ProductIds[i]);
            if (product == null) continue;
            product.SortOrder = i;
            product.UpdatedById = ctx.UserId;
            product.UpdatedDate = now;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Moves a header product (and its sub-items) to a different room, inserting at TargetIndex.
    /// </summary>
    [HttpPost("{id:guid}/move")]
    public async Task<IActionResult> MoveProduct(Guid jobId, Guid roomId, Guid id, [FromBody] MoveJobRoomProductRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        if (!await ValidateAccess(jobId, roomId, ctx.CompanyId)) return NotFound();

        // Validate target room belongs to same job
        var targetRoom = await _db.JobRoom.FindAsync(request.TargetRoomId);
        if (targetRoom == null || targetRoom.JobId != jobId) return BadRequest(new { error = "Invalid target room" });

        var product = await _db.JobRoomProduct.FindAsync(id);
        if (product == null || product.JobRoomId != roomId || product.ParentId != null)
            return NotFound();

        var now = DateTime.UtcNow;

        // Shift existing items in target room at or beyond TargetIndex up by 1
        var targetItems = await _db.JobRoomProduct
            .Where(p => p.JobRoomId == request.TargetRoomId && p.ParentId == null && p.SortOrder >= request.TargetIndex)
            .ToListAsync();
        foreach (var t in targetItems)
        {
            t.SortOrder += 1;
            t.UpdatedDate = now;
        }

        // Move header
        product.JobRoomId = request.TargetRoomId;
        product.SortOrder = request.TargetIndex;
        product.UpdatedById = ctx.UserId;
        product.UpdatedDate = now;

        // Move sub-items
        var subItems = await _db.JobRoomProduct.Where(p => p.ParentId == id).ToListAsync();
        foreach (var sub in subItems)
        {
            sub.JobRoomId = request.TargetRoomId;
            sub.UpdatedDate = now;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProduct(Guid jobId, Guid roomId, Guid id)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        if (!await ValidateAccess(jobId, roomId, ctx.CompanyId)) return NotFound();

        var product = await _db.JobRoomProduct.FindAsync(id);
        if (product == null || product.JobRoomId != roomId) return NotFound();

        // If package header, remove all sub-items
        if (product.PackageId != null && product.ParentId == null)
        {
            var subItems = await _db.JobRoomProduct.Where(p => p.ParentId == id).ToListAsync();
            _db.JobRoomProduct.RemoveRange(subItems);
        }

        _db.JobRoomProduct.Remove(product);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted job room product {ProductId} from room {RoomId}", id, roomId);

        return NoContent();
    }

    private async Task<bool> ValidateAccess(Guid jobId, Guid roomId, Guid companyId)
    {
        var job = await _db.Job.FindAsync(jobId);
        if (job == null || job.CompanyId != companyId) return false;
        var room = await _db.JobRoom.FindAsync(roomId);
        if (room == null || room.JobId != jobId) return false;
        return true;
    }

    private async Task<UserSessionContext?> GetSessionAsync()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email)) return null;
        return await _session.GetOrLoadAsync(email);
    }

    internal static JobRoomProductDto ToDto(JobRoomProduct p) => new()
    {
        Id = p.Id,
        JobRoomId = p.JobRoomId,
        ParentId = p.ParentId,
        PackageId = p.PackageId,
        PackageName = p.Package?.Name,
        MasterProductId = p.MasterProductId,
        ProductName = p.MasterProduct?.ProductName,
        PartNumber = p.MasterProduct?.PartNumber,
        Quantity = p.Quantity,
        Days = p.Days,
        Price = p.Price,
        DiscountPercent = p.DiscountPercent,
        DiscountFixed = p.DiscountFixed,
        ServiceChargePercent = p.ServiceChargePercent,
        ServiceChargeBeforeDiscount = p.ServiceChargeBeforeDiscount,
        TaxRate = p.TaxRate,
        CommissionPercent = p.CommissionPercent,
        Notes = p.Notes,
        SortOrder = p.SortOrder,
        StatusId = p.StatusId,
        CreatedById = p.CreatedById,
        CreatedDate = p.CreatedDate,
        UpdatedById = p.UpdatedById,
        UpdatedDate = p.UpdatedDate
    };
}
