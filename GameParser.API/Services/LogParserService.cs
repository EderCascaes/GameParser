using GameParser.API.Interfaces;
using GameParser.API.Models;

namespace GameParser.API.Services
{
    public class LogParserService : ILogParserService
    {
        private readonly string? _logPath;
        private readonly IConfiguration _configuration;

        public LogParserService(IConfiguration configuration)
        {
            _configuration = configuration;
            _logPath = _configuration["logPath"];
        }
        public List<GameLog> Parse()
        {

            try
            {
                var logs = File.ReadAllLines(_logPath);

                var games = new List<GameLog>();

                GameLog? currentGame = null;
                int gameCounter = 1;

                foreach (var line in logs)
                {
                    if (line.Contains("InitGame"))
                    {
                        currentGame = new GameLog { GameId = gameCounter++ };
                        games.Add(currentGame);
                    }
                    else if (line.Contains("ShutdownGame"))
                    {
                        currentGame = null;
                    }
                    else if (line.Contains("ClientUserinfoChanged"))
                    {
                        var parts = line.Split('\\');
                        var player = parts.Length > 1 ? parts[1] : "Unknown";

                        if (!string.IsNullOrEmpty(player) && !currentGame.Players.Contains(player))
                            currentGame.Players.Add(player);
                    }
                    else if (line.Contains("Kill:") && currentGame != null)
                    {
                        currentGame.TotalKills++;

                        var killData = line.Split(':').Last().Trim();
                        var parts = killData.Split(" by ");

                        var killInfo = parts[0];
                        var cause = parts.Length > 1 ? parts[1] : "Unknown";

                        var playersInKill = killInfo.Split(" killed ");
                        if (playersInKill.Length == 2)
                        {
                            var killer = playersInKill[0].Trim();
                            var victim = playersInKill[1].Trim();

                            // Morte por ambiente
                            if (killer == "<world>")
                            {
                                if (!currentGame.Kills.ContainsKey(victim))
                                    currentGame.Kills[victim] = 0;

                                currentGame.Kills[victim]--;

                                currentGame.Events.Add($"{victim} morreu por {cause}");
                            }
                            else
                            {
                                if (!currentGame.Kills.ContainsKey(killer))
                                    currentGame.Kills[killer] = 0;

                                currentGame.Kills[killer]++;

                                currentGame.Events.Add($"{killer} matou {victim} com {cause}");
                            }
                        }
                    }
                }
                return games;

            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao ler arquivo: " + ex.Message);
                return default;

            }

        }
    }
}
