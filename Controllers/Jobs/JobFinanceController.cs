using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnapdragonApi.DTOs;
using SnapdragonApi.Models;
using SnapdragonApi.Services;

namespace SnapdragonApi.Controllers.Jobs;

[ApiController]
[Route("api/job/{jobId:guid}/finance")]
[Authorize]
public class JobFinanceController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IUserSessionService _session;

    public JobFinanceController(AppDbContext db, IUserSessionService session)
    {
        _db = db;
        _session = session;
    }

    /// <summary>
    /// Returns all products across all rooms for a job, flattened, with room name included.
    /// Header rows only (no sub-items in the top-level list; sub-items nested as usual).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<JobFinanceResponse>> GetFinance(Guid jobId)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var job = await _db.Job.FindAsync(jobId);
        if (job == null || job.CompanyId != ctx.CompanyId) return NotFound();

        var rooms = await _db.JobRoom
            .Where(r => r.JobId == jobId)
            .OrderBy(r => r.SortOrder)
            .ToListAsync();

        var roomIds = rooms.Select(r => r.Id).ToList();

        var allProducts = await _db.JobRoomProduct
            .Where(p => roomIds.Contains(p.JobRoomId))
            .Include(p => p.Package)
            .Include(p => p.MasterProduct)
            .OrderBy(p => p.JobRoomId)
            .ThenBy(p => p.SortOrder)
            .ToListAsync();

        var roomMap = rooms.ToDictionary(r => r.Id, r => r.Name ?? "");

        var headers = allProducts.Where(p => p.ParentId == null).ToList();
        var result = headers.Select(h =>
        {
            var base_dto = JobRoomProductController.ToDto(h);
            var dto = new JobFinanceProductDto
            {
                Id = base_dto.Id,
                JobRoomId = base_dto.JobRoomId,
                ParentId = base_dto.ParentId,
                PackageId = base_dto.PackageId,
                PackageName = base_dto.PackageName,
                MasterProductId = base_dto.MasterProductId,
                ProductName = base_dto.ProductName,
                PartNumber = base_dto.PartNumber,
                Quantity = base_dto.Quantity,
                Days = base_dto.Days,
                Price = base_dto.Price,
                DiscountPercent = base_dto.DiscountPercent,
                DiscountFixed = base_dto.DiscountFixed,
                ServiceChargePercent = base_dto.ServiceChargePercent,
                ServiceChargeBeforeDiscount = base_dto.ServiceChargeBeforeDiscount,
                TaxRate = base_dto.TaxRate,
                CommissionPercent = base_dto.CommissionPercent,
                Notes = base_dto.Notes,
                SortOrder = base_dto.SortOrder,
                StatusId = base_dto.StatusId,
                CreatedById = base_dto.CreatedById,
                CreatedDate = base_dto.CreatedDate,
                UpdatedById = base_dto.UpdatedById,
                UpdatedDate = base_dto.UpdatedDate,
                RoomId = h.JobRoomId,
                RoomName = roomMap.TryGetValue(h.JobRoomId, out var rn) ? rn : "",
                SubItems = allProducts
                    .Where(s => s.ParentId == h.Id)
                    .OrderBy(s => s.SortOrder)
                    .Select(s => JobRoomProductController.ToDto(s))
                    .ToList(),
            };
            return dto;
        }).ToList();

        return Ok(new JobFinanceResponse { Products = result });
    }

    /// <summary>
    /// Bulk-updates financial fields (Price, TaxRate, DiscountPercent, ServiceChargePercent, CommissionPercent)
    /// across any set of products belonging to this job.
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> BulkUpdateFinance(Guid jobId, [FromBody] BulkUpdateFinanceRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var job = await _db.Job.FindAsync(jobId);
        if (job == null || job.CompanyId != ctx.CompanyId) return NotFound();

        var productIds = request.Items.Select(i => i.Id).ToList();
        var products = await _db.JobRoomProduct
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        // Validate all belong to this job's rooms
        var roomIds = await _db.JobRoom
            .Where(r => r.JobId == jobId)
            .Select(r => r.Id)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var item in request.Items)
        {
            var product = products.FirstOrDefault(p => p.Id == item.Id);
            if (product == null || !roomIds.Contains(product.JobRoomId)) continue;

            if (item.Price.HasValue) product.Price = item.Price;
            if (item.TaxRate.HasValue) product.TaxRate = item.TaxRate;
            if (item.DiscountPercent.HasValue) product.DiscountPercent = item.DiscountPercent;
            if (item.ServiceChargePercent.HasValue) product.ServiceChargePercent = item.ServiceChargePercent;
            if (item.CommissionPercent.HasValue) product.CommissionPercent = item.CommissionPercent;
            product.UpdatedById = ctx.UserId;
            product.UpdatedDate = now;
        }

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
