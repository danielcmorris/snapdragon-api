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
public class CompanyController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<CompanyController> _logger;
    private readonly IUserSessionService _session;

    public CompanyController(AppDbContext db, ILogger<CompanyController> logger, IUserSessionService session)
    {
        _db = db;
        _logger = logger;
        _session = session;
    }

    [HttpGet]
    public async Task<ActionResult<CompanyListResponse>> GetCompanies(
        [FromQuery] int? statusId = null,
        [FromQuery] string? search = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var query = _db.Company.AsQueryable();

        if (statusId.HasValue)
            query = query.Where(c => c.StatusId == statusId.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Name.Contains(search) || (c.Email != null && c.Email.Contains(search)));

        var totalCount = await query.CountAsync();
        var companies = await query
            .OrderBy(c => c.Name)
            .Skip(skip)
            .Take(take)
            .Select(c => ToDto(c))
            .ToListAsync();

        return Ok(new CompanyListResponse { Companies = companies, TotalCount = totalCount });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CompanyDto>> GetCompany(Guid id)
    {
        var company = await _db.Company.FindAsync(id);
        if (company == null)
            return NotFound();

        return Ok(ToDto(company));
    }

    [HttpPost]
    public async Task<ActionResult<CompanyDto>> CreateCompany([FromBody] CreateCompanyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Name is required" });

        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        var now = DateTime.UtcNow;
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Vid = request.Vid,
            Name = request.Name,
            Address = request.Address,
            City = request.City,
            State = request.State,
            ZipCode = request.ZipCode,
            Country = request.Country,
            Phone = request.Phone,
            Email = request.Email,
            Website = request.Website,
            Notes = request.Notes,
            StatusId = 1,
            CreatedById = ctx.UserId,
            UpdatedById = ctx.UserId,
            CreatedDate = now,
            UpdatedDate = now
        };

        _db.Company.Add(company);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created company {CompanyId} - {CompanyName}", company.Id, company.Name);

        return CreatedAtAction(nameof(GetCompany), new { id = company.Id }, ToDto(company));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CompanyDto>> UpdateCompany(Guid id, [FromBody] UpdateCompanyRequest request)
    {
        var company = await _db.Company.FindAsync(id);
        if (company == null)
            return NotFound();

        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        if (request.Vid.HasValue) company.Vid = request.Vid;
        if (request.Name != null) company.Name = request.Name;
        if (request.Address != null) company.Address = request.Address;
        if (request.City != null) company.City = request.City;
        if (request.State != null) company.State = request.State;
        if (request.ZipCode != null) company.ZipCode = request.ZipCode;
        if (request.Country != null) company.Country = request.Country;
        if (request.Phone != null) company.Phone = request.Phone;
        if (request.Email != null) company.Email = request.Email;
        if (request.Website != null) company.Website = request.Website;
        if (request.Notes != null) company.Notes = request.Notes;
        if (request.StatusId.HasValue) company.StatusId = request.StatusId.Value;
        if (request.NextJobNumber.HasValue) company.NextJobNumber = request.NextJobNumber.Value;

        company.UpdatedById = ctx.UserId;
        company.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated company {CompanyId}", company.Id);

        return Ok(ToDto(company));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCompany(Guid id)
    {
        var company = await _db.Company.FindAsync(id);
        if (company == null)
            return NotFound();

        _db.Company.Remove(company);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted company {CompanyId}", id);

        return NoContent();
    }

    private async Task<UserSessionContext?> GetSessionAsync()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email)) return null;
        return await _session.GetOrLoadAsync(email);
    }

    private static CompanyDto ToDto(Company c) => new()
    {
        Id = c.Id,
        Vid = c.Vid,
        Name = c.Name,
        Address = c.Address,
        City = c.City,
        State = c.State,
        ZipCode = c.ZipCode,
        Country = c.Country,
        Phone = c.Phone,
        Email = c.Email,
        Website = c.Website,
        Notes = c.Notes,
        NextJobNumber = c.NextJobNumber,
        StatusId = c.StatusId,
        CreatedById = c.CreatedById,
        CreatedDate = c.CreatedDate,
        UpdatedById = c.UpdatedById,
        UpdatedDate = c.UpdatedDate
    };
}
