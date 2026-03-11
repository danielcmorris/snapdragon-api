using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using SnapdragonApi.DTOs;
using SnapdragonApi.Models;

namespace SnapdragonApi.Services;

public interface IInterrogatorService
{
    EventSetup GetEmptyEventSetup();
    Task<InteractiveSetupResponse> ProcessInteractiveSetupAsync(InteractiveSetupRequest request);
    Task<InteractiveClientSetupResponse> ProcessInteractiveClientSetupAsync(InteractiveClientSetupRequest request);
}

public class InterrogatorService : IInterrogatorService
{
    private readonly VertexAiSettings _settings;
    private readonly GoogleCloudSettings _cloudSettings;
    private readonly ILogger<InterrogatorService> _logger;

    private readonly HttpClient _http = new(new SocketsHttpHandler
    {
        ConnectCallback = async (ctx, ct) =>
        {
            var addresses = await Dns.GetHostAddressesAsync(ctx.DnsEndPoint.Host, ct);
            var ipv4 = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
                       ?? addresses.First();
            var socket = new Socket(ipv4.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            await socket.ConnectAsync(new IPEndPoint(ipv4, ctx.DnsEndPoint.Port), ct);
            return new NetworkStream(socket, ownsSocket: true);
        }
    });
    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    private static readonly List<string> RequiredFields = new()
    {
        nameof(EventSetup.ClientName),
        nameof(EventSetup.ClientEmail),
        nameof(EventSetup.EventName),
        nameof(EventSetup.EventType),
        nameof(EventSetup.VenueAddress),
        nameof(EventSetup.City),
        nameof(EventSetup.State),
        nameof(EventSetup.EstimatedAttendees),
        nameof(EventSetup.EventDate),
        nameof(EventSetup.ServicesRequested)
    };

    public InterrogatorService(
        IOptions<VertexAiSettings> settings,
        IOptions<GoogleCloudSettings> cloudSettings,
        ILogger<InterrogatorService> logger)
    {
        _settings = settings.Value;
        _cloudSettings = cloudSettings.Value;
        _logger = logger;
    }

    private async Task<string> GetAccessTokenAsync()
    {
        if (_cachedToken != null && DateTime.UtcNow < _tokenExpiry)
            return _cachedToken;

        var credPath = string.IsNullOrEmpty(_cloudSettings.CredentialPath)
            ? Path.Combine(AppContext.BaseDirectory, "../credentials/snapdragonerp-178c64451dd6.json")
            : _cloudSettings.CredentialPath;

        var cred = JsonNode.Parse(await File.ReadAllTextAsync(credPath))!;
        var clientEmail = cred["client_email"]!.GetValue<string>();
        var privateKeyPem = cred["private_key"]!.GetValue<string>();

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var header = Base64UrlEncode(JsonSerializer.Serialize(new { alg = "RS256", typ = "JWT" }));
        var claims = Base64UrlEncode(JsonSerializer.Serialize(new
        {
            iss = clientEmail,
            scope = "https://www.googleapis.com/auth/cloud-platform",
            aud = "https://oauth2.googleapis.com/token",
            exp = now + 3600,
            iat = now
        }));

        var signingInput = $"{header}.{claims}";
        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
        var signature = Base64UrlEncode(rsa.SignData(Encoding.UTF8.GetBytes(signingInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        var jwt = $"{signingInput}.{signature}";

        var tokenResp = await _http.PostAsync("https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                ["assertion"] = jwt
            }));
        tokenResp.EnsureSuccessStatusCode();
        var tokenJson = JsonNode.Parse(await tokenResp.Content.ReadAsStringAsync())!;
        _cachedToken = tokenJson["access_token"]!.GetValue<string>();
        _tokenExpiry = DateTime.UtcNow.AddMinutes(55);
        return _cachedToken;
    }

    private static string Base64UrlEncode(string text) =>
        Base64UrlEncode(Encoding.UTF8.GetBytes(text));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private async Task<string> CallGeminiAsync(string prompt)
    {
        _logger.LogInformation("CallGeminiAsync: acquiring token");
        var token = await GetAccessTokenAsync();
        _logger.LogInformation("CallGeminiAsync: token acquired, calling REST endpoint");
        var url = $"https://{_settings.Location}-aiplatform.googleapis.com/v1/projects/{_settings.ProjectId}/locations/{_settings.Location}/publishers/google/models/{_settings.ModelId}:generateContent";

        var body = new
        {
            contents = new[] { new { role = "user", parts = new[] { new { text = prompt } } } },
            generationConfig = new { temperature = 0.1, maxOutputTokens = 2048 }
        };

        var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        _logger.LogInformation("CallGeminiAsync: sending HTTP request");
        var resp = await _http.SendAsync(req);
        _logger.LogInformation("CallGeminiAsync: got response {Status}", resp.StatusCode);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync();
        var node = System.Text.Json.Nodes.JsonNode.Parse(json);
        var text = node?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>() ?? "{}";

        text = text.Trim();
        if (text.StartsWith("```"))
        {
            text = text.Substring(text.IndexOf('\n') + 1);
            if (text.EndsWith("```"))
                text = text.Substring(0, text.LastIndexOf("```"));
        }
        return text.Trim();
    }

    public EventSetup GetEmptyEventSetup()
    {
        return new EventSetup();
    }

    public async Task<InteractiveSetupResponse> ProcessInteractiveSetupAsync(InteractiveSetupRequest request)
    {
        _logger.LogInformation("Processing interactive setup text");

        var existingSetup = request.ExistingSetup ?? new EventSetup();

        try
        {
            var existingJson = JsonSerializer.Serialize(existingSetup, new JsonSerializerOptions { WriteIndented = true });

            var contextHint = !string.IsNullOrWhiteSpace(request.PreviousQuestion)
                ? $@"
IMPORTANT CONTEXT: The user was just asked ""{request.PreviousQuestion}"" and their response is: ""{request.UserText}""
If the user's text appears to be a direct answer to this question, extract it into the appropriate field.
For example:
- If asked ""What is the client's name?"" and user says ""John Smith"", set clientName to ""John Smith""
- If asked ""What is the client's email?"" and user says ""john@example.com"", set clientEmail to ""john@example.com""
- If asked ""How many people?"" and user says ""50"", set estimatedAttendees to 50
"
                : "";

            var prompt = $@"You are an assistant that extracts event information from text.
Given the following user text, extract any event-related information and fill in the JSON structure below.
Merge any new information with the existing data (do not overwrite existing non-null values unless the user explicitly corrects them).
When a user mentions a well-known venue by name (e.g. ""Hilton Hotel, SF""), use your knowledge to fill in the full street address, city, state, and zip code. If multiple locations exist, use the most prominent one and note the ambiguity in the notes field.
{contextHint}
User text: ""{request.UserText}""

Existing event setup:
{existingJson}

Return ONLY a valid JSON object matching this exact structure (no markdown, no explanation):
{{
  ""clientName"": string or null,
  ""clientEmail"": string or null,
  ""clientPhone"": string or null,
  ""clientCompany"": string or null,
  ""eventName"": string or null,
  ""eventType"": string or null,
  ""venueName"": string or null,
  ""venueAddress"": string or null,
  ""city"": string or null,
  ""state"": string or null,
  ""zipCode"": string or null,
  ""estimatedAttendees"": number or null,
  ""eventDate"": ""yyyy-MM-dd"" or null,
  ""eventEndDate"": ""yyyy-MM-dd"" or null,
  ""startTime"": string or null,
  ""endTime"": string or null,
  ""setupTime"": string or null,
  ""teardownTime"": string or null,
  ""indoorOutdoor"": boolean or null,
  ""budgetRange"": string or null,
  ""servicesRequested"": [string] or null,
  ""specialRequirements"": string or null,
  ""notes"": string or null
}}";

            var responseText = await CallGeminiAsync(prompt);

            var parsedSetup = JsonSerializer.Deserialize<EventSetup>(responseText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? existingSetup;

            var (completeness, missing) = CalculateCompleteness(parsedSetup);

            return new InteractiveSetupResponse
            {
                EventSetup = parsedSetup,
                CompletenessPercentage = completeness,
                MissingFields = missing,
                SuggestedNextQuestion = GenerateSuggestedQuestion(missing)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing interactive setup");
            var (completeness, missing) = CalculateCompleteness(existingSetup);
            return new InteractiveSetupResponse
            {
                EventSetup = existingSetup,
                CompletenessPercentage = completeness,
                MissingFields = missing,
                SuggestedNextQuestion = GenerateSuggestedQuestion(missing)
            };
        }
    }

    private (double completeness, List<string> missing) CalculateCompleteness(EventSetup setup)
    {
        var missing = new List<string>();
        var type = typeof(EventSetup);

        foreach (var fieldName in RequiredFields)
        {
            var prop = type.GetProperty(fieldName);
            if (prop == null) continue;

            var value = prop.GetValue(setup);
            if (value == null ||
                (value is string s && string.IsNullOrWhiteSpace(s)) ||
                (value is List<string> list && list.Count == 0))
            {
                missing.Add(fieldName);
            }
        }

        var filledCount = RequiredFields.Count - missing.Count;
        var completeness = Math.Round((double)filledCount / RequiredFields.Count * 100, 1);

        return (completeness, missing);
    }

    private string? GenerateSuggestedQuestion(List<string> missingFields)
    {
        if (missingFields.Count == 0) return null;

        var fieldQuestions = new Dictionary<string, string>
        {
            { nameof(EventSetup.ClientName), "What is the client's name?" },
            { nameof(EventSetup.ClientEmail), "What is the client's email address?" },
            { nameof(EventSetup.EventName), "What would you like to call this event?" },
            { nameof(EventSetup.EventType), "What type of event is this (wedding, corporate, party, etc.)?" },
            { nameof(EventSetup.VenueAddress), "What is the venue address?" },
            { nameof(EventSetup.City), "What city is the event in?" },
            { nameof(EventSetup.State), "What state is the event in?" },
            { nameof(EventSetup.EstimatedAttendees), "How many people do you expect to attend?" },
            { nameof(EventSetup.EventDate), "When is the event date?" },
            { nameof(EventSetup.ServicesRequested), "What services are needed for this event?" }
        };

        var firstMissing = missingFields.FirstOrDefault();
        if (firstMissing != null && fieldQuestions.TryGetValue(firstMissing, out var question))
            return question;

        return "Could you provide more details about the event?";
    }

    // ── Client Setup ─────────────────────────────────────────────────────────

    private static readonly List<string> ClientRequiredFields = new()
    {
        nameof(ClientSetup.Name),
        nameof(ClientSetup.PrimaryEmail),
        nameof(ClientSetup.PrimaryPhone),
        nameof(ClientSetup.AccountManager),
        nameof(ClientSetup.InstallationAddress1),
        nameof(ClientSetup.InstallationCity),
        nameof(ClientSetup.InstallationState),
    };

    // Contact name counts as one field — either first or last satisfies it
    private const string ContactNameVirtualField = "ContactName";

    public async Task<InteractiveClientSetupResponse> ProcessInteractiveClientSetupAsync(InteractiveClientSetupRequest request)
    {
        _logger.LogInformation("Processing interactive client setup text");

        var existingSetup = request.ExistingSetup ?? new ClientSetup();

        try
        {
            var existingJson = JsonSerializer.Serialize(existingSetup, new JsonSerializerOptions { WriteIndented = true });

            var contextHint = !string.IsNullOrWhiteSpace(request.PreviousQuestion)
                ? $@"
IMPORTANT CONTEXT: The user was just asked ""{request.PreviousQuestion}"" and their response is: ""{request.UserText}""
If the user's text appears to be a direct answer to this question, extract it into the appropriate field.
"
                : "";

            var prompt = $@"You are an assistant that extracts client/company information from text.
Given the following user text, extract any client-related information and fill in the JSON structure below.
Merge any new information with the existing data (do not overwrite existing non-null values unless the user explicitly corrects them).
{contextHint}
User text: ""{request.UserText}""

Existing client setup:
{existingJson}

Return ONLY a valid JSON object matching this exact structure (no markdown, no explanation):
{{
  ""name"": string or null,
  ""primaryContactFirstName"": string or null,
  ""primaryContactLastName"": string or null,
  ""primaryEmail"": string or null,
  ""primaryPhone"": string or null,
  ""cellPhone"": string or null,
  ""accountManager"": string or null,
  ""installationAddressTitle"": string or null,
  ""installationAddress1"": string or null,
  ""installationAddress2"": string or null,
  ""installationCity"": string or null,
  ""installationState"": string or null,
  ""installationZip"": string or null,
  ""notes"": string or null
}}";

            var responseText = await CallGeminiAsync(prompt);

            var parsedSetup = JsonSerializer.Deserialize<ClientSetup>(responseText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? existingSetup;

            var (completeness, missing) = CalculateClientCompleteness(parsedSetup);

            return new InteractiveClientSetupResponse
            {
                ClientSetup = parsedSetup,
                CompletenessPercentage = completeness,
                MissingFields = missing,
                SuggestedNextQuestion = GenerateClientSuggestedQuestion(missing)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing interactive client setup");
            var (completeness, missing) = CalculateClientCompleteness(existingSetup);
            return new InteractiveClientSetupResponse
            {
                ClientSetup = existingSetup,
                CompletenessPercentage = completeness,
                MissingFields = missing,
                SuggestedNextQuestion = GenerateClientSuggestedQuestion(missing)
            };
        }
    }

    private (double completeness, List<string> missing) CalculateClientCompleteness(ClientSetup setup)
    {
        var missing = new List<string>();
        var type = typeof(ClientSetup);
        var totalFields = ClientRequiredFields.Count + 1; // +1 for contact name virtual field

        // Check contact name (either first or last satisfies it)
        var hasContactName = !string.IsNullOrWhiteSpace(setup.PrimaryContactFirstName) ||
                             !string.IsNullOrWhiteSpace(setup.PrimaryContactLastName);
        if (!hasContactName)
            missing.Add(ContactNameVirtualField);

        foreach (var fieldName in ClientRequiredFields)
        {
            var prop = type.GetProperty(fieldName);
            if (prop == null) continue;

            var value = prop.GetValue(setup);
            if (value == null || (value is string s && string.IsNullOrWhiteSpace(s)))
                missing.Add(fieldName);
        }

        var filledCount = totalFields - missing.Count;
        var completeness = Math.Round((double)filledCount / totalFields * 100, 1);

        return (completeness, missing);
    }

    private string? GenerateClientSuggestedQuestion(List<string> missingFields)
    {
        if (missingFields.Count == 0) return null;

        var fieldQuestions = new Dictionary<string, string>
        {
            { nameof(ClientSetup.Name), "What is the company or client name?" },
            { ContactNameVirtualField, "Who is the primary contact (first and last name)?" },
            { nameof(ClientSetup.PrimaryEmail), "What is the primary email address?" },
            { nameof(ClientSetup.PrimaryPhone), "What is the primary phone number?" },
            { nameof(ClientSetup.AccountManager), "Who is the account manager for this client?" },
            { nameof(ClientSetup.InstallationAddress1), "What is the street address?" },
            { nameof(ClientSetup.InstallationCity), "What city are they located in?" },
            { nameof(ClientSetup.InstallationState), "What state are they located in?" },
        };

        var firstMissing = missingFields.FirstOrDefault();
        if (firstMissing != null && fieldQuestions.TryGetValue(firstMissing, out var question))
            return question;

        return "Can you provide any additional details about this client?";
    }
}
