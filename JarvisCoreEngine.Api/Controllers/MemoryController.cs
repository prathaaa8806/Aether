using JarvisCoreEngine.Services;
using Microsoft.AspNetCore.Mvc;

namespace JarvisCoreEngine.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MemoryController : ControllerBase
    {
        private readonly AiService _ai;

        public MemoryController(AiService ai)
        {
            _ai = ai;
        }

        [HttpPost("reset")]
        public IActionResult Reset()
        {
            _ai.ResetConversation();
            return Ok(new { message = "Conversation reset." });
        }

        [HttpPost("forget")]
        public IActionResult Forget()
        {
            _ai.ForgetEverything();
            return Ok(new { message = "Memory completely wiped." });
        }
    }
}