using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapdragonApi.DTOs;
using SnapdragonApi.Services;

namespace SnapdragonApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VertexAiController : ControllerBase
{
    private readonly IVertexAiService _vertexAiService;
    private readonly IVectorSearchService _vectorSearchService;
    private readonly IUserSessionService _sessionService;
    private readonly ILogger<VertexAiController> _logger;

    public VertexAiController(
        IVertexAiService vertexAiService,
        IVectorSearchService vectorSearchService,
        IUserSessionService sessionService,
        ILogger<VertexAiController> logger)
    {
        _vertexAiService = vertexAiService;
        _vectorSearchService = vectorSearchService;
        _sessionService = sessionService;
        _logger = logger;
    }

    [HttpPost("initiate")]
    public async Task<ActionResult<InitiateChunkingResponse>> InitiateChunking([FromBody] InitiateChunkingRequest request)
    {
        var result = await _vertexAiService.InitiateChunkingAsync(request.Bucket, request.Prefix);
        return Ok(result);
    }

    [HttpPost("search")]
    public async Task<ActionResult<SearchResponse>> Search([FromBody] SearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return BadRequest("Query is required");

        var result = await _vertexAiService.SearchAsync(request);
        return Ok(result);
    }

    [HttpPost("research")]
    public async Task<ActionResult<ResearchResponse>> Research([FromBody] ResearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FollowUpQuestion))
            return BadRequest("Follow-up question is required");

        var result = await _vertexAiService.ResearchAsync(request);
        return Ok(result);
    }

    [HttpPost("proposal")]
    public async Task<ActionResult<ProposalResponse>> GenerateProposal([FromBody] ProposalRequest request)
    {
        var result = await _vertexAiService.GenerateProposalAsync(request);
        return Ok(result);
    }

    [HttpPost("modify-bid")]
    public async Task<ActionResult<ModifyBidResponse>> ModifyBid([FromBody] ModifyBidRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Instruction))
            return BadRequest("Instruction is required");

        var result = await _vertexAiService.ModifyBidAsync(request);
        return Ok(result);
    }

    [HttpPost("match-product")]
    public async Task<ActionResult<MatchProductResponse>> MatchProduct([FromBody] MatchProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProductName))
            return BadRequest("Product name is required");

        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email))
            return Unauthorized("Unable to identify user");

        var session = await _sessionService.GetOrLoadAsync(email);
        var result = await _vertexAiService.MatchProductAsync(request, session.CompanyId);
        return Ok(result);
    }

    [HttpGet("embedding-status")]
    public async Task<ActionResult<EmbeddingStatusResponse>> GetEmbeddingStatus()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email))
            return Unauthorized("Unable to identify user");

        var session = await _sessionService.GetOrLoadAsync(email);
        var result = await _vectorSearchService.GetEmbeddingStatusAsync(session.CompanyId);
        return Ok(result);
    }

    [HttpPost("match-room-products")]
    public async Task<ActionResult<RoomMatchResponse>> MatchRoomProducts([FromBody] RoomMatchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RoomName))
            return BadRequest("Room name is required");

        if (request.Products == null || request.Products.Count == 0)
            return BadRequest("At least one product is required");

        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email))
            return Unauthorized("Unable to identify user");

        var session = await _sessionService.GetOrLoadAsync(email);
        var result = await _vertexAiService.MatchRoomProductsAsync(request, session.CompanyId);
        return Ok(result);
    }
}
