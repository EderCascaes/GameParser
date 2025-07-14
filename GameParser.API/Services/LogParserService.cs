using System.Globalization;
using System.Text.RegularExpressions;
using GameParser.API.Interfaces;
using GameParser.API.Models;

namespace GameParser.API.Services
{
    public class LogParserService : ILogParserService
    {
        private readonly string? _logPath;
        private readonly IConfiguration _configuration;
        private readonly CultureInfo provider = CultureInfo.InvariantCulture;
        private readonly string[]? _logLines;

        public LogParserService(IConfiguration configuration, string[]? logLines = null)
        {
            _configuration = configuration;
            _logPath = _configuration["logPath"];
            _logLines = logLines;
        }

        public List<GameLog> Parse()
        {
            try
            {
                var logs = _logLines ?? File.ReadAllLines(_logPath);
                var games = new List<GameLog>();

                GameLog? currentGame = null;
                int gameCounter = 1;
                Dictionary<string, string> playerIdToName = new();

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
                        playerIdToName.Clear();
                        Console.WriteLine($"[DEBUG] Novo jogo iniciado às {lineTime:HH:mm}");
                    }
                    else if (line.Contains("ShutdownGame") && currentGame != null)
                    {
                        currentGame.EndTime = lineTime;
                        currentGame = null;
                        Console.WriteLine($"[DEBUG] Jogo encerrado às {lineTime:HH:mm}");
                    }
                    else if (currentGame != null)
                    {
                        if (line.Contains("ClientUserinfoChanged"))
                        {
                            var id = line.Split(':')[1].Trim().Split(' ')[0];
                            var match = Regex.Match(line, @"n\\(?<name>.+?)\\");
                            var playerName = match.Success ? match.Groups["name"].Value : "Unknown";

                            playerIdToName[id] = playerName;

                            if (!string.IsNullOrEmpty(playerName) && !currentGame.Players.Contains(playerName))
                                currentGame.Players.Add(playerName);

                            Console.WriteLine($"[DEBUG] Mapeado ID {id} ␦ {playerName}");
                        }
                        else if (line.Contains("Kill:"))
                        {
                            currentGame.TotalKills++;

                            var parts = line.Split(':');
                            var killData = parts.Last().Trim();
                            var idPart = parts[1].Trim().Split(' ')[0];

                            var killInfo = killData.Split(" by ");
                            var killAction = killInfo[0].Split(" killed ");
                            var cause = killInfo.Length > 1 ? killInfo[1] : "Unknown";

                            if (killAction.Length == 2)
                            {
                                var killerRaw = killAction[0].Trim();
                                var victimRaw = killAction[1].Trim();

                                string killer = killerRaw;
                                string victim = victimRaw;

                                if (playerIdToName.ContainsKey(killerRaw))
                                    killer = playerIdToName[killerRaw];
                                if (playerIdToName.ContainsKey(victimRaw))
                                    victim = playerIdToName[victimRaw];

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
