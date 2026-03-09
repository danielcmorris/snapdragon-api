namespace SnapdragonApi.Models;

public class GoogleAuthSettings
{
    public string ClientId { get; set; } = string.Empty;
    public List<string> AllowedEmails { get; set; } = new();
}

public class GoogleCloudSettings
{
    public string ProjectId { get; set; } = string.Empty;
    public string CredentialPath { get; set; } = string.Empty;
    public string PlacesApiKey { get; set; } = string.Empty;
    public BucketSettings Buckets { get; set; } = new();
}

public class BucketSettings
{
    public string Invoice { get; set; } = "invoice-repository";
    public string Object { get; set; } = "object-repository";
    public string Proposal { get; set; } = "proposal-repository";

    public string[] All => new[] { Invoice, Object, Proposal };

    public bool IsValidBucket(string name) =>
        name == Invoice || name == Object || name == Proposal;
}

public class VertexAiSettings
{
    public string ProjectId { get; set; } = string.Empty;
    public string Location { get; set; } = "us-central1";
    public string SearchLocation { get; set; } = "global";
    public string DataStoreId { get; set; } = string.Empty;
    public string ModelId { get; set; } = "gemini-2.0-flash";
}
