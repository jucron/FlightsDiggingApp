using FlightsDiggingApp.Helpers;
using FlightsDiggingApp.Mappers;
using FlightsDiggingApp.Models;
using FlightsDiggingApp.Models.Amadeus;
using FlightsDiggingApp.Properties;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace FlightsDiggingApp.Services.Amadeus
{
    public class AmadeusAuthService : IAmadeusAuthService
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<AmadeusAuthService> _logger;
        private readonly AmadeusApiProperties _amadeusApiProperties;

        public AmadeusAuthService(ICacheService cacheService, ILogger<AmadeusAuthService> logger, IPropertiesProvider propertiesProvider)
        {
            _cacheService = cacheService;
            _logger = logger;
            _amadeusApiProperties = propertiesProvider.AmadeusApiProperties;
        }

        public void ClearToken()
        {
           _cacheService.ClearToken();
        }

        public string GetToken()
        {
            // Get first from cache
            var token = _cacheService.GetToken();
            if (token != "")
            {
                _logger.LogInformation("Token found in cache, returning it");
                return token;
            }

            _logger.LogInformation("Token NOT found in cache, calling api to fetch a new one");
            // If not in cache, we need to fetch from server
            token = GetAsyncToken().Result;
            _cacheService.SetToken(token);
            return token;
        }

        private async Task<string> GetAsyncToken()
        {
            _logger.LogInformation($"GetAsyncToken triggered");

            var headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/x-www-form-urlencoded" }
            };

            var parameters = new Dictionary<string, string>
            {
                { "grant_type", _amadeusApiProperties.grant_type },
                { "client_id", _amadeusApiProperties.client_id },
                { "client_secret", _amadeusApiProperties.client_secret }
            };

            var baseUrl = "https://test.api.amadeus.com/v1/security/oauth2/token";

            var apiResponse = await ApiCallUtility.PostAsyncFormUrlEncodedContent<AuthResponse>(baseUrl, parameters, headers);

            if (apiResponse.operationStatus.hasError || apiResponse.data == null)
            {
                _logger.LogError("Error in GetAsyncToken: " + apiResponse.operationStatus.errorDescription);
                return "";
            }
            return apiResponse.data.access_token;
        }
    }
}
