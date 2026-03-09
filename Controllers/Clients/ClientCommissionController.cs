using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnapdragonApi.DTOs;
using SnapdragonApi.Models;
using SnapdragonApi.Services;

namespace SnapdragonApi.Controllers.Clients;

[ApiController]
[Route("api/client/{clientId:guid}/commission")]
[Authorize]
public class ClientCommissionController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IUserSessionService _session;

    public ClientCommissionController(AppDbContext db, IUserSessionService session)
    {
        _db = db;
        _session = session;
    }

    [HttpGet]
    public async Task<ActionResult<ClientCommissionListResponse>> List(Guid clientId)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var client = await _db.Client.FindAsync(clientId);
        if (client == null || client.CompanyId != ctx.CompanyId) return NotFound();

        var commissions = await _db.ClientCommission
            .Where(c => c.ClientId == clientId && c.StatusId == 1)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();

        if (commissions.Count == 0)
        {
            // Seed from commission_type table (company-wide defaults)
            var types = await _db.CommissionType
                .Where(ct => ct.StatusId == 1)
                .OrderBy(ct => ct.SortOrder)
                .ThenBy(ct => ct.Name)
                .ToListAsync();

            var now = DateTime.UtcNow;
            var defaults = types.Select(ct => new ClientCommission
            {
                Id = Guid.NewGuid(),
                ClientId = clientId,
                CommissionTypeId = ct.Id,
                Name = ct.Name,
                Rate = ct.DefaultPercent,
                SortOrder = ct.SortOrder,
                StatusId = 1,
                CreatedById = ctx.UserId,
                CreatedDate = now,
                UpdatedById = ctx.UserId,
                UpdatedDate = now,
            }).ToList();

            _db.ClientCommission.AddRange(defaults);
            await _db.SaveChangesAsync();

            commissions = defaults;
        }

        return Ok(new ClientCommissionListResponse { Items = commissions.Select(ToDto).ToList() });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ClientCommissionDto>> Update(Guid clientId, Guid id, [FromBody] UpdateClientCommissionRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var client = await _db.Client.FindAsync(clientId);
        if (client == null || client.CompanyId != ctx.CompanyId) return NotFound();

        var commission = await _db.ClientCommission
            .FirstOrDefaultAsync(c => c.Id == id && c.ClientId == clientId);
        if (commission == null) return NotFound();

        commission.Rate = request.Rate;
        commission.UpdatedById = ctx.UserId;
        commission.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ToDto(commission));
    }

    [HttpPost("bulk-update")]
    public async Task<ActionResult<ClientCommissionListResponse>> BulkUpdate(Guid clientId, [FromBody] BulkUpdateClientCommissionsRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var client = await _db.Client.FindAsync(clientId);
        if (client == null || client.CompanyId != ctx.CompanyId) return NotFound();

        var commissions = await _db.ClientCommission
            .Where(c => c.ClientId == clientId && c.StatusId == 1)
            .ToListAsync();

        foreach (var commission in commissions)
        {
            commission.Rate = request.Rate;
            commission.UpdatedById = ctx.UserId;
            commission.UpdatedDate = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        var ordered = commissions.OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToList();
        return Ok(new ClientCommissionListResponse { Items = ordered.Select(ToDto).ToList() });
    }

    private static ClientCommissionDto ToDto(ClientCommission c) => new()
    {
        Id = c.Id,
        ClientId = c.ClientId,
        Name = c.Name,
        Rate = c.Rate,
        SortOrder = c.SortOrder,
    };

    private async Task<UserSessionContext?> GetSessionAsync()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email)) return null;
        return await _session.GetOrLoadAsync(email);
    }
}
