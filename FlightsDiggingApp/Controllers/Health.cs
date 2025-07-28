using Microsoft.AspNetCore.Mvc;

namespace FlightsDiggingApp.Controllers
{
    public class Health : Controller
    {
        [HttpGet("health")]
        public string HealthCheck()
        {
            return "OK";
        }
        
    }
}
