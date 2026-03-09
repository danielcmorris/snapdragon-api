namespace SnapdragonApi.DTOs;

public class InitiateChunkingRequest
{
    public string Bucket { get; set; } = string.Empty;
    public string? Prefix { get; set; }
}

public class InitiateChunkingResponse
{
    public int FilesProcessed { get; set; }
    public List<string> ProcessedFiles { get; set; } = new();
    public string Status { get; set; } = string.Empty;
}

public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
    public bool IncludeGeminiAnswer { get; set; } = true;
    public bool IncludeResultList { get; set; } = true;
    public int MaxResults { get; set; } = 10;
}

public class SearchResponse
{
    public string? GeminiAnswer { get; set; }
    public List<SearchResult> Results { get; set; } = new();
}

public class SearchResult
{
    public string DocumentId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Snippet { get; set; } = string.Empty;
    public double RelevanceScore { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public class ResearchRequest
{
    public string OriginalQuery { get; set; } = string.Empty;
    public string FollowUpQuestion { get; set; } = string.Empty;
    public List<string>? PreviousContext { get; set; }
}

public class ResearchResponse
{
    public string Answer { get; set; } = string.Empty;
    public List<SearchResult> SupportingDocuments { get; set; } = new();
}

public class ProposalRequest
{
    public EventSetup EventDetails { get; set; } = new();
    public string? AdditionalInstructions { get; set; }
}

public class ProposalResponse
{
    public Bid Bid { get; set; } = new();
    public EventSetup EventDetails { get; set; } = new();
    public List<SearchResult> ReferencedDocuments { get; set; } = new();
}

public class Bid
{
    public string BidName { get; set; } = string.Empty;
    public string? InternalNotes { get; set; }
    public string? CustomerFacingNotes { get; set; }
    public List<BidRoom> Rooms { get; set; } = new();
    public double GrandTotal { get; set; }
}

public class BidRoom
{
    public string RoomName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SetupDate { get; set; }
    public string? SetupTime { get; set; }
    public string? EventDate { get; set; }
    public string? EventTime { get; set; }
    public string? EndDate { get; set; }
    public string? EndTime { get; set; }
    public List<BidEquipment> Equipment { get; set; } = new();
    public double Subtotal { get; set; }
}

public class BidEquipment
{
    public string CustomName { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public double Price { get; set; }
    public double ServicePercentBeforeDiscount { get; set; }
    public double Discount { get; set; }
    public double ServicePercentAfterDiscount { get; set; }
    public double SubrentalFee { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public double LineTotal { get; set; }
}

public class ModifyBidRequest
{
    public Bid Bid { get; set; } = new();
    public string Instruction { get; set; } = string.Empty;
}

public class ModifyBidResponse
{
    public Bid Bid { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}

public class MatchProductRequest
{
    public string ProductName { get; set; } = string.Empty;
    public string? PartNumber { get; set; }
    public int MaxResults { get; set; } = 5;
}

public class MatchProductResponse
{
    public List<ProductMatch> Matches { get; set; } = new();
}

public class ProductMatch
{
    public string? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Manufacturer { get; set; }
    public double? Price { get; set; }
    public double ConfidenceScore { get; set; }
    public string? Description { get; set; }
}

public class EmbeddingStatusResponse
{
    public int TotalProducts { get; set; }
    public int WithEmbeddings { get; set; }
    public int WithoutEmbeddings { get; set; }
    public int PercentComplete { get; set; }
}

public class RoomMatchRequest
{
    public string RoomName { get; set; } = string.Empty;
    public List<RoomProductInput> Products { get; set; } = new();
    public int MaxMatchesPerProduct { get; set; } = 3;
    public double MinConfidence { get; set; } = 0.5;
}

public class RoomProductInput
{
    public string LegacyProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal? LegacyPrice { get; set; }
}

public class RoomMatchResponse
{
    public string RoomName { get; set; } = string.Empty;
    public List<ProductMatchGroup> ProductMatches { get; set; } = new();
    public int TotalProducts { get; set; }
    public int MatchedProducts { get; set; }
    public double QueryTimeMs { get; set; }
}

public class ProductMatchGroup
{
    public string LegacyProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal? LegacyPrice { get; set; }
    public List<ProductMatch> CurrentMatches { get; set; } = new();
    public ProductMatch? BestMatch => CurrentMatches.FirstOrDefault();
}
