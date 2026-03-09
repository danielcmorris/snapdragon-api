using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Options;
using SnapdragonApi.DTOs;
using SnapdragonApi.Models;
using Object = Google.Apis.Storage.v1.Data.Object;

namespace SnapdragonApi.Services;

public interface IStorageService
{
    Task<StorageFileInfo> UploadFileAsync(string bucket, string fileName, Stream content, string contentType, Dictionary<string, string>? metadata);
    Task<string> GetSignedUrlAsync(string bucket, string fileName, int expirationMinutes);
    Task<DirectoryListResponse> ListFilesAsync(string bucket, string? prefix, int pageSize, string? pageToken);
    Task<DirectoryListResponse> SearchFilesAsync(string bucket, string? nameContains, string? prefix, Dictionary<string, string>? metadataFilters, int pageSize, string? pageToken);
    Task<List<string>> DeleteFilesAsync(string bucket, List<string> fileNames);
    Task<StorageFileInfo> MoveFileAsync(string bucket, string sourceName, string destinationName);
}

public class StorageService : IStorageService
{
    private readonly StorageClient _storageClient;
    private readonly GoogleCloudSettings _settings;
    private readonly ILogger<StorageService> _logger;
    private readonly GoogleCredential _credential;

    public StorageService(
        IOptions<GoogleCloudSettings> settings,
        ILogger<StorageService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _credential = string.IsNullOrEmpty(_settings.CredentialPath)
            ? GoogleCredential.GetApplicationDefault()
            : GoogleCredential.FromFile(_settings.CredentialPath);
        _storageClient = StorageClient.Create(_credential);
    }

    public async Task<StorageFileInfo> UploadFileAsync(
        string bucket, string fileName, Stream content,
        string contentType, Dictionary<string, string>? metadata)
    {
        var obj = new Object
        {
            Bucket = bucket,
            Name = fileName,
            ContentType = contentType,
            Metadata = metadata
        };

        var uploaded = await _storageClient.UploadObjectAsync(obj, content);
        _logger.LogInformation("Uploaded {FileName} to {Bucket}", fileName, bucket);

        return MapToFileInfo(uploaded);
    }

    public async Task<string> GetSignedUrlAsync(string bucket, string fileName, int expirationMinutes)
    {
        var signer = UrlSigner.FromCredential(_credential);
        var url = await signer.SignAsync(bucket, fileName, TimeSpan.FromMinutes(expirationMinutes));
        return url;
    }

    public async Task<DirectoryListResponse> ListFilesAsync(
        string bucket, string? prefix, int pageSize, string? pageToken)
    {
        var response = new DirectoryListResponse();
        var options = new ListObjectsOptions
        {
            PageSize = pageSize
        };

        var page = _storageClient.ListObjectsAsync(bucket, prefix, options);
        var enumerator = page.AsRawResponses().GetAsyncEnumerator();

        // If we have a page token, we need to skip to it
        // GCS SDK handles this via the page token in options
        if (!string.IsNullOrEmpty(pageToken))
        {
            options.PageToken = pageToken;
            page = _storageClient.ListObjectsAsync(bucket, prefix, options);
            enumerator = page.AsRawResponses().GetAsyncEnumerator();
        }

        if (await enumerator.MoveNextAsync())
        {
            var rawPage = enumerator.Current;
            response.NextPageToken = rawPage.NextPageToken;
            if (rawPage.Items != null)
            {
                response.Files = rawPage.Items.Select(MapToFileInfo).ToList();
            }
        }

        return response;
    }

    public async Task<DirectoryListResponse> SearchFilesAsync(
        string bucket, string? nameContains, string? prefix,
        Dictionary<string, string>? metadataFilters, int pageSize, string? pageToken)
    {
        var allFiles = new List<StorageFileInfo>();
        var options = new ListObjectsOptions { PageSize = pageSize };
        if (!string.IsNullOrEmpty(pageToken))
            options.PageToken = pageToken;

        var page = _storageClient.ListObjectsAsync(bucket, prefix, options);
        var enumerator = page.AsRawResponses().GetAsyncEnumerator();

        string? nextPageToken = null;

        if (await enumerator.MoveNextAsync())
        {
            var rawPage = enumerator.Current;
            nextPageToken = rawPage.NextPageToken;

            if (rawPage.Items != null)
            {
                foreach (var item in rawPage.Items)
                {
                    var matches = true;

                    if (!string.IsNullOrEmpty(nameContains) &&
                        !item.Name.Contains(nameContains, StringComparison.OrdinalIgnoreCase))
                    {
                        matches = false;
                    }

                    if (matches && metadataFilters != null && metadataFilters.Count > 0)
                    {
                        if (item.Metadata == null)
                        {
                            matches = false;
                        }
                        else
                        {
                            foreach (var filter in metadataFilters)
                            {
                                if (!item.Metadata.TryGetValue(filter.Key, out var val) ||
                                    !val.Contains(filter.Value, StringComparison.OrdinalIgnoreCase))
                                {
                                    matches = false;
                                    break;
                                }
                            }
                        }
                    }

                    if (matches)
                        allFiles.Add(MapToFileInfo(item));
                }
            }
        }

        return new DirectoryListResponse
        {
            Files = allFiles,
            NextPageToken = nextPageToken
        };
    }

    public async Task<List<string>> DeleteFilesAsync(string bucket, List<string> fileNames)
    {
        var deleted = new List<string>();
        foreach (var name in fileNames)
        {
            try
            {
                await _storageClient.DeleteObjectAsync(bucket, name);
                deleted.Add(name);
                _logger.LogInformation("Deleted {FileName} from {Bucket}", name, bucket);
            }
            catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("File {FileName} not found in {Bucket}", name, bucket);
            }
        }
        return deleted;
    }

    public async Task<StorageFileInfo> MoveFileAsync(string bucket, string sourceName, string destinationName)
    {
        var copied = await _storageClient.CopyObjectAsync(bucket, sourceName, bucket, destinationName);
        await _storageClient.DeleteObjectAsync(bucket, sourceName);
        _logger.LogInformation("Moved {Source} to {Destination} in {Bucket}", sourceName, destinationName, bucket);
        return MapToFileInfo(copied);
    }

    private static StorageFileInfo MapToFileInfo(Object obj)
    {
        return new StorageFileInfo
        {
            Name = obj.Name,
            Size = obj.Size,
            Updated = obj.UpdatedDateTimeOffset,
            ContentType = obj.ContentType,
            Metadata = obj.Metadata?.ToDictionary(k => k.Key, v => v.Value)
        };
    }
}
