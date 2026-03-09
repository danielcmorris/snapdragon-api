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
public class UserController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<UserController> _logger;
    private readonly IUserSessionService _session;

    public UserController(AppDbContext db, ILogger<UserController> logger, IUserSessionService session)
    {
        _db = db;
        _logger = logger;
        _session = session;
    }

    [HttpGet]
    public async Task<ActionResult<UserListResponse>> GetUsers(
        [FromQuery] Guid? officeId = null,
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

        var query = _db.User
            .Include(u => u.Office)
            .Where(u => u.Office!.CompanyId == ctx.CompanyId);

        if (officeId.HasValue)
            query = query.Where(u => u.OfficeId == officeId.Value);
        if (statusId.HasValue)
            query = query.Where(u => u.StatusId == statusId.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => u.Email.Contains(search) ||
                (u.FirstName != null && u.FirstName.Contains(search)) ||
                (u.LastName != null && u.LastName.Contains(search)));

        var totalCount = await query.CountAsync();
        var users = await query
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Skip(skip)
            .Take(take)
            .Select(u => ToDto(u))
            .ToListAsync();

        return Ok(new UserListResponse { Users = users, TotalCount = totalCount });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetUser(Guid id)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        if (ctx.UserLevel < 2)
            return StatusCode(403, new { error = "Insufficient permissions. Manager role or higher required." });

        var user = await _db.User
            .Include(u => u.Office)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null || user.Office?.CompanyId != ctx.CompanyId)
            return NotFound();

        return Ok(ToDto(user));
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { error = "Email is required" });

        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        if (ctx.UserLevel < 2)
            return StatusCode(403, new { error = "Insufficient permissions. Manager role or higher required." });

        // Verify the office belongs to the user's company
        var office = await _db.Office.FindAsync(request.OfficeId);
        if (office == null || office.CompanyId != ctx.CompanyId)
            return BadRequest(new { error = "Invalid office selection" });

        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Vid = request.Vid,
            OfficeId = request.OfficeId,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            JobTitle = request.JobTitle,
            UserLevel = request.UserLevel,
            StatusId = 1,
            CreatedById = ctx.UserId,
            UpdatedById = ctx.UserId,
            CreatedDate = now,
            UpdatedDate = now
        };

        _db.User.Add(user);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created user {UserId} - {UserEmail}", user.Id, user.Email);

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, ToDto(user));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserDto>> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        if (ctx.UserLevel < 2)
            return StatusCode(403, new { error = "Insufficient permissions. Manager role or higher required." });

        var user = await _db.User
            .Include(u => u.Office)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null || user.Office?.CompanyId != ctx.CompanyId)
            return NotFound();

        // Verify office change belongs to user's company (if office is being changed)
        if (request.OfficeId.HasValue)
        {
            var office = await _db.Office.FindAsync(request.OfficeId.Value);
            if (office == null || office.CompanyId != ctx.CompanyId)
                return BadRequest(new { error = "Invalid office selection" });
            user.OfficeId = request.OfficeId.Value;
        }

        if (request.Vid.HasValue) user.Vid = request.Vid;
        if (request.Email != null) user.Email = request.Email;
        if (request.FirstName != null) user.FirstName = request.FirstName;
        if (request.LastName != null) user.LastName = request.LastName;
        if (request.Phone != null) user.Phone = request.Phone;
        if (request.JobTitle != null) user.JobTitle = request.JobTitle;
        if (request.UserLevel.HasValue) user.UserLevel = request.UserLevel.Value;
        if (request.StatusId.HasValue) user.StatusId = request.StatusId.Value;

        user.UpdatedById = ctx.UserId;
        user.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated user {UserId}", user.Id);

        return Ok(ToDto(user));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var ctx = await GetSessionAsync();
        if (ctx == null)
            return Unauthorized(new { error = "Unable to identify user" });

        if (ctx.UserLevel < 2)
            return StatusCode(403, new { error = "Insufficient permissions. Manager role or higher required." });

        var user = await _db.User
            .Include(u => u.Office)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null || user.Office?.CompanyId != ctx.CompanyId)
            return NotFound();

        if (user.Id == ctx.UserId)
            return BadRequest(new { error = "Cannot delete your own account" });

        _db.User.Remove(user);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted user {UserId}", id);

        return NoContent();
    }

    private async Task<UserSessionContext?> GetSessionAsync()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email)) return null;
        return await _session.GetOrLoadAsync(email);
    }

    private static UserDto ToDto(User u) => new()
    {
        Id = u.Id,
        Vid = u.Vid,
        OfficeId = u.OfficeId,
        Email = u.Email,
        FirstName = u.FirstName,
        LastName = u.LastName,
        Phone = u.Phone,
        JobTitle = u.JobTitle,
        UserLevel = u.UserLevel,
        DefaultWarehouseId = u.DefaultWarehouseId,
        StatusId = u.StatusId,
        CreatedById = u.CreatedById,
        CreatedDate = u.CreatedDate,
        UpdatedById = u.UpdatedById,
        UpdatedDate = u.UpdatedDate
    };
}
