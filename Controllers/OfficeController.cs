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
public class OfficeController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<OfficeController> _logger;
    private readonly IUserSessionService _session;

    public OfficeController(AppDbContext db, ILogger<OfficeController> logger, IUserSessionService session)
    {
        _db = db;
        _logger = logger;
        _session = session;
    }

    [HttpGet]
    public async Task<ActionResult<OfficeListResponse>> GetOffices(
        [FromQuery] Guid? companyId = null,
        [FromQuery] int? statusId = null,
        [FromQuery] string? search = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        if (ctx.UserLevel < 2)
            return StatusCode(403, new { error = "Insufficient permissions. Manager role or higher required." });

        var query = _db.Office.Where(o => o.CompanyId == ctx.CompanyId);

        if (statusId.HasValue)
            query = query.Where(o => o.StatusId == statusId.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(o => o.Name.Contains(search));

        var totalCount = await query.CountAsync();
        var offices = await query
            .OrderBy(o => o.SortOrder)
            .ThenBy(o => o.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        var warehouseMap = await _db.OfficeWarehouse
            .Where(ow => offices.Select(o => o.Id).Contains(ow.OfficeId))
            .GroupBy(ow => ow.OfficeId)
            .ToDictionaryAsync(g => g.Key, g => g.Select(ow => ow.WarehouseId).ToList());

        var dtos = offices.Select(o => ToDto(o, warehouseMap.GetValueOrDefault(o.Id))).ToList();

        return Ok(new OfficeListResponse { Offices = dtos, TotalCount = totalCount });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OfficeDto>> GetOffice(Guid id)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        if (ctx.UserLevel < 2)
            return StatusCode(403, new { error = "Insufficient permissions. Manager role or higher required." });

        var office = await _db.Office.FindAsync(id);
        if (office == null || office.CompanyId != ctx.CompanyId)
            return NotFound();

        var warehouseIds = await _db.OfficeWarehouse
            .Where(ow => ow.OfficeId == id)
            .Select(ow => ow.WarehouseId)
            .ToListAsync();

        return Ok(ToDto(office, warehouseIds));
    }

    [HttpPost]
    public async Task<ActionResult<OfficeDto>> CreateOffice([FromBody] CreateOfficeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Name is required" });

        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        if (ctx.UserLevel < 2)
            return StatusCode(403, new { error = "Insufficient permissions. Manager role or higher required." });

        var now = DateTime.UtcNow;
        var office = new Office
        {
            Id = Guid.NewGuid(),
            Vid = request.Vid,
            CompanyId = ctx.CompanyId,
            OfficeNumber = request.OfficeNumber?.Trim(),
            Name = request.Name.Trim(),
            ContactName = request.ContactName?.Trim(),
            Phone = request.Phone?.Trim(),
            Fax = request.Fax?.Trim(),
            Email = request.Email?.Trim(),
            Address = request.Address?.Trim(),
            Address2 = request.Address2?.Trim(),
            Address3 = request.Address3?.Trim(),
            City = request.City?.Trim(),
            State = request.State?.Trim(),
            ZipCode = request.ZipCode?.Trim(),
            Country = request.Country?.Trim(),
            SortOrder = request.SortOrder,
            TaxRegionId = request.TaxRegionId,
            DefaultWarehouseId = request.DefaultWarehouseId,
            ShowOnContactList = request.ShowOnContactList,
            ServiceChargeLabel = request.ServiceChargeLabel?.Trim(),
            DiscountThreshold = request.DiscountThreshold,
            FeatureFlags = request.FeatureFlags?.Trim(),
            QuickbooksClass = request.QuickbooksClass?.Trim(),
            NsLocationId = request.NsLocationId,
            NsEntityId = request.NsEntityId,
            IsHeadquarters = request.IsHeadquarters,
            StatusId = 1,
            CreatedById = ctx.UserId,
            UpdatedById = ctx.UserId,
            CreatedDate = now,
            UpdatedDate = now
        };

        _db.Office.Add(office);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created office {OfficeId} - {OfficeName}", office.Id, office.Name);

        return CreatedAtAction(nameof(GetOffice), new { id = office.Id }, ToDto(office, null));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<OfficeDto>> UpdateOffice(Guid id, [FromBody] UpdateOfficeRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        if (ctx.UserLevel < 2)
            return StatusCode(403, new { error = "Insufficient permissions. Manager role or higher required." });

        var office = await _db.Office.FindAsync(id);
        if (office == null || office.CompanyId != ctx.CompanyId)
            return NotFound();

        if (request.Vid.HasValue) office.Vid = request.Vid;
        if (request.OfficeNumber != null) office.OfficeNumber = string.IsNullOrWhiteSpace(request.OfficeNumber) ? null : request.OfficeNumber.Trim();
        if (request.Name != null) office.Name = request.Name.Trim();
        if (request.ContactName != null) office.ContactName = string.IsNullOrWhiteSpace(request.ContactName) ? null : request.ContactName.Trim();
        if (request.Phone != null) office.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        if (request.Fax != null) office.Fax = string.IsNullOrWhiteSpace(request.Fax) ? null : request.Fax.Trim();
        if (request.Email != null) office.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        if (request.Address != null) office.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        if (request.Address2 != null) office.Address2 = string.IsNullOrWhiteSpace(request.Address2) ? null : request.Address2.Trim();
        if (request.Address3 != null) office.Address3 = string.IsNullOrWhiteSpace(request.Address3) ? null : request.Address3.Trim();
        if (request.City != null) office.City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim();
        if (request.State != null) office.State = string.IsNullOrWhiteSpace(request.State) ? null : request.State.Trim();
        if (request.ZipCode != null) office.ZipCode = string.IsNullOrWhiteSpace(request.ZipCode) ? null : request.ZipCode.Trim();
        if (request.Country != null) office.Country = string.IsNullOrWhiteSpace(request.Country) ? null : request.Country.Trim();
        if (request.SortOrder.HasValue) office.SortOrder = request.SortOrder.Value;
        if (request.ClearTaxRegion == true) office.TaxRegionId = null;
        else if (request.TaxRegionId.HasValue) office.TaxRegionId = request.TaxRegionId.Value;
        if (request.ClearDefaultWarehouse == true) office.DefaultWarehouseId = null;
        else if (request.DefaultWarehouseId.HasValue) office.DefaultWarehouseId = request.DefaultWarehouseId.Value;
        if (request.ShowOnContactList.HasValue) office.ShowOnContactList = request.ShowOnContactList.Value;
        if (request.ServiceChargeLabel != null) office.ServiceChargeLabel = string.IsNullOrWhiteSpace(request.ServiceChargeLabel) ? null : request.ServiceChargeLabel.Trim();
        if (request.DiscountThreshold.HasValue) office.DiscountThreshold = request.DiscountThreshold.Value;
        if (request.FeatureFlags != null) office.FeatureFlags = string.IsNullOrWhiteSpace(request.FeatureFlags) ? null : request.FeatureFlags.Trim();
        if (request.QuickbooksClass != null) office.QuickbooksClass = string.IsNullOrWhiteSpace(request.QuickbooksClass) ? null : request.QuickbooksClass.Trim();
        if (request.NsLocationId.HasValue) office.NsLocationId = request.NsLocationId.Value;
        if (request.NsEntityId.HasValue) office.NsEntityId = request.NsEntityId.Value;
        if (request.IsHeadquarters.HasValue) office.IsHeadquarters = request.IsHeadquarters.Value;
        if (request.StatusId.HasValue) office.StatusId = request.StatusId.Value;

        office.UpdatedById = ctx.UserId;
        office.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated office {OfficeId}", office.Id);

        var warehouseIds = await _db.OfficeWarehouse
            .Where(ow => ow.OfficeId == id)
            .Select(ow => ow.WarehouseId)
            .ToListAsync();

        return Ok(ToDto(office, warehouseIds));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteOffice(Guid id)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        if (ctx.UserLevel < 2)
            return StatusCode(403, new { error = "Insufficient permissions. Manager role or higher required." });

        var office = await _db.Office.FindAsync(id);
        if (office == null || office.CompanyId != ctx.CompanyId)
            return NotFound();

        office.StatusId = 2;
        office.UpdatedById = ctx.UserId;
        office.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Soft-deleted office {OfficeId}", id);

        return NoContent();
    }

    // PUT api/office/{id}/warehouses — replace full set of warehouse access for an office
    [HttpPut("{id:guid}/warehouses")]
    public async Task<ActionResult<OfficeDto>> SetWarehouses(Guid id, [FromBody] SetOfficeWarehousesRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        if (ctx.UserLevel < 2)
            return StatusCode(403, new { error = "Insufficient permissions." });

        var office = await _db.Office.FindAsync(id);
        if (office == null || office.CompanyId != ctx.CompanyId)
            return NotFound();

        // Validate warehouse IDs belong to this company
        var validIds = await _db.Warehouse
            .Where(w => w.CompanyId == ctx.CompanyId && request.WarehouseIds.Contains(w.Id))
            .Select(w => w.Id)
            .ToListAsync();

        // Remove all existing
        var existing = await _db.OfficeWarehouse.Where(ow => ow.OfficeId == id).ToListAsync();
        _db.OfficeWarehouse.RemoveRange(existing);

        // Add new
        foreach (var wid in validIds)
        {
            _db.OfficeWarehouse.Add(new OfficeWarehouse
            {
                OfficeId = id,
                WarehouseId = wid,
                CreatedDate = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        return Ok(ToDto(office, validIds));
    }

    private async Task<UserSessionContext?> GetSessionAsync()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email)) return null;
        return await _session.GetOrLoadAsync(email);
    }

    private static OfficeDto ToDto(Office o, List<Guid>? warehouseIds) => new()
    {
        Id = o.Id,
        Vid = o.Vid,
        CompanyId = o.CompanyId,
        OfficeNumber = o.OfficeNumber,
        Name = o.Name,
        ContactName = o.ContactName,
        Phone = o.Phone,
        Fax = o.Fax,
        Email = o.Email,
        Address = o.Address,
        Address2 = o.Address2,
        Address3 = o.Address3,
        City = o.City,
        State = o.State,
        ZipCode = o.ZipCode,
        Country = o.Country,
        SortOrder = o.SortOrder,
        TaxRegionId = o.TaxRegionId,
        DefaultWarehouseId = o.DefaultWarehouseId,
        ShowOnContactList = o.ShowOnContactList,
        ServiceChargeLabel = o.ServiceChargeLabel,
        DiscountThreshold = o.DiscountThreshold,
        FeatureFlags = o.FeatureFlags,
        QuickbooksClass = o.QuickbooksClass,
        NsLocationId = o.NsLocationId,
        NsEntityId = o.NsEntityId,
        IsHeadquarters = o.IsHeadquarters,
        StatusId = o.StatusId,
        WarehouseIds = warehouseIds ?? new(),
        CreatedById = o.CreatedById,
        CreatedDate = o.CreatedDate,
        UpdatedById = o.UpdatedById,
        UpdatedDate = o.UpdatedDate
    };
}
