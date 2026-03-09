using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnapdragonApi.DTOs;
using SnapdragonApi.Models;
using SnapdragonApi.Services;

namespace SnapdragonApi.Controllers.Clients;

[ApiController]
[Route("api/client/{clientId:guid}/contact")]
[Authorize]
public class ClientContactController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IUserSessionService _session;

    public ClientContactController(AppDbContext db, IUserSessionService session)
    {
        _db = db;
        _session = session;
    }

    [HttpGet]
    public async Task<ActionResult<ClientContactListResponse>> List(Guid clientId)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var client = await _db.Client.FindAsync(clientId);
        if (client == null || client.CompanyId != ctx.CompanyId) return NotFound();

        var items = await _db.ClientContact
            .Where(c => c.ClientId == clientId && c.StatusId == 1)
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .ToListAsync();

        return Ok(new ClientContactListResponse { Items = items.Select(ToDto).ToList() });
    }

    [HttpPost]
    public async Task<ActionResult<ClientContactDto>> Create(Guid clientId, [FromBody] CreateClientContactRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var client = await _db.Client.FindAsync(clientId);
        if (client == null || client.CompanyId != ctx.CompanyId) return NotFound();

        var contact = new ClientContact
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Notes = request.Notes,
            StatusId = 1,
            CreatedById = ctx.UserId,
            CreatedDate = DateTime.UtcNow,
            UpdatedById = ctx.UserId,
            UpdatedDate = DateTime.UtcNow,
        };

        _db.ClientContact.Add(contact);
        await _db.SaveChangesAsync();

        return Ok(ToDto(contact));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ClientContactDto>> Update(Guid clientId, Guid id, [FromBody] UpdateClientContactRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var client = await _db.Client.FindAsync(clientId);
        if (client == null || client.CompanyId != ctx.CompanyId) return NotFound();

        var contact = await _db.ClientContact
            .FirstOrDefaultAsync(c => c.Id == id && c.ClientId == clientId);
        if (contact == null) return NotFound();

        if (request.FirstName != null) contact.FirstName = request.FirstName;
        if (request.LastName != null) contact.LastName = request.LastName;
        if (request.Email != null) contact.Email = request.Email;
        if (request.Phone != null) contact.Phone = request.Phone;
        if (request.Notes != null) contact.Notes = request.Notes;
        if (request.StatusId.HasValue) contact.StatusId = request.StatusId.Value;
        contact.UpdatedById = ctx.UserId;
        contact.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ToDto(contact));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid clientId, Guid id)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null) return Unauthorized(new { error = "Unable to identify user" });
        if (ctx.UserLevel < 2) return StatusCode(403, new { error = "Insufficient permissions" });

        var client = await _db.Client.FindAsync(clientId);
        if (client == null || client.CompanyId != ctx.CompanyId) return NotFound();

        var contact = await _db.ClientContact
            .FirstOrDefaultAsync(c => c.Id == id && c.ClientId == clientId);
        if (contact == null) return NotFound();

        contact.StatusId = 2;
        contact.UpdatedById = ctx.UserId;
        contact.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static ClientContactDto ToDto(ClientContact c) => new()
    {
        Id = c.Id,
        ClientId = c.ClientId,
        FirstName = c.FirstName,
        LastName = c.LastName,
        Email = c.Email,
        Phone = c.Phone,
        Notes = c.Notes,
        StatusId = c.StatusId,
        CreatedDate = c.CreatedDate,
        UpdatedDate = c.UpdatedDate,
    };

    private async Task<UserSessionContext?> GetSessionAsync()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email)) return null;
        return await _session.GetOrLoadAsync(email);
    }
}
