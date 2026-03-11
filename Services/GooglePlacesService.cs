// using Newtonsoft.Json;
//  using System.Text.Json;

// namespace SnapdragonApi.Services{
//     public class JobAddressDto
//     {
//         public string GooglePlaceID { get; set; }
//         public string FormattedAddress { get; set; }
//         public string StreetNumber { get; set; }
//         public string Route { get; set; }
//         public string Suite { get; set; }
//         public string Neighborhood { get; set; }
//         public string Locality { get; set; }
//         public string Sublocality { get; set; }
//         public string AdministrativeAreaLevel1 { get; set; }
//         public string AdministrativeAreaLevel2 { get; set; }
//         public string AdministrativeAreaLevel3 { get; set; }
//         public string Country { get; set; }
//         public string CountryCode { get; set; }
//         public string PostalCode { get; set; }
//         public string PostalCodeSuffix { get; set; }
//         public double Latitude { get; set; }
//         public double Longitude { get; set; }
//         public string LocationType { get; set; }
//         public string PlaceTypes { get; set; }
//     }

//     public class AddressSuggestionDto
//     {
//         public string? Street { get; set; }   // e.g. "405 Columbus Avenue"
//         public string? City { get; set; }   // e.g. "San Francisco"
//         public string? State { get; set; }   // e.g. "CA"
//         public string? Zip { get; set; }   // e.g. "94102" (may be empty if not found)
//         public double Latitude { get; set; }
//         public double Longitude { get; set; }
//     }

//     public class GooglePlacesModule
//     {
//         private record Prediction(  string Description, string Place_id);

//         public record PlaceSuggestion
//         {
//             public string Description { get; set; } = default!;
//             public string PlaceId { get; set; } = default!;
//         }

//         public record AutocompleteResponse
//         {
//             public List<PlaceSuggestion> Predictions { get; set; } = new();
//         }

//         // Static HttpClient to avoid socket exhaustion - reused across all requests
//         private static readonly HttpClient _httpClient = new HttpClient();

//         private readonly IConfiguration _config;
//         private record GooglePlacesAutocomplete( string Status, List<Prediction> Predictions);


//         private string apiKey = System.Environment.GetEnvironmentVariable("GOOGLE_API_KEY") ?? "";
//         private string placesUrl;

//         public static string detailUrl(string id, string apiKey)
//         {
//             return $"https://places.googleapis.com/v1/places/{Uri.EscapeDataString(id)}?fields=formattedAddress,addressComponents,postalAddress,location&key={apiKey}";
//         }

//         public async Task<AddressSuggestionDto> GetPlaceFromSuggestion(string input, string location, double? radius = null)
//         {

//             var suggestions = await GetSuggestions(input, location, radius);
//             if (suggestions == null || suggestions.Count == 0) return null;
//             var firstSuggestion = suggestions[0];
//             var placeDetails = await GetPlaceDetails(firstSuggestion.PlaceId);
//             return placeDetails;
//         }

//         public async Task<JobAddressDto> GetFullPlaceDetails(string input, string location, double? radius = null)
//         {
//             var suggestions = await GetSuggestions(input, location, radius);
//             if (suggestions == null || suggestions.Count == 0) return null;
//             var firstSuggestion = suggestions[0];
//             return await GetJobAddressDetails(firstSuggestion.PlaceId);
//         }

//         public async Task<JobAddressDto> GetJobAddressDetails(string placeId)
//         {
//             var url = $"https://places.googleapis.com/v1/places/{Uri.EscapeDataString(placeId)}?fields=id,formattedAddress,addressComponents,location,types&key={this.apiKey}";

//             var detailResp = await _httpClient.GetAsync(url);
//             if (!detailResp.IsSuccessStatusCode) return null;

//             using var detJson = await detailResp.Content.ReadAsStreamAsync();
//             using var detDoc = await JsonDocument.ParseAsync(detJson);

//             var root = detDoc.RootElement;

//             var dto = new JobAddressDto
//             {
//                 GooglePlaceID = placeId,
//                 FormattedAddress = root.TryGetProperty("formattedAddress", out var fa) ? fa.GetString() : null,
//                 StreetNumber = GetAddressComponent(root, "street_number"),
//                 Route = GetAddressComponent(root, "route"),
//                 Suite = GetAddressComponent(root, "subpremise"),
//                 Neighborhood = GetAddressComponent(root, "neighborhood"),
//                 Locality = GetAddressComponent(root, "locality"),
//                 Sublocality = GetAddressComponent(root, "sublocality"),
//                 AdministrativeAreaLevel1 = GetAddressComponent(root, "administrative_area_level_1"),
//                 AdministrativeAreaLevel2 = GetAddressComponent(root, "administrative_area_level_2"),
//                 AdministrativeAreaLevel3 = GetAddressComponent(root, "administrative_area_level_3"),
//                 Country = GetAddressComponent(root, "country"),
//                 PostalCode = GetAddressComponent(root, "postal_code"),
//                 PostalCodeSuffix = GetAddressComponent(root, "postal_code_suffix"),
//             };

//             // Get country code (short name)
//             if (root.TryGetProperty("addressComponents", out var components))
//             {
//                 foreach (var component in components.EnumerateArray())
//                 {
//                     if (!component.TryGetProperty("types", out var types))
//                         continue;
//                     foreach (var t in types.EnumerateArray())
//                     {
//                         if (t.GetString() == "country")
//                         {
//                             if (component.TryGetProperty("shortText", out var shortText))
//                                 dto.CountryCode = shortText.GetString();
//                             break;
//                         }
//                     }
//                 }
//             }

