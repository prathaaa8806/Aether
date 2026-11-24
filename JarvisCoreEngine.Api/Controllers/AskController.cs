using JarvisCoreEngine.Api.Dtos;
using JarvisCoreEngine.Services;
using Microsoft.AspNetCore.Mvc;

namespace JarvisCoreEngine.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AskController : ControllerBase
    {
        private readonly AiService _ai;

        public AskController(AiService ai)
        {
            _ai = ai;
        }

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] AskRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new { error = "Message cannot be empty." });

            var answer = await _ai.AskAsync(request.Message);

            if (string.IsNullOrWhiteSpace(answer))
                return StatusCode(500, new { error = "AI returned no response." });

            return Ok(new { response = answer });
        }
    }
}