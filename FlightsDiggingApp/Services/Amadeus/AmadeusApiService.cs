using FlightsDiggingApp.Mappers;
using System.Text.Json;
using FlightsDiggingApp.Models;
using FlightsDiggingApp.Models.RapidApi;
using FlightsDiggingApp.Properties;
using FlightsDiggingApp.Helpers;
using FlightsDiggingApp.Models.Amadeus;
using System.Net;
using Microsoft.Extensions.Options;

namespace FlightsDiggingApp.Services.Amadeus
{
    public class AmadeusApiService : IRoundTripApiService
    {
        private readonly ILogger<AmadeusApiService> _logger;
        private readonly IAmadeusAuthService _authService;
        private readonly AmadeusApiProperties _amadeusApiProperties;

        public AmadeusApiService(ILogger<AmadeusApiService> logger, IAmadeusAuthService authService, IPropertiesProvider propertiesProvider)
        {
            _logger = logger;
            _authService = authService;
            _amadeusApiProperties = propertiesProvider.AmadeusApiProperties;
        }

        public async Task<IApiServiceResponse> GetAirportsAsync(string query, int tries = 3)
        {
            var triesLeft = tries-1;
            _logger.LogInformation($"GetAirportsAsync with query: <{query}>. Tries left: {triesLeft}");

            var headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/x-www-form-urlencoded" }
            };

            var parameters = new Dictionary<string, string>
            {
                { "keyword", query },
                { "subType", "CITY,AIRPORT" }
            };

            var bearerToken = _authService.GetToken();

            var baseUrl = _amadeusApiProperties.base_url_getairports;

            var apiResponse = await ApiCallUtility.GetAsync<AmadeusAirportResponse>(baseUrl,parameters,headers, bearerToken);

            if (apiResponse.operationStatus.hasError || apiResponse.data == null)
            {
                _logger.LogError("Error in GetAirportsAsync: " + apiResponse.operationStatus.errorDescription);

                // If tries still available
                if (triesLeft > 0)
                {
                    // If token expired, clear it from cache. The getToken will renew in the next call
                    if (apiResponse.operationStatus.status.httpStatus == HttpStatusCode.Unauthorized)
                    {
                        _authService.ClearToken();
                    }
                    Task.Delay(2000).Wait();
                    return await GetAirportsAsync(query, triesLeft);
                }
                // API tries expired
                return new AmadeusAirportResponse() { operationStatus = apiResponse.operationStatus };
            }
            apiResponse.data.operationStatus = apiResponse.operationStatus;
            return apiResponse.data;
        }

        public async Task<IApiServiceResponse> GetRoundtripAsync(RoundtripRequest request, int tries = 3)
        {
            var triesLeft = tries - 1;
            _logger.LogInformation($"GetRoundtripAsync triggered. Tries left: {triesLeft}");

            var parameters = new Dictionary<string, string>
            {
                { "originLocationCode", request.from },
                { "destinationLocationCode", request.to },
                { "departureDate", request.departDate },
                { "returnDate", request.returnDate },
                { "adults", request.adults.ToString() },
                { "children", request.children.ToString() },
                { "infants", request.infants.ToString() },
                { "travelClass", request.travelClass },
                { "currencyCode", request.currency }
            };

            var bearerToken = _authService.GetToken();

            var baseUrl = _amadeusApiProperties.base_url_roundtrips;

            var apiResponse = await ApiCallUtility.GetAsync<AmadeusSearchFlightsResponse>(baseUrl, parameters, null, bearerToken);

            if (apiResponse.operationStatus.hasError || apiResponse.data == null)
            {
                _logger.LogError("Error in GetRoundtripAsync: " + apiResponse.operationStatus.errorDescription);

                // If tries still available
                if (triesLeft > 0)
                {
                    // If token expired, clear it from cache. The getToken will renew in the next call
                    if (apiResponse.operationStatus.status.httpStatus == HttpStatusCode.Unauthorized)
                    {
                        _authService.ClearToken();
                    }
                    Task.Delay(2000).Wait();
                    return await GetRoundtripAsync(request, triesLeft);
                }
                // API tries expired
                return new AmadeusSearchFlightsResponse() { operationStatus = apiResponse.operationStatus };
            }
            apiResponse.data.operationStatus = apiResponse.operationStatus;
            return apiResponse.data;
        }
    }
}
