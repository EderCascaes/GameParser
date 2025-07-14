using System.Globalization;
using GameParser.API.Interfaces;
using GameParser.API.Models;


namespace GameParser.API.Services
{
    public class LogParserService : ILogParserService
    {
        private readonly string? _logPath;
        private readonly IConfiguration _configuration;
        private readonly CultureInfo provider = CultureInfo.InvariantCulture;
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
                    string timePart = GetTimeFromLine(line);

                    if (string.IsNullOrWhiteSpace(timePart)) continue;

                    DateTime lineTime = ParseLineTime(timePart);

                    if (line.Contains("InitGame"))
                    {
                        currentGame = new GameLog
                        {
                            GameId = gameCounter++,
                            StartTime = lineTime
                        };
                        games.Add(currentGame);
                    }
                    else if (line.Contains("ShutdownGame") && currentGame != null)
                    {
                        currentGame.EndTime = lineTime;
                        currentGame = null;
                    }
                    else if (currentGame != null)
                    {
                        if (line.Contains("ClientUserinfoChanged"))
                        {
                            var parts = line.Split('\\');
                            var player = parts.Length > 1 ? parts[1].Trim() : "Unknown";

                            if (!string.IsNullOrEmpty(player) && !currentGame.Players.Contains(player))
                                currentGame.Players.Add(player);
                        }
                        else if (line.Contains("Kill:"))
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

                                if (killer == "<world>")
                                {
                                    if (!currentGame.Kills.ContainsKey(victim))
                                        currentGame.Kills[victim] = 0;

                                    currentGame.Kills[victim]--;
                                    currentGame.Events.Add($"{victim} morreu por {cause} às {lineTime:HH:mm}");
                                }
                                else
                                {
                                    if (!currentGame.Kills.ContainsKey(killer))
                                        currentGame.Kills[killer] = 0;

                                    currentGame.Kills[killer]++;
                                    currentGame.Events.Add($"{killer} matou {victim} com {cause} às {lineTime:HH:mm}");
                                }
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

        private string GetTimeFromLine(string line)
        {
            var split = line.TrimStart().Split(' ', 2);
            return split.Length > 1 ? split[0] : string.Empty;
        }

        private DateTime ParseLineTime(string rawTime)
        {
            DateTime.TryParseExact(rawTime, "H:mm", provider, DateTimeStyles.None, out var result);
            return result;
        }
    }
}