//             // Get lat/lng
//             if (root.TryGetProperty("location", out var loc) && loc.ValueKind == JsonValueKind.Object)
//             {
//                 dto.Latitude = loc.TryGetProperty("latitude", out var lat) ? lat.GetDouble() : 0;
//                 dto.Longitude = loc.TryGetProperty("longitude", out var lng) ? lng.GetDouble() : 0;
//             }

//             // Get place types
//             if (root.TryGetProperty("types", out var typesArray) && typesArray.ValueKind == JsonValueKind.Array)
//             {
//                 var typesList = new List<string>();
//                 foreach (var t in typesArray.EnumerateArray())
//                 {
//                     typesList.Add(t.GetString());
//                 }
//                 dto.PlaceTypes = string.Join(",", typesList);
//             }

//             return dto;
//         }



//         public GooglePlacesModule(IConfiguration config)
//         {

//             var c = config;

//             var g = c.GetRequiredSection("Google:GooglePlaces:PlacesAutocompleteUrl");
//             this.placesUrl = g.Value ?? "";
//             var k = c.GetRequiredSection("Google:GooglePlaces:ApiKey");
//             this.apiKey = k.Value ?? "";
//             _config = config;

//         }


//         public async Task<AddressSuggestionDto> GetPlaceDetails(string placeId)
//         {
//             var url = detailUrl(placeId, this.apiKey);

//             var detailResp = await _httpClient.GetAsync(url);
//             if (!detailResp.IsSuccessStatusCode) return null;

//             using var detJson = await detailResp.Content.ReadAsStreamAsync();
//             using var detDoc = await JsonDocument.ParseAsync(detJson);

//             var comp = detDoc.RootElement;
//             var city = GetAddressComponent(comp, "locality").ToUpper();
//             var state = GetAddressComponent(comp, "administrative_area_level_1").ToUpper();
//             var zip = GetAddressComponent(comp, "postal_code").ToUpper();
//             var streetName = GetAddressComponent(comp, "route").ToUpper();
//             var streetNumber = GetAddressComponent(comp, "street_number").ToUpper();

//             if (streetNumber.ToString().Length > 0) streetName = $"{streetNumber} {streetName}";

//             var suggestion = new AddressSuggestionDto
//             {
//                 Street = streetName,
//                 City = city,
//                 State = state,
//                 Zip = zip,
//                 Latitude = double.Parse(GetAddressComponent(comp, "latitude")),
//                 Longitude = double.Parse(GetAddressComponent(comp, "longitude"))
//             };

//             return suggestion;
//         }

//         public async Task<List<PlaceSuggestion>> GetSuggestions(string input, string location, double? radius = null)
//         {
//             // Build Google request URL
//             var url = $"{this.placesUrl}?input={Uri.EscapeDataString(input)}&components=country:us&key={this.apiKey}";

//             if (!string.IsNullOrWhiteSpace(location))
//                 url += $"&location={location}";
//             if (radius != null)
//                 url += $"&radius={radius.Value}";

//             var r = await _httpClient.GetAsync(url);
//             var json = await r.Content.ReadAsStringAsync();

//             System.Diagnostics.Debug.WriteLine($"######## Google Autocomplete Response: {json}");

//             GooglePlacesAutocomplete googleResponse = JsonConvert.DeserializeObject<GooglePlacesAutocomplete>(json);

//             if (googleResponse == null || googleResponse.Predictions == null || googleResponse.Predictions.Count == 0)
//                 return null;

//             // Map Google response to our DTO
//             var result = new AutocompleteResponse();
//             foreach (var p in googleResponse.Predictions)
//             {
//                 result.Predictions.Add(new PlaceSuggestion
//                 {
//                     Description = p.Description,
//                     PlaceId = p.Place_id
//                 });
//             }

//             return result.Predictions;
//         }


         

//         private string GetAddressComponent(JsonElement root, string type)
//         {
//             // Prefer the structured "location" object
//             if (root.TryGetProperty("location", out var lo) && lo.ValueKind == JsonValueKind.Object)
//             {
//                 string latitude = lo.TryGetProperty("latitude", out var lat) && lat.ValueKind == JsonValueKind.Number ? lat.GetDouble().ToString() : "0";

//                 string longitude = lo.TryGetProperty("longitude", out var lon) && lon.ValueKind == JsonValueKind.Number ? lon.GetDouble().ToString() : "0";
//                 if (type == "latitude") return latitude;
//                 if (type == "longitude") return longitude;

//             }

//             // Safely check for addressComponents
//             if (!root.TryGetProperty("addressComponents", out var components))
//                 return "";

//             // components is the "address_components" array from the Place Details response.
//             foreach (var component in components.EnumerateArray())
//             {
//                 // component["types"] is an array of strings.
//                 if (!component.TryGetProperty("types", out var types))
//                     continue;
//                 foreach (var t in types.EnumerateArray())
//                 {
//                     if (t.GetString() == type)
//                     {
//                         // Found the matching type – return its long_name.
//                         if (component.TryGetProperty("longText", out var longNameElem))
//                             return longNameElem.GetString();
//                         else
//                             return "";
//                     }
//                 }
//             }

//             // No component with the requested type was found.
//             return "";
//         }

//     }
// }
