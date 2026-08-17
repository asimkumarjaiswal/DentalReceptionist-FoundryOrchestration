using Microsoft.AspNetCore.Mvc;
using VoiceDentalReceptionist.Models.Responses;

namespace VoiceDentalReceptionist.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<HealthResponse> Get() => Ok(new HealthResponse("Healthy"));
}
