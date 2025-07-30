using FlightsDiggingApp.Models;
using FlightsDiggingApp.Models.Auth;
using FlightsDiggingApp.Services.Auth;
using Microsoft.AspNetCore.Mvc;

namespace FlightsDiggingApp.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("authenticate")]
        public IActionResult Authenticate([FromBody] AuthRequestDTO request)
        {
            try
            {
                var response = _authService.Authenticate(request);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }

        }
    }
}
