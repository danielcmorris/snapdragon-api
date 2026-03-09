using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.AIPlatform.V1;
using Google.Cloud.DiscoveryEngine.V1;
using Google.Cloud.BigQuery.V2;
using Microsoft.Extensions.Options;
using SnapdragonApi.DTOs;
using SnapdragonApi.Models;
using DiscoverySearchRequest = Google.Cloud.DiscoveryEngine.V1.SearchRequest;

namespace SnapdragonApi.Services;

public interface IVertexAiService
{
    Task<InitiateChunkingResponse> InitiateChunkingAsync(string bucket, string? prefix);
    Task<DTOs.SearchResponse> SearchAsync(DTOs.SearchRequest request);
    Task<ResearchResponse> ResearchAsync(ResearchRequest request);
    Task<ProposalResponse> GenerateProposalAsync(ProposalRequest request);
    Task<ModifyBidResponse> ModifyBidAsync(ModifyBidRequest request);
    Task<MatchProductResponse> MatchProductAsync(MatchProductRequest request, Guid companyId);
    Task<RoomMatchResponse> MatchRoomProductsAsync(RoomMatchRequest request, Guid companyId);
}

public class VertexAiService : IVertexAiService
{
    private readonly VertexAiSettings _settings;
    private readonly GoogleCloudSettings _cloudSettings;
    private readonly ILogger<VertexAiService> _logger;
    private readonly GoogleCredential _credential;
    private readonly IVectorSearchService? _vectorSearch;

    public VertexAiService(
        IOptions<VertexAiSettings> settings,
        IOptions<GoogleCloudSettings> cloudSettings,
        ILogger<VertexAiService> logger,
        IVectorSearchService? vectorSearch = null)
    {
        _settings = settings.Value;
        _cloudSettings = cloudSettings.Value;
        _logger = logger;
        _credential = GoogleCredential.FromFile(_cloudSettings.CredentialPath);
        _vectorSearch = vectorSearch;
    }

