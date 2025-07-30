
using System.Security.Cryptography;
using System.Text;
using FlightsDiggingApp.Properties;
using Microsoft.Extensions.Options;

namespace FlightsDiggingApp.Services.Auth
{
    public class AuthHelper
    {
        public static string ComputeHmac(string secretKey, string message)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(messageBytes);
            return Convert.ToBase64String(hashBytes); // or BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        public static bool IsValidApiKey(string expected, string provided)
        {
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(provided)
            );
        }

        internal static void ProvideApiKeyForTesting(WebApplicationBuilder builder)
        {
            EnvironmentProperties env = builder.Services.BuildServiceProvider().GetRequiredService<IOptions<EnvironmentProperties>>().Value;
            var time = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string apiKey = AuthHelper.ComputeHmac(env.API_SECRET, env.CLIENT_ID + time);
            Console.WriteLine($"Use this Api-Key request for testing: \nCLIENT_ID: {env.CLIENT_ID} \nTime: {time} \nApi-Key: {apiKey}");
        }
    }
}
