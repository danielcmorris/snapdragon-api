using Google.Api.Gax.Grpc;
using Google.Maps.Places.V1;
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
    private readonly PlacesClient _client;
    private readonly ILogger<PlacesService> _logger;

    public PlacesService(IOptions<GoogleCloudSettings> settings, ILogger<PlacesService> logger)
    {
        _logger = logger;
        _client = new PlacesClientBuilder
        {
            ApiKey = settings.Value.PlacesApiKey
        }.Build();
    }

    public async Task<CompanyLookupResponse> LookupCompanyAsync(string name)
    {
        try
        {
            var query = IsDomainInput(name) ? DomainToSearchText(name) : name;

            var request = new SearchTextRequest
            {
                TextQuery = query
            };

            var fieldMask = "places.displayName,places.formattedAddress,places.addressComponents,places.nationalPhoneNumber,places.websiteUri";
            var callSettings = CallSettings.FromHeader("X-Goog-FieldMask", fieldMask);

            var response = await _client.SearchTextAsync(request, callSettings);

            if (response.Places == null || response.Places.Count == 0)
                return new CompanyLookupResponse { Found = false };

            var place = response.Places[0];
            var components = place.AddressComponents;

            string? streetNumber = null;
            string? route = null;
            string? city = null;
            string? state = null;
            string? zip = null;
            string? country = null;

            if (components != null)
            {
                foreach (var c in components)
                {
                    var types = c.Types_;
                    if (types.Contains("street_number"))
                        streetNumber = c.LongText;
                    else if (types.Contains("route"))
                        route = c.LongText;
                    else if (types.Contains("locality"))
                        city = c.LongText;
                    else if (types.Contains("administrative_area_level_1"))
                        state = c.ShortText;
                    else if (types.Contains("postal_code"))
                        zip = c.LongText;
                    else if (types.Contains("country"))
                        country = c.LongText;
                }
            }

            var street = streetNumber != null && route != null
                ? $"{streetNumber} {route}"
                : route ?? streetNumber;

            return new CompanyLookupResponse
            {
                Found = true,
                CompanyName = place.DisplayName?.Text,
                Address = street,
                City = city,
                State = state,
                ZipCode = zip,
                Country = country,
                Phone = place.NationalPhoneNumber,
                Website = place.WebsiteUri
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
            var request = new SearchTextRequest
            {
                TextQuery = query
            };

            var fieldMask = "places.formattedAddress,places.addressComponents";
            var callSettings = CallSettings.FromHeader("X-Goog-FieldMask", fieldMask);

            var response = await _client.SearchTextAsync(request, callSettings);

            if (response.Places == null || response.Places.Count == 0)
                return new AddressValidationResponse { Valid = false };

            var place = response.Places[0];
            var components = place.AddressComponents;

            string? streetNumber = null;
            string? route = null;
            string? city = null;
            string? state = null;
            string? zip = null;
            string? country = null;

            if (components != null)
            {
                foreach (var c in components)
                {
                    var types = c.Types_;
                    if (types.Contains("street_number"))
                        streetNumber = c.LongText;
                    else if (types.Contains("route"))
                        route = c.LongText;
                    else if (types.Contains("locality"))
                        city = c.LongText;
                    else if (types.Contains("administrative_area_level_1"))
                        state = c.ShortText;
                    else if (types.Contains("postal_code"))
                        zip = c.LongText;
                    else if (types.Contains("country"))
                        country = c.LongText;
                }
            }

            var street = streetNumber != null && route != null
                ? $"{streetNumber} {route}"
                : route ?? streetNumber;

            return new AddressValidationResponse
            {
                Valid = true,
                Address = street,
                City = city,
                State = state,
                ZipCode = zip,
                Country = country,
                FormattedAddress = place.FormattedAddress
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Address validation failed for '{Query}'", query);
            return new AddressValidationResponse { Valid = false };
        }
    }

    private static bool IsDomainInput(string input)
    {
        return input.Contains('.') && !input.Contains(' ');
    }

    private static string DomainToSearchText(string domain)
    {
        // Strip common TLDs and www prefix to get a company-friendly search term
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
