using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapdragonApi.DTOs;
using SnapdragonApi.Services;

namespace SnapdragonApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LookupController : ControllerBase
{
    private readonly IPlacesService _placesService;
    private readonly ILogger<LookupController> _logger;

    public LookupController(IPlacesService placesService, ILogger<LookupController> logger)
    {
        _placesService = placesService;
        _logger = logger;
    }

    [HttpGet("company")]
    public async Task<ActionResult<CompanyLookupResponse>> LookupCompany([FromQuery] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Company name is required");

        _logger.LogInformation("Company lookup requested for '{Name}'", name);
        var result = await _placesService.LookupCompanyAsync(name);
        return Ok(result);
    }

    [HttpGet("address")]
    public async Task<ActionResult<AddressValidationResponse>> ValidateAddress([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("Address query is required");

        _logger.LogInformation("Address validation requested for '{Query}'", q);
        var result = await _placesService.ValidateAddressAsync(q);
        return Ok(result);
    }
}
