using Microsoft.AspNetCore.Mvc;

namespace FlightsDiggingApp.Controllers
{
    [Route("health")]
    public class Health : Controller
    {
        [HttpGet]
        public string HealthCheck()
        {
            return "OK";
        }
        
    }
}
