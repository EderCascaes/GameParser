using GameParser.API.Models;

namespace GameParser.API.Interfaces
{
    public interface ILogParserService
    {
        List<GameLog> Parse();
    }
}
