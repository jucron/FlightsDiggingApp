using FlightsDiggingApp.Models;
using FlightsDiggingApp.Models.RapidApi;

namespace FlightsDiggingApp.Services
{
    public interface IRoundTripApiService
    {
        public Task<IApiServiceResponse> GetAirportsAsync(string query, int tries = 3);

        public Task<IApiServiceResponse> GetRoundtripAsync(RoundtripRequest request, int tries = 3);
    }
}
