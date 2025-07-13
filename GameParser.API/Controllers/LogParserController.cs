using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace GameParser.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LogParserController : ControllerBase
    {
        public IActionResult Index()
        {
            return Ok("OK");
        }
    }
}
