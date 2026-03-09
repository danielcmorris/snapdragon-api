using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnapdragonApi.Models;

namespace SnapdragonApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;

    public HealthController(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpGet]
    public ActionResult<object> Get()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }

    [HttpGet("detailed")]
    [Authorize]
    public async Task<ActionResult<object>> GetDetailed()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var isAuthenticated = !string.IsNullOrEmpty(email);

        // Check database connection
        var dbName = _db.Database.GetDbConnection().Database;
        var dbConnected = false;
        string? dbError = null;

        try
        {
            await _db.Database.CanConnectAsync();
            dbConnected = true;
        }
        catch (Exception ex)
        {
            dbError = ex.Message;
        }

        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            database = new
            {
                name = dbName,
                connected = dbConnected,
                error = dbError
            },
            authentication = new
            {
                isAuthenticated = isAuthenticated,
                userEmail = email
            }
        });
    }
}
