using Microsoft.AspNetCore.Mvc;
using GestionQ.Web.Services;

namespace GestionQ.Web.Controllers
{
    [ApiController]
    [Route("api/tunnel")]
    public class TunnelApiController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        
        public TunnelApiController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("url")]
        public IActionResult GetUrl()
        {
            var domain = _configuration["Ngrok:Domain"];
            if (string.IsNullOrEmpty(domain))
            {
                return Ok(new { url = "", status = "waiting" });
            }
            
            return Ok(new { url = $"https://{domain}", status = "ready" });
        }
    }
}