    public async Task<InitiateChunkingResponse> InitiateChunkingAsync(string bucket, string? prefix)
    {
        _logger.LogInformation("Initiating chunking for bucket {Bucket} with prefix {Prefix}", bucket, prefix);

        // This would connect to VertexAI's RAG API to import documents from GCS
        // The actual implementation depends on your specific VertexAI data store setup
        var response = new InitiateChunkingResponse
        {
            Status = "initiated",
            ProcessedFiles = new List<string>()
        };

        try
        {
            var gcsSource = $"gs://{bucket}";
            if (!string.IsNullOrEmpty(prefix))
                gcsSource += $"/{prefix}";

            // Import documents into the Discovery Engine data store
            var clientBuilder = new DocumentServiceClientBuilder
            {
                CredentialsPath = _cloudSettings.CredentialPath
            };
            var client = await clientBuilder.BuildAsync();

            var dataStoreName = DataStoreName.FromProjectLocationDataStore(
                _settings.ProjectId, _settings.SearchLocation, _settings.DataStoreId);

            var importRequest = new ImportDocumentsRequest
            {
                Parent = BranchName.FromProjectLocationDataStoreBranch(
                    _settings.ProjectId, _settings.SearchLocation,
                    _settings.DataStoreId, "default_branch").ToString(),
                GcsSource = new Google.Cloud.DiscoveryEngine.V1.GcsSource
                {
                    InputUris = { $"{gcsSource}/*" }
                },
                ReconciliationMode = ImportDocumentsRequest.Types.ReconciliationMode.Incremental
            };

            var operation = await client.ImportDocumentsAsync(importRequest);
            var result = await operation.PollUntilCompletedAsync();

            response.Status = result.IsCompleted ? "completed" : "in_progress";
            _logger.LogInformation("Chunking operation status: {Status}", response.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating chunking");
            response.Status = $"error: {ex.Message}";
        }

        return response;
    }

    public async Task<DTOs.SearchResponse> SearchAsync(DTOs.SearchRequest request)
    {
        _logger.LogInformation("Searching for: {Query}", request.Query);

        var response = new DTOs.SearchResponse();

        try
        {
            var clientBuilder = new SearchServiceClientBuilder
            {
                CredentialsPath = _cloudSettings.CredentialPath
            };
            var client = await clientBuilder.BuildAsync();

            var servingConfig = ServingConfigName.FromProjectLocationDataStoreServingConfig(
                _settings.ProjectId, _settings.SearchLocation,
                _settings.DataStoreId, "default_config");

            var searchRequest = new DiscoverySearchRequest
            {
                ServingConfig = servingConfig.ToString(),
                Query = request.Query,
                PageSize = request.MaxResults
            };

            if (request.IncludeGeminiAnswer)
            {
                searchRequest.ContentSearchSpec = new DiscoverySearchRequest.Types.ContentSearchSpec
                {
                    SummarySpec = new DiscoverySearchRequest.Types.ContentSearchSpec.Types.SummarySpec
                    {
                        SummaryResultCount = request.MaxResults,
                        IncludeCitations = true
                    },
                    SnippetSpec = new DiscoverySearchRequest.Types.ContentSearchSpec.Types.SnippetSpec
                    {
                        ReturnSnippet = true
                    }
                };
            }

            var searchResponse = client.Search(searchRequest);
            var page = searchResponse.AsRawResponses().FirstOrDefault();

            if (page != null)
            {
                if (request.IncludeGeminiAnswer && page.Summary != null)
                {
                    response.GeminiAnswer = page.Summary.SummaryText;
                }

                if (request.IncludeResultList)
                {
                    foreach (var result in page)
                    {
                        var derivedData = result.Document?.DerivedStructData?.Fields;
                        var link = derivedData?.GetValueOrDefault("link")?.StringValue ?? "";
                        var title = derivedData?.GetValueOrDefault("title")?.StringValue
                            ?? result.Document?.Name ?? "";
                        var snippet = derivedData?.GetValueOrDefault("snippets")?.ListValue?.Values
                            ?.FirstOrDefault()?.StructValue?.Fields
                            .GetValueOrDefault("snippet")?.StringValue ?? "";

                        // Extract bucket and filename from gs:// URI (e.g. gs://bucket-name/path/to/file.pdf)
                        var documentId = link;
                        var bucket = "";
                        if (link.StartsWith("gs://"))
                        {
                            var withoutScheme = link.Substring(5);
                            var slashIdx = withoutScheme.IndexOf('/');
                            if (slashIdx > 0)
                            {
                                bucket = withoutScheme.Substring(0, slashIdx);
                                documentId = withoutScheme.Substring(slashIdx + 1);
                            }
                        }

                        var searchResult = new DTOs.SearchResult
                        {
                            DocumentId = documentId,
                            Title = title,
                            Snippet = snippet,
                            Metadata = new Dictionary<string, string> { { "bucket", bucket } }
                        };
                        response.Results.Add(searchResult);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing search");
            throw;
        }

        return response;
    }

    public async Task<ResearchResponse> ResearchAsync(ResearchRequest request)
    {
        _logger.LogInformation("Researching follow-up: {Question}", request.FollowUpQuestion);

        // Combine the original query context with the follow-up question
        var combinedQuery = $"Context: {request.OriginalQuery}\n\nFollow-up question: {request.FollowUpQuestion}";

        if (request.PreviousContext != null && request.PreviousContext.Count > 0)
        {
            combinedQuery = $"Previous research:\n{string.Join("\n", request.PreviousContext)}\n\n{combinedQuery}";
        }

        var dtoSearchRequest = new DTOs.SearchRequest
        {
            Query = combinedQuery,
            IncludeGeminiAnswer = true,
            IncludeResultList = true,
            MaxResults = 10
        };

        var searchResponse = await SearchAsync(dtoSearchRequest);

        return new ResearchResponse
        {
            Answer = searchResponse.GeminiAnswer ?? "No answer generated.",
            SupportingDocuments = searchResponse.Results
        };
    }

    public async Task<ProposalResponse> GenerateProposalAsync(ProposalRequest request)
    {
        _logger.LogInformation("Generating proposal for event: {EventName}", request.EventDetails.EventName);

        // First, search for relevant past proposals and pricing
        var searchQuery = BuildProposalSearchQuery(request.EventDetails);
        var searchResults = await SearchAsync(new DTOs.SearchRequest
        {
            Query = searchQuery,
            IncludeGeminiAnswer = true,
            IncludeResultList = true,
            MaxResults = 10
        });

        // Use Gemini to generate the proposal based on search results and event details
        var bid = await GenerateProposalWithGeminiAsync(request, searchResults);

        return new ProposalResponse
        {
            Bid = bid,
            EventDetails = request.EventDetails,
            ReferencedDocuments = searchResults.Results
        };
    }

    private string BuildProposalSearchQuery(EventSetup eventDetails)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(eventDetails.EventType))
            parts.Add(eventDetails.EventType);
        if (eventDetails.EstimatedAttendees.HasValue)
            parts.Add($"{eventDetails.EstimatedAttendees} attendees");
        if (eventDetails.ServicesRequested != null)
            parts.AddRange(eventDetails.ServicesRequested);
        if (!string.IsNullOrEmpty(eventDetails.City))
            parts.Add(eventDetails.City);

        return $"proposal bid {string.Join(" ", parts)}";
    }

    private async Task<Bid> GenerateProposalWithGeminiAsync(ProposalRequest request, DTOs.SearchResponse searchResults)
    {
        try
        {
            var clientBuilder = new PredictionServiceClientBuilder
            {
                CredentialsPath = _cloudSettings.CredentialPath,
                Endpoint = $"{_settings.Location}-aiplatform.googleapis.com"
            };
            var client = await clientBuilder.BuildAsync();

            var model = $"projects/{_settings.ProjectId}/locations/{_settings.Location}/publishers/google/models/{_settings.ModelId}";

            var prompt = $@"Generate an AV equipment technical specification for an event as a JSON object.

Event Details:
- Client: {request.EventDetails.ClientName} ({request.EventDetails.ClientCompany})
- Event: {request.EventDetails.EventName} ({request.EventDetails.EventType})
- Date: {request.EventDetails.EventDate:yyyy-MM-dd}
- Location: {request.EventDetails.VenueName}, {request.EventDetails.VenueAddress}, {request.EventDetails.City}, {request.EventDetails.State}
- Estimated Attendees: {request.EventDetails.EstimatedAttendees}
- Services Requested: {string.Join(", ", request.EventDetails.ServicesRequested ?? new List<string>())}
- Budget: {request.EventDetails.BudgetRange}
- Special Requirements: {request.EventDetails.SpecialRequirements}
- Indoor/Outdoor: {(request.EventDetails.IndoorOutdoor.HasValue ? (request.EventDetails.IndoorOutdoor.Value ? "Indoor" : "Outdoor") : "Not specified")}

Reference information from similar past proposals:
{searchResults.GeminiAnswer}

{(string.IsNullOrEmpty(request.AdditionalInstructions) ? "" : $"Additional instructions: {request.AdditionalInstructions}")}

Output ONLY valid JSON matching this exact schema (no markdown fences, no commentary):
{{
  ""bidName"": ""<event name>"",
  ""internalNotes"": ""<internal notes about the job>"",
  ""customerFacingNotes"": ""<customer-facing notes>"",
  ""rooms"": [
    {{
      ""roomName"": ""<room or area name>"",
      ""description"": ""<purpose, e.g. Breakfast, Main Session>"",
      ""setupDate"": ""<MM/DD/YYYY or null>"",
      ""setupTime"": ""<HH:MM AM/PM or N/A>"",
      ""eventDate"": ""<MM/DD/YYYY>"",
      ""eventTime"": ""<HH:MM AM/PM or N/A>"",
      ""endDate"": ""<MM/DD/YYYY>"",
      ""endTime"": ""<HH:MM AM/PM or N/A>"",
      ""equipment"": [
        {{
          ""customName"": ""<equipment item description>"",
          ""quantity"": 1.0,
          ""price"": 100.00,
          ""servicePercentBeforeDiscount"": 0.0,
          ""discount"": 0.0,
          ""servicePercentAfterDiscount"": 0.0,
          ""subrentalFee"": 0.0,
          ""startDate"": ""<MM/DD/YYYY or null>"",
          ""endDate"": ""<MM/DD/YYYY or null>""
        }}
      ]
    }}
  ]
}}

Rules:
- Break the event into logical rooms/areas based on the venue and event type.
- Include a ""Set/Strike Labor"" room with technician labor items.
- Use realistic AV equipment (projectors, screens, speakers, microphones, lighting, staging, video switching, etc.) scaled to the event size and attendee count.
- Use realistic pricing based on the reference proposals and industry standards.
- All numeric fields must be numbers, not strings.
- Dates should use MM/DD/YYYY format. Times should use HH:MM AM/PM format.
- Output ONLY the JSON object, nothing else.";

            var content = new Content
            {
                Role = "user",
                Parts = { new Part { Text = prompt } }
            };

            var generateRequest = new GenerateContentRequest
            {
                Model = model,
                Contents = { content }
            };

            var response = await client.GenerateContentAsync(generateRequest);
            var text = response.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "";

            // Strip markdown code fences if present
            text = text.Trim();
            if (text.StartsWith("```"))
            {
                var firstNewline = text.IndexOf('\n');
                if (firstNewline > 0)
                    text = text.Substring(firstNewline + 1);
                if (text.EndsWith("```"))
                    text = text.Substring(0, text.Length - 3).Trim();
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var bid = JsonSerializer.Deserialize<Bid>(text, options);
            if (bid == null) return new Bid { BidName = "Unable to parse proposal" };
            CalculateBidTotals(bid);
            return bid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating proposal with Gemini");
            return new Bid { BidName = "Error", InternalNotes = ex.Message };
        }
    }

    public async Task<ModifyBidResponse> ModifyBidAsync(ModifyBidRequest request)
    {
        _logger.LogInformation("Modifying bid: {Instruction}", request.Instruction);

        try
        {
            var clientBuilder = new PredictionServiceClientBuilder
            {
                CredentialsPath = _cloudSettings.CredentialPath,
                Endpoint = $"{_settings.Location}-aiplatform.googleapis.com"
            };
            var client = await clientBuilder.BuildAsync();

            var model = $"projects/{_settings.ProjectId}/locations/{_settings.Location}/publishers/google/models/{_settings.ModelId}";

            var currentBidJson = JsonSerializer.Serialize(request.Bid, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            var prompt = $@"You are modifying an AV equipment bid/proposal JSON object.

Current bid JSON:
{currentBidJson}

User instruction: {request.Instruction}

Apply the user's instruction to the bid. Rules:
- Match rooms by name (case-insensitive, partial match OK — e.g. ""banquet"" matches ""Banquet Hall"").
- When adding equipment, if the same item already exists in the bid (any room), use the same price and settings. Otherwise use a reasonable industry price.
- When removing equipment, match by name (case-insensitive, partial match OK).
- When modifying quantities, prices, or other fields, update only what was requested.
- You may add new rooms if the instruction asks for it.
- Do NOT remove rooms or equipment unless explicitly asked.
- All numeric fields must be numbers, not strings.
- Do NOT calculate lineTotal, subtotal, or grandTotal — leave them as 0. The server will recalculate.

Respond with ONLY a JSON object with two fields:
1. ""bid"": the modified bid object matching the exact same schema as the input
2. ""summary"": a short one-sentence description of what you changed

Output ONLY valid JSON, no markdown fences, no commentary.";

            var content = new Content
            {
                Role = "user",
                Parts = { new Part { Text = prompt } }
            };

            var generateRequest = new GenerateContentRequest
            {
                Model = model,
                Contents = { content }
            };

            var response = await client.GenerateContentAsync(generateRequest);
            var text = response.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "";

            text = text.Trim();
            if (text.StartsWith("```"))
            {
                var firstNewline = text.IndexOf('\n');
                if (firstNewline > 0)
                    text = text.Substring(firstNewline + 1);
                if (text.EndsWith("```"))
                    text = text.Substring(0, text.Length - 3).Trim();
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<ModifyBidResponse>(text, options);
            if (result?.Bid == null)
                return new ModifyBidResponse { Bid = request.Bid, Summary = "Unable to parse modification result." };

            CalculateBidTotals(result.Bid);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error modifying bid with Gemini");
            return new ModifyBidResponse { Bid = request.Bid, Summary = $"Error: {ex.Message}" };
        }
    }

    public async Task<MatchProductResponse> MatchProductAsync(MatchProductRequest request, Guid companyId)
    {
        _logger.LogInformation("Matching product: {ProductName} for company {CompanyId}", request.ProductName, companyId);

        // Try vector search first if available
        if (_vectorSearch != null)
        {
            try
            {
                _logger.LogInformation("Using vector embeddings for semantic search");
                var vectorResponse = await _vectorSearch.SearchProductsAsync(request.ProductName, companyId, request.MaxResults);
                if (vectorResponse.Matches.Count > 0)
                {
                    _logger.LogInformation("Vector search found {Count} matches", vectorResponse.Matches.Count);
                    return vectorResponse;
                }
                _logger.LogWarning("Vector search returned no results, falling back to Gemini AI scoring");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Vector search failed, falling back to Gemini AI scoring");
            }
        }

        // Fallback to Gemini AI scoring
        _logger.LogInformation("Using Gemini AI for product matching");
        var response = new MatchProductResponse();

        try
        {
            // Query BigQuery ProductList table for candidate products
            var bigQueryClient = await BigQueryClient.CreateAsync(_settings.ProjectId, _credential);
            var datasetId = "snapdragon_data";
            var tableId = "ProductList";

            // Build search query with fuzzy matching
            var searchTerms = request.ProductName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var whereClauses = new List<string>();

            foreach (var term in searchTerms.Take(5)) // Limit to 5 terms to avoid overly complex queries
            {
                var safeTerm = term.Replace("'", "''").ToLower();
                whereClauses.Add($"(LOWER(product_name) LIKE '%{safeTerm}%' OR LOWER(description) LIKE '%{safeTerm}%' OR LOWER(category) LIKE '%{safeTerm}%')");
            }

            if (!string.IsNullOrWhiteSpace(request.PartNumber))
            {
                var safePart = request.PartNumber.Replace("'", "''").ToLower();
                whereClauses.Add($"LOWER(part_number) LIKE '%{safePart}%'");
            }

            var sql = $@"
                SELECT
                    product_id,
                    product_name,
                    part_number,
                    category,
                    manufacturer,
                    default_price,
                    description
                FROM `{_settings.ProjectId}.{datasetId}.{tableId}`
                WHERE company_id = '{companyId}'
                  AND is_active = true
                  AND ({string.Join(" OR ", whereClauses)})
                LIMIT {Math.Min(request.MaxResults * 3, 20)}
            ";

            _logger.LogInformation("Querying BigQuery for product candidates");

            var queryResult = await bigQueryClient.ExecuteQueryAsync(sql, parameters: null);
            var candidates = new List<(string id, string name, string partNum, string? category, string? manufacturer, double? price, string? description)>();

            foreach (var row in queryResult)
            {
                candidates.Add((
                    id: row["product_id"]?.ToString() ?? "",
                    name: row["product_name"]?.ToString() ?? "",
                    partNum: row["part_number"]?.ToString() ?? "",
                    category: row["category"]?.ToString(),
                    manufacturer: row["manufacturer"]?.ToString(),
                    price: row["default_price"] != null ? Convert.ToDouble(row["default_price"]) : null,
                    description: row["description"]?.ToString()
                ));
            }

            if (candidates.Count == 0)
            {
                _logger.LogWarning("No candidate products found in BigQuery for {ProductName}", request.ProductName);
                return response;
            }

            // Use Gemini to score and rank candidates
            var clientBuilder = new PredictionServiceClientBuilder
            {
                CredentialsPath = _cloudSettings.CredentialPath,
                Endpoint = $"{_settings.Location}-aiplatform.googleapis.com"
            };
            var aiClient = await clientBuilder.BuildAsync();
            var model = $"projects/{_settings.ProjectId}/locations/{_settings.Location}/publishers/google/models/{_settings.ModelId}";

            var candidatesJson = JsonSerializer.Serialize(candidates.Select((c, idx) => new
            {
                index = idx,
                name = c.name,
                partNumber = c.partNum,
                category = c.category,
                manufacturer = c.manufacturer,
                description = c.description
            }));

            var prompt = $@"You are a product matching expert. Score each candidate product on how well it matches the search query.

Search Query:
- Product Name: {request.ProductName}
- Part Number: {request.PartNumber ?? "N/A"}

Candidate Products:
{candidatesJson}

Scoring guidelines:
- 1.0 = Perfect match (exact name or part number)
- 0.9 = Very strong match (very similar name, same category)
- 0.7-0.8 = Good match (similar name or purpose)
- 0.5-0.6 = Moderate match (related category)
- < 0.5 = Weak match

Return ONLY valid JSON (no markdown):
[
  {{""index"": 0, ""confidenceScore"": 0.95}},
  {{""index"": 1, ""confidenceScore"": 0.75}}
]

Only include candidates with confidence >= 0.5. Sort by score descending.";

            var content = new Content
            {
                Role = "user",
                Parts = { new Part { Text = prompt } }
            };

            var generateRequest = new GenerateContentRequest
            {
                Model = model,
                Contents = { content },
                GenerationConfig = new GenerationConfig
                {
                    Temperature = 0.1f,
                    MaxOutputTokens = 2048
                }
            };

            var aiResponse = await aiClient.GenerateContentAsync(generateRequest);
            var responseText = aiResponse.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "[]";

            // Clean markdown fences
            responseText = responseText.Trim();
            if (responseText.StartsWith("```"))
            {
                var start = responseText.IndexOf('\n') + 1;
                var end = responseText.LastIndexOf("```");
                responseText = end > start ? responseText.Substring(start, end - start) : responseText;
            }

            var scores = JsonSerializer.Deserialize<List<JsonElement>>(responseText) ?? new();

            // Build matches from AI scores
            foreach (var scoreObj in scores.Take(request.MaxResults))
            {
                if (!scoreObj.TryGetProperty("index", out var indexProp) ||
                    !scoreObj.TryGetProperty("confidenceScore", out var scoreProp))
                    continue;

                var idx = indexProp.GetInt32();
                var score = scoreProp.GetDouble();

                if (idx >= 0 && idx < candidates.Count)
                {
                    var candidate = candidates[idx];
                    response.Matches.Add(new ProductMatch
                    {
                        ProductName = candidate.name,
                        PartNumber = candidate.partNum,
                        Category = candidate.category,
                        Manufacturer = candidate.manufacturer,
                        Price = candidate.price,
                        Description = candidate.description,
                        ConfidenceScore = score
                    });
                }
            }

            _logger.LogInformation("Found {Count} AI-scored product matches", response.Matches.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error matching product");
            // Return empty matches on error rather than throwing
        }

        return response;
    }

    public async Task<RoomMatchResponse> MatchRoomProductsAsync(RoomMatchRequest request, Guid companyId)
    {
        _logger.LogInformation("Matching {Count} products in room {RoomName} for company {CompanyId}",
            request.Products.Count, request.RoomName, companyId);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = new RoomMatchResponse
        {
            RoomName = request.RoomName,
            TotalProducts = request.Products.Count
        };

        if (request.Products.Count == 0)
        {
            _logger.LogWarning("No products provided in room match request");
            return response;
        }

        try
        {
            var bigQueryClient = await BigQueryClient.CreateAsync(_settings.ProjectId, _credential);

            // Extract legacy product names
            var legacyProductNames = request.Products.Select(p => p.LegacyProductName).ToList();

            // Build BigQuery SQL with UNNEST to process all products at once
            var sql = $@"
WITH legacy_products AS (
  SELECT
    product_name,
    embedding AS legacy_embedding
  FROM UNNEST(@legacyProductNames) AS product_name
  LEFT JOIN `{_settings.ProjectId}.snapdragon.master_product_repository_embeddings` AS legacy
    ON LOWER(TRIM(legacy.ProductName)) = LOWER(TRIM(product_name))
  WHERE embedding IS NOT NULL
),
matches AS (
  SELECT
    lp.product_name AS legacy_product_name,
    cp.product_id AS current_product_id,
    cp.product_name AS current_product_name,
    cp.part_number,
    cp.default_price,
    cp.category,
    cp.manufacturer,
    ML.DISTANCE(lp.legacy_embedding, cp.embedding, 'COSINE') AS distance,
    (1 - ML.DISTANCE(lp.legacy_embedding, cp.embedding, 'COSINE')) AS confidence,
    ROW_NUMBER() OVER (
      PARTITION BY lp.product_name
      ORDER BY ML.DISTANCE(lp.legacy_embedding, cp.embedding, 'COSINE')
    ) AS rank
  FROM legacy_products lp
  CROSS JOIN `{_settings.ProjectId}.snapdragon_data.ProductList` cp
  WHERE cp.company_id = @companyId
    AND cp.is_active = TRUE
    AND cp.embedding IS NOT NULL
)
SELECT
  legacy_product_name,
  current_product_id,
  current_product_name,
  part_number,
  default_price,
  category,
  manufacturer,
  confidence
FROM matches
WHERE rank <= @maxMatchesPerProduct
  AND confidence >= @minConfidence
ORDER BY legacy_product_name, rank";

            // Create query parameters
            var parameters = new[]
            {
                new BigQueryParameter("legacyProductNames", BigQueryDbType.Array, legacyProductNames.ToArray()),
                new BigQueryParameter("companyId", BigQueryDbType.String, companyId.ToString()),
                new BigQueryParameter("maxMatchesPerProduct", BigQueryDbType.Int64, request.MaxMatchesPerProduct),
                new BigQueryParameter("minConfidence", BigQueryDbType.Float64, request.MinConfidence)
            };

            _logger.LogInformation("Executing BigQuery room product matching query");

            var queryResult = await bigQueryClient.ExecuteQueryAsync(sql, parameters);

            // Group BigQuery results by legacy product name
            var queryResults = new List<(string LegacyProductName, string CurrentProductId, string CurrentProductName,
                string PartNumber, double? DefaultPrice, string? Category, string? Manufacturer, double Confidence)>();

            foreach (var row in queryResult)
            {
                queryResults.Add((
                    LegacyProductName: row["legacy_product_name"]?.ToString() ?? "",
                    CurrentProductId: row["current_product_id"]?.ToString() ?? "",
                    CurrentProductName: row["current_product_name"]?.ToString() ?? "",
                    PartNumber: row["part_number"]?.ToString() ?? "",
                    DefaultPrice: row["default_price"] != null ? Convert.ToDouble(row["default_price"]) : null,
                    Category: row["category"]?.ToString(),
                    Manufacturer: row["manufacturer"]?.ToString(),
                    Confidence: row["confidence"] != null ? Convert.ToDouble(row["confidence"]) : 0.0
                ));
            }

            var productGroups = queryResults
                .GroupBy(r => r.LegacyProductName)
                .Select(g => new ProductMatchGroup
                {
                    LegacyProductName = g.Key,
                    Quantity = request.Products.First(p => p.LegacyProductName == g.Key).Quantity,
                    LegacyPrice = request.Products.First(p => p.LegacyProductName == g.Key).LegacyPrice,
                    CurrentMatches = g.Select(m => new ProductMatch
                    {
                        ProductId = m.CurrentProductId,
                        ProductName = m.CurrentProductName,
                        PartNumber = m.PartNumber,
                        Price = m.DefaultPrice,
                        ConfidenceScore = m.Confidence,
                        Category = m.Category,
                        Manufacturer = m.Manufacturer
                    }).ToList()
                })
                .ToList();

            response.ProductMatches = productGroups;
            response.MatchedProducts = productGroups.Count(g => g.CurrentMatches.Any());
            stopwatch.Stop();
            response.QueryTimeMs = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("Matched {MatchedCount}/{TotalCount} products in {ElapsedMs}ms",
                response.MatchedProducts, response.TotalProducts, response.QueryTimeMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error matching room products");
            stopwatch.Stop();
            response.QueryTimeMs = stopwatch.ElapsedMilliseconds;
            // Return partial results on error
        }

        return response;
    }

    private static void CalculateBidTotals(Bid bid)
    {
        foreach (var room in bid.Rooms)
        {
            foreach (var eq in room.Equipment)
            {
                var baseAmount = eq.Quantity * eq.Price;
                var serviceBefore = baseAmount * eq.ServicePercentBeforeDiscount;
                var afterDiscount = (baseAmount + serviceBefore) * (1 - eq.Discount);
                var serviceAfter = afterDiscount * eq.ServicePercentAfterDiscount;
                eq.LineTotal = Math.Round(afterDiscount + serviceAfter + eq.SubrentalFee, 2);
            }
            room.Subtotal = Math.Round(room.Equipment.Sum(e => e.LineTotal), 2);
        }
        bid.GrandTotal = Math.Round(bid.Rooms.Sum(r => r.Subtotal), 2);
    }
}
