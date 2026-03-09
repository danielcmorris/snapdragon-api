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
public class LoginController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IUserSessionService _sessionService;
    private readonly ILogger<LoginController> _logger;

    public LoginController(AppDbContext db, IUserSessionService sessionService, ILogger<LoginController> logger)
    {
        _db = db;
        _sessionService = sessionService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<LoginResponse>> Login()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(email))
        {
            return Ok(new LoginResponse
            {
                Found = false,
                Status = "unknown"
            });
        }

        var user = await _db.User
            .Include(u => u.Office)
            .ThenInclude(o => o!.Company)
            .FirstOrDefaultAsync(u => u.Email == email && u.StatusId == 1);

        if (user == null)
        {
            _logger.LogInformation("Login attempt for unknown user {Email}", email);
            return Ok(new LoginResponse
            {
                Found = false,
                Status = "unknown"
            });
        }

        var office = user.Office;
        var company = office?.Company;

        if (office == null || company == null || office.StatusId != 2 || company.StatusId != 2)
        {
            _logger.LogInformation("Login attempt for user {Email} with inactive office/company", email);
            return Ok(new LoginResponse
            {
                Found = false,
                Status = "unknown"
            });
        }

        _logger.LogInformation("User {Email} logged in successfully", email);

        var session = await _sessionService.GetOrLoadAsync(email);

        return Ok(new LoginResponse
        {
            Found = true,
            Status = "active",
            Company = new CompanyDto
            {
                Id = company.Id,
                Vid = company.Vid,
                Name = company.Name,
                Address = company.Address,
                City = company.City,
                State = company.State,
                ZipCode = company.ZipCode,
                Country = company.Country,
                Phone = company.Phone,
                Email = company.Email,
                Website = company.Website,
                Notes = company.Notes,
                StatusId = company.StatusId,
                CreatedById = company.CreatedById,
                CreatedDate = company.CreatedDate,
                UpdatedById = company.UpdatedById,
                UpdatedDate = company.UpdatedDate
            },
            Office = new OfficeDto
            {
                Id = office.Id,
                Vid = office.Vid,
                CompanyId = office.CompanyId,
                Name = office.Name,
                Address = office.Address,
                City = office.City,
                State = office.State,
                ZipCode = office.ZipCode,
                Country = office.Country,
                Phone = office.Phone,
                Email = office.Email,
                IsHeadquarters = office.IsHeadquarters,
                StatusId = office.StatusId,
                CreatedById = office.CreatedById,
                CreatedDate = office.CreatedDate,
                UpdatedById = office.UpdatedById,
                UpdatedDate = office.UpdatedDate
            },
            User = new UserDto
            {
                Id = user.Id,
                Vid = user.Vid,
                OfficeId = user.OfficeId,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.Phone,
                JobTitle = user.JobTitle,
                UserLevel = user.UserLevel,
                DefaultWarehouseId = user.DefaultWarehouseId,
                StatusId = user.StatusId,
                CreatedById = user.CreatedById,
                CreatedDate = user.CreatedDate,
                UpdatedById = user.UpdatedById,
                UpdatedDate = user.UpdatedDate
            },
            Session = session
        });
    }
}
