using Google.Cloud.BigQuery.V2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapdragonApi.Models;

namespace SnapdragonApi.Services;

public interface IBigQueryService
{
    Task SyncProductsAsync(Guid companyId);
}

public class BigQueryService : IBigQueryService
{
    private readonly AppDbContext _db;
    private readonly GoogleCloudSettings _cloudSettings;
    private readonly ILogger<BigQueryService> _logger;

    public BigQueryService(
        AppDbContext db,
        IOptions<GoogleCloudSettings> cloudSettings,
        ILogger<BigQueryService> logger)
    {
        _db = db;
        _cloudSettings = cloudSettings.Value;
        _logger = logger;
    }

    public async Task SyncProductsAsync(Guid companyId)
    {
        _logger.LogInformation("Starting product sync to BigQuery for company {CompanyId}", companyId);

        var client = await BigQueryClient.CreateAsync(_cloudSettings.ProjectId);
        var datasetId = "snapdragon_data";
        var tableId = "ProductList";

        // Ensure dataset exists
        var dataset = await client.GetOrCreateDatasetAsync(datasetId);

        // Define table schema
        var schema = new TableSchemaBuilder
        {
            { "product_id", BigQueryDbType.String },
            { "company_id", BigQueryDbType.String },
            { "product_name", BigQueryDbType.String },
            { "friendly_name", BigQueryDbType.String },
            { "part_number", BigQueryDbType.String },
            { "description", BigQueryDbType.String },
            { "category", BigQueryDbType.String },
            { "subcategory", BigQueryDbType.String },
            { "product_type", BigQueryDbType.String },
            { "manufacturer", BigQueryDbType.String },
            { "default_price", BigQueryDbType.Float64 },
            { "default_cost", BigQueryDbType.Float64 },
            { "price_level_a", BigQueryDbType.Float64 },
            { "price_level_b", BigQueryDbType.Float64 },
            { "price_level_c", BigQueryDbType.Float64 },
            { "price_level_d", BigQueryDbType.Float64 },
            { "price_level_e", BigQueryDbType.Float64 },
            { "taxable", BigQueryDbType.Bool },
            { "is_active", BigQueryDbType.Bool },
            { "sync_timestamp", BigQueryDbType.Timestamp }
        }.Build();

        // Get or create table
        var table = await dataset.GetOrCreateTableAsync(tableId, schema);

        // Fetch all active master products for the company
        var products = await _db.MasterProduct
            .Where(p => p.CompanyId == companyId && p.StatusId == 1)
            .ToListAsync();

        if (products.Count == 0)
        {
            _logger.LogWarning("No active products found for company {CompanyId}", companyId);
            return;
        }

        // Delete existing rows for this company
        var deleteQuery = $@"
            DELETE FROM `{_cloudSettings.ProjectId}.{datasetId}.{tableId}`
            WHERE company_id = '{companyId}'
        ";

        try
        {
            await client.ExecuteQueryAsync(deleteQuery, parameters: null);
            _logger.LogInformation("Deleted existing products for company {CompanyId}", companyId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error deleting existing products (table may be empty): {Message}", ex.Message);
        }

        // Prepare rows for insertion
        var rows = products.Select(p => new BigQueryInsertRow
        {
            ["product_id"] = p.Id.ToString(),
            ["company_id"] = p.CompanyId.ToString(),
            ["product_name"] = p.ProductName,
            ["friendly_name"] = p.FriendlyName ?? "",
            ["part_number"] = p.PartNumber ?? "",
            ["description"] = p.Description ?? "",
            ["category"] = p.Category ?? "",
            ["subcategory"] = p.Subcategory ?? "",
            ["product_type"] = p.ProductType ?? "",
            ["manufacturer"] = p.Manufacturer ?? "",
            ["default_price"] = (double?)p.DefaultPrice ?? 0.0,
            ["default_cost"] = (double?)p.DefaultCost ?? 0.0,
            ["price_level_a"] = (double?)p.PriceLevelA ?? 0.0,
            ["price_level_b"] = (double?)p.PriceLevelB ?? 0.0,
            ["price_level_c"] = (double?)p.PriceLevelC ?? 0.0,
            ["price_level_d"] = (double?)p.PriceLevelD ?? 0.0,
            ["price_level_e"] = (double?)p.PriceLevelE ?? 0.0,
            ["taxable"] = p.Taxable,
            ["is_active"] = p.StatusId == 1,
            ["sync_timestamp"] = DateTime.UtcNow
        }).ToList();

        // Insert rows
        await table.InsertRowsAsync(rows);

        _logger.LogInformation("Synced {Count} products to BigQuery for company {CompanyId}", products.Count, companyId);
    }
}
