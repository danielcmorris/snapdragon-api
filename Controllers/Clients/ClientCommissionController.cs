using System.Data;
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
    public async Task<ActionResult<List<ClientCommissionDto>>> List(Guid clientId)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var client = await _db.Client.FindAsync(clientId);
        if (client == null || client.CompanyId != ctx.CompanyId) return NotFound();

        var results = await CallFunctionAsync(clientId);
        if (results.Count == 0)
        {
            await SeedCommissionsAsync(clientId, ctx);
            results = await CallFunctionAsync(clientId);
        }

        return Ok(results);
    }

    [HttpPut("{commissionTypeId:int}")]
    public async Task<ActionResult> Update(Guid clientId, int commissionTypeId, [FromBody] UpdateClientCommissionRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var client = await _db.Client.FindAsync(clientId);
        if (client == null || client.CompanyId != ctx.CompanyId) return NotFound();

        var commissionType = await _db.CommissionType.FindAsync(commissionTypeId);
        if (commissionType == null) return NotFound();

        var existing = await _db.ClientCommission
            .FirstOrDefaultAsync(c => c.ClientId == clientId && c.CommissionTypeId == commissionTypeId);

        var now = DateTime.UtcNow;
        if (existing != null)
        {
            existing.Rate = request.Rate;
            existing.UpdatedById = ctx.UserId;
            existing.UpdatedDate = now;
        }
        else
        {
            _db.ClientCommission.Add(new ClientCommission
            {
                Id = Guid.NewGuid(),
                ClientId = clientId,
                CommissionTypeId = commissionTypeId,
                Name = commissionType.Name,
                Rate = request.Rate,
                SortOrder = commissionType.SortOrder,
                StatusId = 1,
                CreatedById = ctx.UserId,
                CreatedDate = now,
                UpdatedById = ctx.UserId,
                UpdatedDate = now,
            });
        }

        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("bulk-update")]
    public async Task<ActionResult<List<ClientCommissionDto>>> BulkUpdate(Guid clientId, [FromBody] BulkUpdateClientCommissionsRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var client = await _db.Client.FindAsync(clientId);
        if (client == null || client.CompanyId != ctx.CompanyId) return NotFound();

        var types = await _db.CommissionType
            .Where(ct => ct.StatusId == 1)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var ct in types)
        {
            var existing = await _db.ClientCommission
                .FirstOrDefaultAsync(c => c.ClientId == clientId && c.CommissionTypeId == ct.Id);

            if (existing != null)
            {
                existing.Rate = request.Rate;
                existing.UpdatedById = ctx.UserId;
                existing.UpdatedDate = now;
            }
            else
            {
                _db.ClientCommission.Add(new ClientCommission
                {
                    Id = Guid.NewGuid(),
                    ClientId = clientId,
                    CommissionTypeId = ct.Id,
                    Name = ct.Name,
                    Rate = request.Rate,
                    SortOrder = ct.SortOrder,
                    StatusId = 1,
                    CreatedById = ctx.UserId,
                    CreatedDate = now,
                    UpdatedById = ctx.UserId,
                    UpdatedDate = now,
                });
            }
        }

        await _db.SaveChangesAsync();
        return Ok(await CallFunctionAsync(clientId));
    }

    private async Task<List<ClientCommissionDto>> CallFunctionAsync(Guid clientId)
    {
        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, name, default_percent, rate FROM client_commission_rates(@p0)";
        var param = cmd.CreateParameter();
        param.ParameterName = "p0";
        param.Value = clientId;
        cmd.Parameters.Add(param);

        using var reader = await cmd.ExecuteReaderAsync();
        var results = new List<ClientCommissionDto>();
        while (await reader.ReadAsync())
        {
            results.Add(new ClientCommissionDto
            {
                CommissionTypeId = reader.GetInt32(0),
                Name = reader.GetString(1),
                DefaultPercent = reader.GetDecimal(2),
                Rate = reader.GetDecimal(3),
            });
        }
        return results;
    }

    private async Task SeedCommissionsAsync(Guid clientId, UserSessionContext ctx)
    {
        var types = await _db.CommissionType
            .Where(ct => ct.StatusId == 1)
            .OrderBy(ct => ct.SortOrder)
            .ThenBy(ct => ct.Name)
            .ToListAsync();

        var now = DateTime.UtcNow;
        _db.ClientCommission.AddRange(types.Select(ct => new ClientCommission
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
        }));

        await _db.SaveChangesAsync();
    }

    private async Task<UserSessionContext?> GetSessionAsync()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email)) return null;
        return await _session.GetOrLoadAsync(email);
    }
}
