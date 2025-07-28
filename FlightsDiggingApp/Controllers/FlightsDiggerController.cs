using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using FlightsDiggingApp.Mappers;
using FlightsDiggingApp.Models;
using FlightsDiggingApp.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace FlightsDiggingApp.Controllers
{
    //[EnableCors("AllowAll")]
    [ApiController]
    [Route("api/flightsdigger")]
    public class FlightsDiggerController : ControllerBase
    {
        private readonly ILogger<FlightsDiggerController> _logger;
        IFlightsDiggerService _flightsDiggerService;

        public FlightsDiggerController(ILogger<FlightsDiggerController> logger, IFlightsDiggerService flightsDiggerService)
        {
            _logger = logger;
            _flightsDiggerService = flightsDiggerService;
        }

        /*[HttpGet("getroundtrips")]
        public async Task GetRoundTrips()
        {
            _logger.LogInformation(">>>>>>>>>>Starting: GetRoundTrips");
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await _flightsDiggerService.HandleRoundTripsAsync(webSocket);
            }
            else
            {
                _logger.LogInformation("Not a WebSocket request");
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
            
        }
        */

        [HttpGet("airports")]
        public AirportsResponseDTO GetAirports([FromQuery] string query)
        {
            return _flightsDiggerService.GetAirports(query);
        }

        [HttpPost("singleroundtrip")]
        public RoundtripResponseDTO SingleRoundTrip([FromBody] RoundtripRequest request)
        {
            return _flightsDiggerService.GetRoundTrip(request);
        }

        [HttpPost("cachedroundtrips")]
        public CachedRoundTripsResponseDTO CachedRoundTrips([FromBody] CachedRoundTripsRequest request)
        {
            return _flightsDiggerService.GetCachedRoundTrips(request);
        }

#if DEBUG
        // TESTING ENDPOINTS - DEACTIVATE IN PROD
        [HttpGet("authtoken")]
        public string GetAuthToken()
        {
            return _flightsDiggerService.GetAuthToken();
        }
       
    }
#endif

}
