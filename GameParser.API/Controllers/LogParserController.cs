using GameParser.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GameParser.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LogParserController : ControllerBase
    {
        private readonly ILogParserService _parser;

        public LogParserController(ILogParserService parser)
        {
            this._parser = parser;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var result = _parser.Parse();
            return Ok(result);
        }
    }
}
