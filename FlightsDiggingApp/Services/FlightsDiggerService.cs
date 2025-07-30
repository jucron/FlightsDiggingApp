using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlightsDiggingApp.Mappers;
using FlightsDiggingApp.Models;
using FlightsDiggingApp.Models.RapidApi;
using FlightsDiggingApp.Properties;
using FlightsDiggingApp.Services.Amadeus;
using FlightsDiggingApp.Services.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlightsDiggingApp.Services
{
    public class FlightsDiggerService : IFlightsDiggerService
    {
        private readonly ILogger<FlightsDiggerService> _logger;
        private readonly IRoundTripApiService _apiService;
        private readonly IFilterService _filterService;
        private readonly ICacheService _cacheService;
        private readonly IAmadeusAuthService _authService;
        private readonly IPropertiesProvider _propertiesProvider;

        public FlightsDiggerService(ILogger<FlightsDiggerService> logger, 
            IRoundTripApiService apiService, IFilterService filterService, ICacheService cacheService, 
            IAmadeusAuthService authService, IPropertiesProvider propertiesProvider)
        {
            _logger = logger;
            _apiService = apiService;
            _filterService = filterService;
            _cacheService = cacheService;
            _authService = authService;
            _propertiesProvider = propertiesProvider;
        }

        public string GetAuthToken()
        {
            return _authService.GetToken();
        }

        public AirportsResponseDTO GetAirports(string query)
        {
            var response = _apiService.GetAirportsAsync(query).Result;
            if (response == null)
            {
                return new AirportsResponseDTO() { status = OperationStatus.CreateStatusFailure(HttpStatusCode.NoContent, "Response from external API is null") };
            }
            return AirportsMapper.MapAirportsResponseToDTO(response);
        }

        public RoundtripResponseDTO GetRoundTrip(RoundtripRequest request)
        {
            bool isTest = _propertiesProvider.AmadeusApiProperties.isTest;

            RoundtripResponseDTO responseDTO;

            if (!isTest) { 
                var response = _apiService.GetRoundtripAsync(request).Result;
                if (response == null)
                {
                    return new RoundtripResponseDTO() { status = OperationStatus.CreateStatusFailure(HttpStatusCode.NoContent, "Response from external API is null") };
                }
                // Map response to DTO
                responseDTO = RoundtripMapper.MapGetRoundtripResponseToDTO(response, request, _propertiesProvider.AffiliateProperties);
            } else
            {
                // Only for testing!
                string json = File.ReadAllText("Properties\\ExampleResponseDTO.json");
                JsonSerializerOptions jsonOptions = new() { Converters = { new JsonStringEnumConverter() } };
                responseDTO = JsonSerializer.Deserialize<RoundtripResponseDTO>(json, jsonOptions);
            }

            // If response have error, no need to store in cache or apply filter
            if (responseDTO == null || responseDTO.status.hasError) { return responseDTO; }

            // Generate UUID
            responseDTO.id = _cacheService.GenerateUUID();

            // Generate original Metrics
            _filterService.ApplyMetrics(responseDTO);

            // Persist in cache for future filterings
            _cacheService.StoreRoundtripResponseDTO(responseDTO);

            // ApplyFilter filter the DTO before sending it to front
            RoundtripResponseDTO filteredResponseDTO = _filterService.FilterRoundtripResponseDTO(request.filter, responseDTO);

            // Return filtered results
            return filteredResponseDTO;
        }

        public async Task HandleRoundTripsAsync(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result;
            string receivedMessage = string.Empty;

            // Receive the request
            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);

            _logger.LogInformation($"Received message before deserializing: {receivedMessage}");

            try
            {
                var request = JsonSerializer.Deserialize<RoundtripsRequest>(receivedMessage, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });


                if (request != null)
                {
                    _logger.LogInformation($"Received request for RoundTrips in request: {request}");

                    DateTime startDepartDate = DateTime.Parse(request.initDepartDateString);
                    DateTime endDepartDate = DateTime.Parse(request.endDepartDateString);

                    DateTime startReturnDate = DateTime.Parse(request.initReturnDateString);
                    DateTime endReturnDate = DateTime.Parse(request.endReturnDateString);

                    int batchSize = 5;
                    var semaphore = new SemaphoreSlim(batchSize);
                    var tasks = new List<Task>();  // Store tasks for concurrent execution

                    for (DateTime departDate = startDepartDate; departDate <= endDepartDate; departDate = departDate.AddDays(1))
                    {

                        for (DateTime returnDate = startReturnDate; returnDate <= endReturnDate; returnDate = returnDate.AddDays(1))
                        {

                            // Create a copy of the request for each iteration
                            var requestCopy = RoundtripsMapper.CreateCopyOfGetRoundtripsRequest(request, departDate, returnDate);
                            /*
                            // Add the request processing as a task to the list
                            tasks.Add(ProcessRoundtripAsyncWithConcurrentControl(requestCopy, webSocket, semaphore));
                            */
                            if (tasks.Count >= batchSize)
                            {
                                await Task.WhenAll(tasks);
                                tasks.Clear();
                            }
                            await Task.Delay(500);
                        }
                    }
                    // Wait for all tasks to complete before finishing
                    await Task.WhenAll(tasks);
                }
            }
            catch (JsonException)
            {
                _logger.LogError("Invalid JSON format received.");
                await webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("Invalid request format")),
                                          WebSocketMessageType.Text,
                                          true,
                                          CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing WebSocket request");
            }
            finally
            {
                // Close the WebSocket connection after all data has been sent
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Finished sending data", CancellationToken.None);
            }
        }
        /*
        private async Task ProcessRoundtripAsyncWithConcurrentControl(RoundtripsRequest request, WebSocket webSocket, SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync(); // Wait for an available slot
            try
            {
                await ProcessRoundtripAsync(request, webSocket);
            }
            finally
            {
                semaphore.Release(); // Release the slot for the next task
            }
        }
        
        private async Task ProcessRoundtripAsync(RoundtripsRequest requestCopy, WebSocket webSocket)
        {
            try
            {
                // Call external API TODO: fix this! will break. Should expect IApiResponse
                RapidApiRoundtripsResponse roundtripsResponse = (RapidApiRoundtripsResponse)await _apiService.GetRoundtripAsync(requestCopy);

                // Map the response into DTO object (cleaning)
                RoundtripsResponseDTO getRoundtripsResponseDTO = RoundtripsMapper.MapGetRoundtripsResponseToDTO(roundtripsResponse);

                // Generate UUID
                getRoundtripsResponseDTO.id = _cacheService.GenerateUUID();

                // Persist in cache for future filterings
                _cacheService.StoreGetRoundtripsResponseDTO(getRoundtripsResponseDTO);

                // ApplyFilter filter the DTO before sending it to front
                RoundtripsResponseDTO filteredResponseDTO = _filterService.FilterFlightsFromGetRoundtripsResponseDTO(requestCopy.filter,getRoundtripsResponseDTO);

                // Serialize response
                var responseJson = JsonSerializer.Serialize(filteredResponseDTO, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                // Convert to bytes for WebSocket
                var responseBytes = Encoding.UTF8.GetBytes(responseJson);

                // Send response back if WebSocket is open
                if (webSocket.State == WebSocketState.Open)
                {
                    await webSocket.SendAsync(new ArraySegment<byte>(responseBytes),
                                              WebSocketMessageType.Text,
                                              true, // Marks the message as completed
                                              CancellationToken.None);
                }
                else
                {
                    _logger.LogWarning("WebSocket connection is not open.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing roundtrip request");

                var errorMessage = Encoding.UTF8.GetBytes("Error processing roundtrip request.");
                await webSocket.SendAsync(new ArraySegment<byte>(errorMessage),
                                          WebSocketMessageType.Text,
                                          true,
                                          CancellationToken.None);
            }
        }
        */
        public CachedRoundTripsResponseDTO GetCachedRoundTrips(CachedRoundTripsRequest request)
        {
            _logger.LogInformation($"GetCachedRoundTrips triggered: {request}");
            CachedRoundTripsResponseDTO cachedRoundTripsResponseDTO = new() { responses = [], status = OperationStatus.CreateStatusSuccess(HttpStatusCode.OK) };

            foreach (var id in request.ids)
            {
                try
                {
                    // Get Response from cache
                    var response = _cacheService.RetrieveRoundtripResponseDTO(id);

                    if (response.status.hasError) {
                        _logger.LogError(response.status.errorDescription);
                        cachedRoundTripsResponseDTO.status = response.status;
                        break;
                    }

                    // Filter the response
                    var filteredResponse = _filterService.FilterRoundtripResponseDTO(request.filter, response);

                    // Add to the list
                    cachedRoundTripsResponseDTO.responses.Add(filteredResponse);

                } catch (Exception ex)
                {
                    var errorMessage = $"Error retrieving cached roundtrip with uuid: {id}, exception: \n{ex}";
                    _logger.LogError(ex, errorMessage);
                    cachedRoundTripsResponseDTO.status = OperationStatus.CreateStatusFailure(HttpStatusCode.ExpectationFailed, errorMessage);
                    break;
                }
            }
            return cachedRoundTripsResponseDTO;
        }
    }

}
