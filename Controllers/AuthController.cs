using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapdragonApi.DTOs;
using SnapdragonApi.Services;

namespace SnapdragonApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IGoogleAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IGoogleAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("google-login")]
    public async Task<ActionResult<AuthResponse>> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        var payload = await _authService.ValidateGoogleTokenAsync(request.IdToken);
        if (payload == null)
        {
            return Unauthorized(new AuthResponse
            {
                Success = false,
                Error = "Invalid Google token"
            });
        }

        if (!_authService.IsEmailAllowed(payload.Email))
        {
            _logger.LogWarning("Unauthorized login attempt from {Email}", payload.Email);
            return Unauthorized(new AuthResponse
            {
                Success = false,
                Error = "Email not authorized"
            });
        }

        var token = _authService.GenerateJwtToken(payload.Email, payload.Name);

        return Ok(new AuthResponse
        {
            Success = true,
            Token = token,
            Email = payload.Email,
            Name = payload.Name
        });
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<object> GetCurrentUser()
    {
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var name = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

        return Ok(new { email, name });
    }
}
