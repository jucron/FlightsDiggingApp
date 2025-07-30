using Microsoft.AspNetCore.Mvc;

namespace FlightsDiggingApp.Controllers
{
    [Route("health")]
    public class HealthController : Controller
    {
        [HttpGet]
        public string HealthCheck()
        {
            return "OK";
        }
        
    }
}
