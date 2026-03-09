using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapdragonApi.DTOs;
using SnapdragonApi.Models;

namespace SnapdragonApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OnboardingController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<OnboardingController> _logger;

    public OnboardingController(AppDbContext db, ILogger<OnboardingController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<OnboardingResponse>> Onboard([FromBody] OnboardingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyName))
            return BadRequest(new { error = "CompanyName is required" });
        if (string.IsNullOrWhiteSpace(request.UserEmail))
            return BadRequest(new { error = "UserEmail is required" });

        var now = DateTime.UtcNow;

        // Create company
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Vid = request.CompanyVid,
            Name = request.CompanyName,
            Address = request.Address,
            City = request.City,
            State = request.State,
            ZipCode = request.ZipCode,
            Country = request.Country,
            Phone = request.CompanyPhone,
            Email = request.CompanyEmail,
            Website = request.Website,
            Notes = request.Notes,
            StatusId = 1,
            CreatedDate = now,
            UpdatedDate = now
        };

        // Create office with same address info
        var office = new Office
        {
            Id = Guid.NewGuid(),
            Vid = request.CompanyVid,
            CompanyId = company.Id,
            Name = "Headquarters",
            Address = request.Address,
            City = request.City,
            State = request.State,
            ZipCode = request.ZipCode,
            Country = request.Country,
            Phone = request.CompanyPhone,
            Email = request.CompanyEmail,
            IsHeadquarters = true,
            StatusId = 1,
            CreatedDate = now,
            UpdatedDate = now
        };

        // Create user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Vid = request.UserVid,
            OfficeId = office.Id,
            Email = request.UserEmail,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.UserPhone,
            JobTitle = request.JobTitle,
            StatusId = 1,
            CreatedDate = now,
            UpdatedDate = now
        };

        _db.Company.Add(company);
        _db.Office.Add(office);
        _db.User.Add(user);

        await _db.SaveChangesAsync();

        _logger.LogInformation("Onboarded company {CompanyId} with office {OfficeId} and user {UserId}",
            company.Id, office.Id, user.Id);

        return CreatedAtAction(nameof(Onboard), new OnboardingResponse
        {
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
                StatusId = user.StatusId,
                CreatedById = user.CreatedById,
                CreatedDate = user.CreatedDate,
                UpdatedById = user.UpdatedById,
                UpdatedDate = user.UpdatedDate
            }
        });
    }
}
