using System.Security.Claims;
using Google.Cloud.BigQuery.V2;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SnapdragonApi.DTOs;
using SnapdragonApi.Models;
using SnapdragonApi.Services;

namespace SnapdragonApi.Controllers.Admin;

[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly GoogleCloudSettings _cloudSettings;
    private readonly IUserSessionService _sessionService;
    private readonly ILogger<AdminController> _logger;
    private readonly GoogleCredential _credential;

    public AdminController(
        IOptions<GoogleCloudSettings> cloudSettings,
        IUserSessionService sessionService,
        ILogger<AdminController> logger)
    {
        _cloudSettings = cloudSettings.Value;
        _sessionService = sessionService;
        _logger = logger;
        _credential = GoogleCredential.FromFile(_cloudSettings.CredentialPath);
    }

    [HttpPost("bigquery")]
    public async Task<ActionResult<BigQueryResponse>> ExecuteBigQuery([FromBody] BigQueryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Sql))
            return BadRequest("SQL query is required");

        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email))
            return Unauthorized("Unable to identify user");

        var session = await _sessionService.GetOrLoadAsync(email);

        // Require Admin (3) or Sysadmin (4) level
        if (session.UserLevel < 3)
        {
            _logger.LogWarning(
                "Unauthorized BigQuery access attempt by {Email} (UserLevel: {UserLevel})",
                email,
                session.UserLevel
            );
            return StatusCode(403, new { error = "Insufficient permissions. Admin role or higher required." });
        }

        _logger.LogInformation(
            "BigQuery execution by {Email} (UserLevel: {UserLevel}): {Sql}",
            email,
            session.UserLevel,
            request.Sql.Length > 200 ? request.Sql.Substring(0, 200) + "..." : request.Sql
        );

        try
        {
            var client = await BigQueryClient.CreateAsync(_cloudSettings.ProjectId, _credential);

            var queryOptions = new QueryOptions
            {
                UseQueryCache = request.UseCache ?? true
            };

            BigQueryResults results;

            if (request.Parameters != null && request.Parameters.Count > 0)
            {
                var parameters = request.Parameters.Select(p =>
                    new BigQueryParameter(p.Name, ParseBigQueryType(p.Type), p.Value)
                ).ToArray();

                results = await client.ExecuteQueryAsync(request.Sql, parameters, queryOptions);
            }
            else
            {
                results = await client.ExecuteQueryAsync(request.Sql, parameters: null, queryOptions);
            }

            var response = new BigQueryResponse
            {
                Success = true,
                Rows = new List<Dictionary<string, object?>>()
            };

            // Convert rows to JSON-friendly format
            foreach (var row in results)
            {
                var rowDict = new Dictionary<string, object?>();

                foreach (var field in results.Schema.Fields)
                {
                    var value = row[field.Name];
                    rowDict[field.Name] = ConvertBigQueryValue(value);
                }

                response.Rows.Add(rowDict);
            }

            response.RowCount = response.Rows.Count;
            response.TotalRows = results.TotalRows;

            _logger.LogInformation(
                "BigQuery execution successful: {RowCount} rows returned",
                response.RowCount
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BigQuery execution failed: {Message}", ex.Message);

            return Ok(new BigQueryResponse
            {
                Success = false,
                Error = ex.Message,
                Rows = new List<Dictionary<string, object?>>()
            });
        }
    }

    private static BigQueryDbType ParseBigQueryType(string type)
    {
        return type.ToUpperInvariant() switch
        {
            "STRING" => BigQueryDbType.String,
            "INT64" => BigQueryDbType.Int64,
            "FLOAT64" => BigQueryDbType.Float64,
            "BOOL" => BigQueryDbType.Bool,
            "TIMESTAMP" => BigQueryDbType.Timestamp,
            "DATE" => BigQueryDbType.Date,
            "DATETIME" => BigQueryDbType.DateTime,
            "TIME" => BigQueryDbType.Time,
            "BYTES" => BigQueryDbType.Bytes,
            "NUMERIC" => BigQueryDbType.Numeric,
            "BIGNUMERIC" => BigQueryDbType.BigNumeric,
            "GEOGRAPHY" => BigQueryDbType.Geography,
            "JSON" => BigQueryDbType.Json,
            _ => BigQueryDbType.String
        };
    }

    private static object? ConvertBigQueryValue(object? value)
    {
        if (value == null)
            return null;

        // Handle arrays
        if (value is System.Collections.IEnumerable enumerable and not string)
        {
            var list = new List<object?>();
            foreach (var item in enumerable)
            {
                list.Add(ConvertBigQueryValue(item));
            }
            return list;
        }

        // Handle special types
        return value switch
        {
            DateTime dt => dt.ToString("O"), // ISO 8601 format
            DateTimeOffset dto => dto.ToString("O"),
            byte[] bytes => Convert.ToBase64String(bytes),
            _ => value
        };
    }
}
