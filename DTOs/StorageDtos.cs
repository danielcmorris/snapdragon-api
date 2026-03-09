namespace SnapdragonApi.DTOs;

public class FileUploadRequest
{
    public IFormFile File { get; set; } = null!;
    public string Bucket { get; set; } = string.Empty;
    public string? Path { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public class SignedLinkRequest
{
    public string Bucket { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}

public class DirectoryListRequest
{
    public string Bucket { get; set; } = string.Empty;
    public string? Prefix { get; set; }
    public int PageSize { get; set; } = 50;
    public string? PageToken { get; set; }
}

public class DirectoryListResponse
{
    public List<StorageFileInfo> Files { get; set; } = new();
    public string? NextPageToken { get; set; }
}

public class StorageFileInfo
{
    public string Name { get; set; } = string.Empty;
    public ulong? Size { get; set; }
    public DateTimeOffset? Updated { get; set; }
    public string? ContentType { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public class SearchFilterRequest
{
    public string Bucket { get; set; } = string.Empty;
    public string? NameContains { get; set; }
    public string? Prefix { get; set; }
    public Dictionary<string, string>? MetadataFilters { get; set; }
    public int PageSize { get; set; } = 50;
    public string? PageToken { get; set; }
}

public class DeleteFilesRequest
{
    public string Bucket { get; set; } = string.Empty;
    public List<string> FileNames { get; set; } = new();
}

public class MoveFileRequest
{
    public string Bucket { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public string DestinationName { get; set; } = string.Empty;
}
