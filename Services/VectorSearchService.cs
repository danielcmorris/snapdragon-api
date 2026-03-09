using Google.Cloud.BigQuery.V2;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;
using SnapdragonApi.DTOs;
using SnapdragonApi.Models;

namespace SnapdragonApi.Services;

public interface IVectorSearchService
{
    Task<MatchProductResponse> SearchProductsAsync(string searchText, Guid companyId, int maxResults = 5);
    Task<EmbeddingStatusResponse> GetEmbeddingStatusAsync(Guid companyId);
}

public class VectorSearchService : IVectorSearchService
{
    private readonly GoogleCloudSettings _cloudSettings;
    private readonly ILogger<VectorSearchService> _logger;
    private readonly GoogleCredential _credential;

    public VectorSearchService(
        IOptions<GoogleCloudSettings> cloudSettings,
        ILogger<VectorSearchService> logger)
    {
        _cloudSettings = cloudSettings.Value;
        _logger = logger;
        _credential = string.IsNullOrEmpty(_cloudSettings.CredentialPath)
            ? GoogleCredential.GetApplicationDefault()
            : GoogleCredential.FromFile(_cloudSettings.CredentialPath);
    }

    public async Task<MatchProductResponse> SearchProductsAsync(string searchText, Guid companyId, int maxResults = 5)
    {
        _logger.LogInformation("Vector search for: {SearchText}, Company: {CompanyId}", searchText, companyId);

        var response = new MatchProductResponse();

        try
        {
            var client = await BigQueryClient.CreateAsync(_cloudSettings.ProjectId, _credential);

            // Use vector similarity search with embeddings
            var sql = $@"
                WITH search_embedding AS (
                    SELECT ml_generate_embedding_result as embedding
                    FROM ML.GENERATE_EMBEDDING(
                        MODEL `{_cloudSettings.ProjectId}.snapdragon_data.embedding_model`,
                        (SELECT @searchText as content)
                    )
                )
                SELECT
                    p.product_name,
                    p.friendly_name,
                    p.part_number,
                    p.category,
                    p.manufacturer,
                    p.description,
                    p.default_price,
                    `{_cloudSettings.ProjectId}.snapdragon_data.cosine_similarity`(
                        p.embedding,
                        s.embedding
                    ) as similarity_score
                FROM `{_cloudSettings.ProjectId}.snapdragon_data.ProductList` p
                CROSS JOIN search_embedding s
                WHERE p.company_id = @companyId
                  AND p.is_active = true
                  AND p.embedding IS NOT NULL
                  AND ARRAY_LENGTH(p.embedding) > 0
                ORDER BY similarity_score DESC
                LIMIT @maxResults
            ";

            var parameters = new[]
            {
                new BigQueryParameter("searchText", BigQueryDbType.String, searchText),
                new BigQueryParameter("companyId", BigQueryDbType.String, companyId.ToString()),
                new BigQueryParameter("maxResults", BigQueryDbType.Int64, maxResults)
            };

            _logger.LogInformation("Executing vector similarity search");

            var queryResults = await client.ExecuteQueryAsync(sql, parameters);

            var position = 0;
            foreach (var row in queryResults)
            {
                var similarityScore = row["similarity_score"] != null
                    ? Convert.ToDouble(row["similarity_score"])
                    : 0.0;

                // Only include results with similarity > 0.5 (50% match)
                if (similarityScore < 0.5)
                    continue;

                var match = new ProductMatch
                {
                    ProductName = row["product_name"]?.ToString() ?? row["friendly_name"]?.ToString() ?? "",
                    PartNumber = row["part_number"]?.ToString() ?? "",
                    Category = row["category"]?.ToString(),
                    Manufacturer = row["manufacturer"]?.ToString(),
                    Price = row["default_price"] != null ? Convert.ToDouble(row["default_price"]) : null,
                    ConfidenceScore = similarityScore, // Use actual AI similarity score
                    Description = row["description"]?.ToString()
                };

                response.Matches.Add(match);
                position++;
            }

            _logger.LogInformation("Found {Count} vector matches (similarity > 0.5)", response.Matches.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in vector search");

            // Fallback to keyword search if embeddings aren't set up
            _logger.LogWarning("Falling back to keyword search");
            return await FallbackKeywordSearchAsync(searchText, companyId, maxResults);
        }

        return response;
    }

    private async Task<MatchProductResponse> FallbackKeywordSearchAsync(string searchText, Guid companyId, int maxResults)
    {
        _logger.LogInformation("Fallback keyword search for: {SearchText}", searchText);

        var response = new MatchProductResponse();

        try
        {
            var client = await BigQueryClient.CreateAsync(_cloudSettings.ProjectId, _credential);

            // Build search terms for LIKE matching
            var searchTerms = searchText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var whereClauses = new List<string>();

            foreach (var term in searchTerms)
            {
                var escapedTerm = term.Replace("'", "''");
                whereClauses.Add(
                    $"(LOWER(product_name) LIKE LOWER('%{escapedTerm}%') OR " +
                    $"LOWER(friendly_name) LIKE LOWER('%{escapedTerm}%') OR " +
                    $"LOWER(part_number) LIKE LOWER('%{escapedTerm}%') OR " +
                    $"LOWER(description) LIKE LOWER('%{escapedTerm}%'))"
                );
            }

            var whereClause = string.Join(" AND ", whereClauses);

            var sql = $@"
                SELECT DISTINCT
                    product_name,
                    friendly_name,
                    part_number,
                    category,
                    manufacturer,
                    description,
                    default_price,
                    COUNT(*) OVER() as total_matches
                FROM `{_cloudSettings.ProjectId}.snapdragon_data.ProductList`
                WHERE company_id = '{companyId}'
                  AND is_active = true
                  AND ({whereClause})
                ORDER BY product_name
                LIMIT {maxResults}
            ";

            var queryResults = await client.ExecuteQueryAsync(sql, parameters: null);

            var position = 0;
            foreach (var row in queryResults)
            {
                // Use position-based confidence for keyword search
                var confidenceScore = Math.Max(0.5, 1.0 - (position * 0.1));

                var match = new ProductMatch
                {
                    ProductName = row["product_name"]?.ToString() ?? row["friendly_name"]?.ToString() ?? "",
                    PartNumber = row["part_number"]?.ToString() ?? "",
                    Category = row["category"]?.ToString(),
                    Manufacturer = row["manufacturer"]?.ToString(),
                    Price = row["default_price"] != null ? Convert.ToDouble(row["default_price"]) : null,
                    ConfidenceScore = confidenceScore,
                    Description = row["description"]?.ToString()
                };

                response.Matches.Add(match);
                position++;
            }

            _logger.LogInformation("Fallback search found {Count} matches", response.Matches.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in fallback keyword search");
        }

        return response;
    }

    public async Task<EmbeddingStatusResponse> GetEmbeddingStatusAsync(Guid companyId)
    {
        _logger.LogInformation("Checking embedding status for Company: {CompanyId}", companyId);

        var response = new EmbeddingStatusResponse();

        try
        {
            var client = await BigQueryClient.CreateAsync(_cloudSettings.ProjectId, _credential);

            var sql = $@"
                SELECT
                    COUNT(*) as total_products,
                    COUNTIF(embedding IS NOT NULL AND ARRAY_LENGTH(embedding) > 0) as with_embeddings,
                    COUNTIF(embedding IS NULL OR ARRAY_LENGTH(embedding) = 0) as without_embeddings
                FROM `{_cloudSettings.ProjectId}.snapdragon_data.ProductList`
                WHERE company_id = @companyId
            ";

            var parameters = new[]
            {
                new BigQueryParameter("companyId", BigQueryDbType.String, companyId.ToString())
            };

            var queryResults = await client.ExecuteQueryAsync(sql, parameters);

            foreach (var row in queryResults)
            {
                response.TotalProducts = row["total_products"] != null ? Convert.ToInt32(row["total_products"]) : 0;
                response.WithEmbeddings = row["with_embeddings"] != null ? Convert.ToInt32(row["with_embeddings"]) : 0;
                response.WithoutEmbeddings = row["without_embeddings"] != null ? Convert.ToInt32(row["without_embeddings"]) : 0;
                break; // Only one row expected
            }

            response.PercentComplete = response.TotalProducts > 0
                ? (int)Math.Round((double)response.WithEmbeddings / response.TotalProducts * 100)
                : 0;

            _logger.LogInformation(
                "Embedding status: {WithEmbeddings}/{TotalProducts} ({PercentComplete}%)",
                response.WithEmbeddings,
                response.TotalProducts,
                response.PercentComplete
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking embedding status");
            throw;
        }

        return response;
    }
}
