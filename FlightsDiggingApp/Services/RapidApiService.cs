using FlightsDiggingApp.Models;
using System.Text.Json;
using System.Text;
using FlightsDiggingApp.Controllers;
using Microsoft.OpenApi.Any;
using System.Threading;
using FlightsDiggingApp.Mappers;
using Microsoft.Extensions.Logging;
using FlightsDiggingApp.Models.RapidApi;

/*
namespace FlightsDiggingApp.Services
{
    
    public class RapidApiService : IRoundTripApiService
    {
        private readonly string _rapidapi_key = "441f8260camsh5ee529fad4a52c9p1cadf2jsnd01e83b82152";
        private readonly ILogger<RapidApiService> _logger;

        public RapidApiService(ILogger<RapidApiService> logger)
        {
            _logger = logger;
        }

        // tries is optional parameter

        public async Task<IApiServiceResponse> GetAirportsAsync(string query, int tries)
        {
            _logger.LogInformation($"GetAirportsAsync with query: <{query}>. Tries left: {tries-1}");
            //trim whitespaces of query at ends
            query = query.Trim();
            query = query.Replace(" ", "%20");
            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://sky-scanner3.p.rapidapi.com/flights/auto-complete?query={query}"),
                Headers =
                {
                    { "x-rapidapi-key", _rapidapi_key },
                    { "x-rapidapi-host", "sky-scanner3.p.rapidapi.com" },
                },
            };
            try
            {
                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var jsonString = await response.Content.ReadAsStringAsync();
                    if (jsonString != null)
                    {
                        RapidApiAirportsResponse finalResponse = JsonSerializer.Deserialize<RapidApiAirportsResponse>(jsonString);
                        if (finalResponse != null)
                        {
                            finalResponse.operationStatus = OperationStatus.CreateStatusSuccess();
                            return AirportsMapper.MapGetAirportsResponseToDTO(finalResponse);
                        }
                    }
                    return new AirportsResponseDTO() { operationStatus = OperationStatus.CreateStatusFailure("Unexpected error -> jsonstring from response API is null") };
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Error in ExecuteSearchIncompleteAsync " + ex.ToString());
                if (tries > 1)
                {
                    return await GetAirportsAsync(query, tries-1);
                }
                return new AirportsResponseDTO() { operationStatus = OperationStatus.CreateStatusFailure("Unexpected error -> "+ex.ToString()) };
            }
        }

        public async Task<IApiServiceResponse> GetRoundtripAsync(RoundtripsRequest request, int tries, string errorDescription)
        {
            var delayMillisecondsDefault = 1000;

            SearchRoundTripResponse resultSearchRoundTrip = await ExecuteSearchRoundTripAsync(request);
            
            if (resultSearchRoundTrip == null || resultSearchRoundTrip.data == null || resultSearchRoundTrip.data.context == null)
            {
                errorDescription = $"ExecuteSearchRoundTripAsync with NULL objects, tries remaining: {tries-1}";
                _logger.LogInformation(errorDescription);
                if (tries > 1) { return await GetRoundtripAsync(request, tries - 1, errorDescription); }
                return new RapidApiRoundtripsResponse() { operationStatus = OperationStatus.CreateStatusFailure(errorDescription) };
            }
            var searchRoundTripData = resultSearchRoundTrip.data;
            //_logger.LogInformation($"resultSearchRoundTrip operationStatus: {searchRoundTripData.context.operationStatus}");

            if (searchRoundTripData.context.operationStatus == "failure")
            {
                errorDescription = $"ExecuteSearchRoundTripAsync with operationStatus FAILURE, tries remaining: {tries-1}";
                _logger.LogInformation(errorDescription);
                if (tries > 1) { return await GetRoundtripAsync(request, tries - 1, errorDescription); }
                return new RapidApiRoundtripsResponse() { operationStatus = OperationStatus.CreateStatusFailure(errorDescription) };
            }
            else if (searchRoundTripData.context.operationStatus == "complete")
            {
                // todo: handle complete responses from ExecuteSearchRoundTripAsync
                errorDescription = $"ExecuteSearchRoundTripAsync with operationStatus COMPLETE. Not implemented yet.";
                _logger.LogInformation(errorDescription);
                return new RapidApiRoundtripsResponse() { operationStatus = OperationStatus.CreateStatusFailure(errorDescription) };
            }
            else if (searchRoundTripData.context.operationStatus == "incomplete")
            {
                request.sessionId = searchRoundTripData.context.sessionId.TrimEnd('=');

                //_logger.LogInformation($"sessionId: {sessionId}");

                // tries 5 times
                int maxTries = 5;
                delayMillisecondsDefault = 5000;

                // Error description in case tries exceed
                errorDescription = $"ExecuteSearchRoundTripAsync run out of maxTries!";

                for (int i = 1; i <= maxTries; i++)
                {
                    var delayMilliseconds = delayMillisecondsDefault * Math.Max(1, (maxTries - i - 1));

                    _logger.LogInformation($"ExecuteSearchIncompleteAsync for dates: {request.departDate} -> {request.returnDate}. Try number: {i} of {maxTries}, with delay of {delayMilliseconds / 1000} seconds");

                    // wait 
                    Task.Delay(delayMilliseconds).Wait();

                    var resultSearchIncomplete = await ExecuteSearchIncompleteAsync(request);

                    if (resultSearchIncomplete == null || resultSearchIncomplete.data == null || resultSearchIncomplete.data.context == null)
                    {
                        errorDescription = "ExecuteSearchIncompleteAsync with NULL objects!";
                        _logger.LogInformation("Restarting count, cause of error: {errorDescription}", errorDescription);
                        continue;
                    }

                    var searchIncompleteData = resultSearchIncomplete.data;
                    //_logger.LogInformation($"resultSearchIncomplete operationStatus: {searchIncompleteData?.context?.operationStatus}");

                    if (searchIncompleteData?.context.operationStatus == "complete")
                    {
                        // Success
                        return RoundtripsMapper.MapSearchIncompleteResponseToGetRoundtripsResponse(resultSearchIncomplete, request);
                    }
                }
            }
            _logger.LogInformation(errorDescription);
            return new RapidApiRoundtripsResponse() { operationStatus = new() { hasError = true, errorDescription = errorDescription } };
        }

        private async Task<SearchIncompleteResponse> ExecuteSearchIncompleteAsync(RoundtripsRequest roundTripRequest)
        {
            // Data to be send
            var sessionId = roundTripRequest.sessionId;
            var currency = roundTripRequest.currency;

            var client = new HttpClient();
            var uri = $"https://sky-scanner3.p.rapidapi.com/flights/search-incomplete?sessionId={sessionId}&sort=cheapest_first&currency={currency}";
            _logger.LogInformation($"ExecuteSearchIncompleteAsync with URI: {uri}");


            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(uri),
                Headers =
                    {
                        { "x-rapidapi-key", _rapidapi_key },
                        { "x-rapidapi-host", "sky-scanner3.p.rapidapi.com" },
                    },
            };
            string jsonString = "not defined yet";
            try
            {
                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    //return await response.Content.ReadAsStringAsync();
                    jsonString = await response.Content.ReadAsStringAsync();

                    return JsonSerializer.Deserialize<SearchIncompleteResponse>(jsonString);
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Error in ExecuteSearchIncompleteAsync " + ex.ToString());
                _logger.LogInformation("Content in jsonString: " + jsonString);
            }
            return new SearchIncompleteResponse();
        }

        private async Task<SearchRoundTripResponse> ExecuteSearchRoundTripAsync(RoundtripsRequest roundTripRequest)
        {
            // Data to be sent
            var from = roundTripRequest.from;
            var to = roundTripRequest.to;
            var departDate = roundTripRequest.departDate;
            var returnDate = roundTripRequest.returnDate;
            var currency = roundTripRequest.currency;
            var adults = roundTripRequest.adults;
            var cabinclass = roundTripRequest.cabinclass;
            var infants = roundTripRequest.infants;
            var children = roundTripRequest.children.Count;

            var client = new HttpClient();
            var uri = $"https://sky-scanner3.p.rapidapi.com/flights/search-roundtrip?fromEntityId={from}&toEntityId={to}&departDate={departDate}&returnDate={returnDate}&currency={currency}&adults={adults}&cabinClass={cabinclass}&infants={infants}&children={children}&sort=cheapest_first";
            _logger.LogInformation($"ExecuteSearchRoundTripAsync with URI: {uri}");

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(uri),
                Headers =
                    {
                        { "x-rapidapi-key", _rapidapi_key },
                        { "x-rapidapi-host", "sky-scanner3.p.rapidapi.com" },
                    },
            };

            try
            {
                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    //return await response.Content.ReadAsStringAsync();
                    var jsonString = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<SearchRoundTripResponse>(jsonString);
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Error in ExecuteSearchRoundTripAsync " + ex.ToString());
            }
            return new SearchRoundTripResponse();
        }
    }
}
*/