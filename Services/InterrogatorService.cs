using System.Reflection;
using System.Text.Json;
using Google.Cloud.AIPlatform.V1;
using Microsoft.Extensions.Options;
using SnapdragonApi.DTOs;
using SnapdragonApi.Models;

namespace SnapdragonApi.Services;

public interface IInterrogatorService
{
    EventSetup GetEmptyEventSetup();
    Task<InteractiveSetupResponse> ProcessInteractiveSetupAsync(InteractiveSetupRequest request);
}

public class InterrogatorService : IInterrogatorService
{
    private readonly VertexAiSettings _settings;
    private readonly GoogleCloudSettings _cloudSettings;
    private readonly ILogger<InterrogatorService> _logger;

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
            var clientBuilder = new PredictionServiceClientBuilder
            {
                CredentialsPath = _cloudSettings.CredentialPath,
                Endpoint = $"{_settings.Location}-aiplatform.googleapis.com"
            };
            var client = await clientBuilder.BuildAsync();

            var model = $"projects/{_settings.ProjectId}/locations/{_settings.Location}/publishers/google/models/{_settings.ModelId}";

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

            var response = await client.GenerateContentAsync(generateRequest);
            var responseText = response.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "{}";

            // Clean up any markdown code fences
            responseText = responseText.Trim();
            if (responseText.StartsWith("```"))
            {
                responseText = responseText.Substring(responseText.IndexOf('\n') + 1);
                if (responseText.EndsWith("```"))
                    responseText = responseText.Substring(0, responseText.LastIndexOf("```"));
            }

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
}
