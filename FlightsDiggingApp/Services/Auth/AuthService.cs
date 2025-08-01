using System.Collections.Concurrent;
using FlightsDiggingApp.Models.Auth;
using FlightsDiggingApp.Properties;

namespace FlightsDiggingApp.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly EnvironmentProperties _environmentProperties;

        private readonly ConcurrentDictionary<string, DateTime> _tokenStore;

        public AuthService(IPropertiesProvider propertiesProvider)
        {
            _environmentProperties = propertiesProvider.EnvironmentProperties;
            _tokenStore = [];
        }

        public AuthResponseDTO Authenticate(AuthRequestDTO request)
        {
            string expectedClientId = _environmentProperties.CLIENT_ID;
            string secretKey = _environmentProperties.API_SECRET;

            if (request.ClientId != expectedClientId)
                throw new UnauthorizedAccessException("Invalid client ID.");

            // Optional: check timestamp freshness (e.g., 5 minutes window)
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (Math.Abs(now - request.Timestamp) > 300)
                throw new UnauthorizedAccessException("Request timestamp expired or too far from server time.");

            var expectedSignature = AuthHelper.ComputeHmac(secretKey, request.ClientId + request.Timestamp);
            if (!AuthHelper.IsValidApiKey(expectedSignature, request.Signature))
                throw new UnauthorizedAccessException("Invalid token.");

            // Generate a response token and store it
            var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            var expires = DateTime.UtcNow.AddMinutes(15);

            _tokenStore.TryAdd(token, expires);

            return new AuthResponseDTO
            {
                Token = token,
                ExpiresAt = expires
            };
        }
        public bool Authorize(string clientId, long timestamp, string token)
        {
            try
            {
                var expectedClientId = _environmentProperties.CLIENT_ID;
                var secretKey = _environmentProperties.API_SECRET;

                if (clientId != expectedClientId)
                    return false;

                var time = DateTimeOffset.FromUnixTimeSeconds(timestamp);
                if (Math.Abs((DateTimeOffset.UtcNow - time).TotalMinutes) > 5)
                    return false;

                return IsTokenValid(token);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            
        }
        private bool IsTokenValid(string token)
        {
            if (_tokenStore.TryGetValue(token, out var expires))
            {
                if (expires > DateTime.UtcNow)
                {
                    return true;
                }
                else
                {
                    // Token is expired; remove it to free memory
                    _tokenStore.TryRemove(token, out _);
                }
            }

            return false;
        }
    }
}
