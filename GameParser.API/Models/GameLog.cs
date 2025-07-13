namespace GameParser.API.Models
{
    public class GameLog
    {
        public int GameId { get; set; }
        public List<string> Players { get; set; } = new();
        public int TotalKills { get; set; }
        public Dictionary<string, int> Kills { get; set; } = new();
        public List<string> Events { get; set; } = new(); // para mortes com causas
    }
}
