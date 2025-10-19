using HealthCheckService.Telemetry;
using Microsoft.AspNetCore.Mvc;

namespace HealthCheckService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TelemetryController(HealthService healthService) : Controller
    {
        [HttpPost("set/degraded")]
        public async Task<IActionResult> SetDegraded(bool value)
        {
            healthService.Degraded = value;
            return Ok();
        }

        [HttpPost("set/failed")]
        public async Task<IActionResult> SetFailed(bool value)
        {
            healthService.Failed = value;
            return Ok();
        }
    }
}
