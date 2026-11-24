using JarvisCoreEngine.Api.Dtos;
using JarvisCoreEngine.Services;
using Microsoft.AspNetCore.Mvc;

namespace JarvisCoreEngine.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly FileService _files;

        public FilesController(FileService files)
        {
            _files = files;
        }

        [HttpGet]
        public IActionResult List()
        {
            var files = _files.ListFiles();
            return Ok(new { files });
        }

        [HttpGet("{name}")]
        public IActionResult Read(string name)
        {
            var content = _files.ReadFile(name);
            if (content == null)
                return NotFound(new { error = "File not found." });

            return Ok(new { fileName = name, content });
        }

        [HttpPost]
        public IActionResult Write([FromBody] WriteFileRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FileName))
                return BadRequest(new { error = "Filename required." });

            var success = _files.WriteFile(request.FileName, request.Content);
            if (!success)
                return StatusCode(500, new { error = "Failed to write file." });

            return Ok(new { message = "File written.", fileName = request.FileName });
        }

        [HttpDelete("{name}")]
        public IActionResult Delete(string name)
        {
            var success = _files.DeleteFile(name);
            if (!success)
                return NotFound(new { error = "File not found." });

            return Ok(new { message = "File deleted.", fileName = name });
        }
    }
}