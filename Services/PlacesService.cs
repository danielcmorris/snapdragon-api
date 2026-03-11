using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using SnapdragonApi.DTOs;
using SnapdragonApi.Models;

namespace SnapdragonApi.Services;

public interface IPlacesService
{
    Task<CompanyLookupResponse> LookupCompanyAsync(string name);
    Task<AddressValidationResponse> ValidateAddressAsync(string query);
}

public class PlacesService : IPlacesService
{
    private readonly string _apiKey;
    private readonly ILogger<PlacesService> _logger;

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

    private const string SearchTextUrl = "https://places.googleapis.com/v1/places:searchText";
    private const string CompanyFieldMask = "places.displayName,places.formattedAddress,places.addressComponents,places.nationalPhoneNumber,places.websiteUri";
    private const string AddressFieldMask = "places.formattedAddress,places.addressComponents";

    public PlacesService(IOptions<GoogleCloudSettings> settings, ILogger<PlacesService> logger)
    {
        _logger = logger;
        _apiKey = settings.Value.PlacesApiKey;
    }

    public async Task<CompanyLookupResponse> LookupCompanyAsync(string name)
    {
        try
        {
            var query = IsDomainInput(name) ? DomainToSearchText(name) : name;
            var places = await SearchTextAsync(query, CompanyFieldMask);

            if (places == null || places.Count == 0)
                return new CompanyLookupResponse { Found = false };

            var place = places[0];
            var (street, city, state, zip, country) = ParseAddressComponents(place);

            return new CompanyLookupResponse
            {
                Found = true,
                CompanyName = place["displayName"]?["text"]?.GetValue<string>(),
                Address = street,
                City = city,
                State = state,
                ZipCode = zip,
                Country = country,
                Phone = place["nationalPhoneNumber"]?.GetValue<string>(),
                Website = place["websiteUri"]?.GetValue<string>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Places API lookup failed for '{Name}'", name);
            return new CompanyLookupResponse { Found = false };
        }
    }

    public async Task<AddressValidationResponse> ValidateAddressAsync(string query)
    {
        try
        {
            var places = await SearchTextAsync(query, AddressFieldMask);

            if (places == null || places.Count == 0)
                return new AddressValidationResponse { Valid = false };

            var place = places[0];
            var (street, city, state, zip, country) = ParseAddressComponents(place);

            return new AddressValidationResponse
            {
                Valid = true,
                Address = street,
                City = city,
                State = state,
                ZipCode = zip,
                Country = country,
                FormattedAddress = place["formattedAddress"]?.GetValue<string>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Address validation failed for '{Query}'", query);
            return new AddressValidationResponse { Valid = false };
        }
    }

    private async Task<JsonArray?> SearchTextAsync(string textQuery, string fieldMask)
    {
        var body = JsonSerializer.Serialize(new { textQuery });
        var req = new HttpRequestMessage(HttpMethod.Post, SearchTextUrl)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        req.Headers.Add("X-Goog-Api-Key", _apiKey);
        req.Headers.Add("X-Goog-FieldMask", fieldMask);

        var resp = await _http.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var json = JsonNode.Parse(await resp.Content.ReadAsStringAsync());
        return json?["places"]?.AsArray();
    }

    private static (string? street, string? city, string? state, string? zip, string? country)
        ParseAddressComponents(JsonNode? place)
    {
        string? streetNumber = null, route = null, city = null, state = null, zip = null, country = null;

        var components = place?["addressComponents"]?.AsArray();
        if (components != null)
        {
            foreach (var c in components)
            {
                var types = c?["types"]?.AsArray();
                if (types == null) continue;
                var typeList = types.Select(t => t?.GetValue<string>() ?? "").ToList();

                if (typeList.Contains("street_number"))
                    streetNumber = c?["longText"]?.GetValue<string>();
                else if (typeList.Contains("route"))
                    route = c?["longText"]?.GetValue<string>();
                else if (typeList.Contains("locality"))
                    city = c?["longText"]?.GetValue<string>();
                else if (typeList.Contains("administrative_area_level_1"))
                    state = c?["shortText"]?.GetValue<string>();
                else if (typeList.Contains("postal_code"))
                    zip = c?["longText"]?.GetValue<string>();
                else if (typeList.Contains("country"))
                    country = c?["longText"]?.GetValue<string>();
            }
        }

        var street = streetNumber != null && route != null
            ? $"{streetNumber} {route}"
            : route ?? streetNumber;

        return (street, city, state, zip, country);
    }

    private static bool IsDomainInput(string input) =>
        input.Contains('.') && !input.Contains(' ');

    private static string DomainToSearchText(string domain)
    {
        var name = domain
            .Replace("www.", "")
            .Replace("http://", "")
            .Replace("https://", "");

        var dotIndex = name.LastIndexOf('.');
        if (dotIndex > 0)
            name = name[..dotIndex];

        return name.Replace('.', ' ').Replace('-', ' ');
    }
}
