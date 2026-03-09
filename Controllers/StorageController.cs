using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SnapdragonApi.DTOs;
using SnapdragonApi.Models;
using SnapdragonApi.Services;

namespace SnapdragonApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StorageController : ControllerBase
{
    private readonly IStorageService _storageService;
    private readonly BucketSettings _bucketSettings;
    private readonly ILogger<StorageController> _logger;

    public StorageController(
        IStorageService storageService,
        IOptions<GoogleCloudSettings> cloudSettings,
        ILogger<StorageController> logger)
    {
        _storageService = storageService;
        _bucketSettings = cloudSettings.Value.Buckets;
        _logger = logger;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(100_000_000)] // 100MB limit
    public async Task<ActionResult<StorageFileInfo>> Upload([FromForm] FileUploadRequest request)
    {
        if (!_bucketSettings.IsValidBucket(request.Bucket))
            return BadRequest($"Invalid bucket. Valid buckets: {string.Join(", ", _bucketSettings.All)}");

        if (request.File == null || request.File.Length == 0)
            return BadRequest("No file provided");

        var fileName = request.Path != null
            ? $"{request.Path.TrimEnd('/')}/{request.File.FileName}"
            : request.File.FileName;

        using var stream = request.File.OpenReadStream();
        var result = await _storageService.UploadFileAsync(
            request.Bucket, fileName, stream,
            request.File.ContentType, request.Metadata);

        return Ok(result);
    }

    [HttpPost("signed-link")]
    public async Task<ActionResult<object>> GetSignedLink([FromBody] SignedLinkRequest request)
    {
        if (!_bucketSettings.IsValidBucket(request.Bucket))
            return BadRequest($"Invalid bucket. Valid buckets: {string.Join(", ", _bucketSettings.All)}");

        var url = await _storageService.GetSignedUrlAsync(
            request.Bucket, request.FileName, request.ExpirationMinutes);

        return Ok(new { url, expiresIn = $"{request.ExpirationMinutes} minutes" });
    }

    [HttpPost("list")]
    public async Task<ActionResult<DirectoryListResponse>> ListFiles([FromBody] DirectoryListRequest request)
    {
        if (!_bucketSettings.IsValidBucket(request.Bucket))
            return BadRequest($"Invalid bucket. Valid buckets: {string.Join(", ", _bucketSettings.All)}");

        var result = await _storageService.ListFilesAsync(
            request.Bucket, request.Prefix, request.PageSize, request.PageToken);

        return Ok(result);
    }

    [HttpPost("search")]
    public async Task<ActionResult<DirectoryListResponse>> SearchFiles([FromBody] SearchFilterRequest request)
    {
        if (!_bucketSettings.IsValidBucket(request.Bucket))
            return BadRequest($"Invalid bucket. Valid buckets: {string.Join(", ", _bucketSettings.All)}");

        var result = await _storageService.SearchFilesAsync(
            request.Bucket, request.NameContains, request.Prefix,
            request.MetadataFilters, request.PageSize, request.PageToken);

        return Ok(result);
    }

    [HttpPost("delete")]
    public async Task<ActionResult<object>> DeleteFiles([FromBody] DeleteFilesRequest request)
    {
        if (!_bucketSettings.IsValidBucket(request.Bucket))
            return BadRequest($"Invalid bucket. Valid buckets: {string.Join(", ", _bucketSettings.All)}");

        if (request.FileNames == null || request.FileNames.Count == 0)
            return BadRequest("No file names provided");

        var deleted = await _storageService.DeleteFilesAsync(request.Bucket, request.FileNames);

        return Ok(new
        {
            deletedCount = deleted.Count,
            deletedFiles = deleted,
            requestedCount = request.FileNames.Count
        });
    }

    [HttpPost("move")]
    public async Task<ActionResult<StorageFileInfo>> MoveFile([FromBody] MoveFileRequest request)
    {
        if (!_bucketSettings.IsValidBucket(request.Bucket))
            return BadRequest($"Invalid bucket. Valid buckets: {string.Join(", ", _bucketSettings.All)}");

        var result = await _storageService.MoveFileAsync(
            request.Bucket, request.SourceName, request.DestinationName);

        return Ok(result);
    }
}
