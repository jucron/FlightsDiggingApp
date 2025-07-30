using FlightsDiggingApp.Models.Auth;

namespace FlightsDiggingApp.Services.Auth
{
    public interface IAuthService
    {
        AuthResponseDTO Authenticate(AuthRequestDTO request);
        bool Authorize(string clientId, long timestamp, string token);
    }
}
