using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapdragonApi.DTOs;
using SnapdragonApi.Services;

namespace SnapdragonApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InterrogatorController : ControllerBase
{
    private readonly IInterrogatorService _interrogatorService;
    private readonly ILogger<InterrogatorController> _logger;

    public InterrogatorController(IInterrogatorService interrogatorService, ILogger<InterrogatorController> logger)
    {
        _interrogatorService = interrogatorService;
        _logger = logger;
    }

    [HttpGet("event-setup")]
    public ActionResult<EventSetup> GetEventSetupTemplate()
    {
        var template = _interrogatorService.GetEmptyEventSetup();
        return Ok(template);
    }

    [HttpPost("interactive-setup")]
    public async Task<ActionResult<InteractiveSetupResponse>> InteractiveSetup([FromBody] InteractiveSetupRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserText))
            return BadRequest("User text is required");

        var result = await _interrogatorService.ProcessInteractiveSetupAsync(request);
        return Ok(result);
    }
}
