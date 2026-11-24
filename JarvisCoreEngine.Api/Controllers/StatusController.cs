using JarvisCoreEngine.Models;
using Microsoft.AspNetCore.Mvc;

namespace JarvisCoreEngine.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatusController : ControllerBase
    {
        private readonly AppSettings _settings;

        public StatusController(AppSettings settings)
        {
            _settings = settings;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                status = "Online",
                app = _settings.AppName,
                version = _settings.Version,
                author = _settings.Author,
                timestamp = DateTime.UtcNow
            });
        }
    }
}