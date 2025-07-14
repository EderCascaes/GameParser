using GameParser.API.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;



namespace GameParser.Test
{
    public class GameParserTest_1
    {
        [Fact]
        public void Must_Parse_A_Game_With_Players_And_Kills()
        {
            //Deve_Parsear_Um_Jogo_Com_Jogadores_E_Kills

            // Arrange
            var logLines = new[]
            {
                " 9:00 InitGame:",
                " 9:01 ClientUserinfoChanged: 2 n\\Isgalamido\\t\\0\\",
                " 9:02 ClientUserinfoChanged: 3 n\\Dono da Bola\\t\\0\\",
                " 9:03 Kill: 2 3 10: Isgalamido killed Dono da Bola by MOD_RAILGUN",
                " 9:04 Kill: 1022 2 22: <world> killed Isgalamido by MOD_TRIGGER_HURT",
                " 9:05 ShutdownGame:"
            };

            var parser = new LogParserService(BuildFakeConfiguration(), logLines);

            // Act
            var result = parser.Parse();

            // Assert
            result.Should().HaveCount(1);
            var game = result[0];

            game.StartTime?.ToString("HH:mm").Should().Be("09:00");
            game.EndTime?.ToString("HH:mm").Should().Be("09:05");
            game.TotalKills.Should().Be(2);
            game.Kills.Should().ContainKey("Dono da Bola");
            game.Players.Should().BeEquivalentTo(new[] { "Isgalamido", "Dono da Bola" });
            game.Kills["Isgalamido"].Should().Be(1);
            game.Kills["Dono da Bola"].Should().Be(0); // não matou ninguém
            game.Events.Should().Contain("Isgalamido matou Dono da Bola com MOD_RAILGUN às 09:03");
            game.Events.Should().Contain("Isgalamido morreu por MOD_TRIGGER_HURT às 09:04");
        }

        [Fact]
        public void Should_Ignore_Blank_Lines_Or_Spaces()
        {
            //Deve_Ignorar_Linhas_Em_Branco_Ou_Espacos
            var logLines = new[]
            {
                "",
                "    ",
                " 10:00 InitGame:",
                " 10:01 ShutdownGame:"
            };

            var parser = new LogParserService(BuildFakeConfiguration(), logLines);
            var result = parser.Parse();

            result.Should().HaveCount(1);
            result[0].Players.Should().BeEmpty();
            result[0].TotalKills.Should().Be(0);
        }

        [Fact]
        public void Must_Ignore_Malformed_Kills_And_Count_Total()
        {
            //Deve_Ignorar_Kills_Malformados_E_Contar_Total

            var logLines = new[]
            {
            " 11:00 InitGame:",
            " 11:01 Kill: isso não é uma linha válida",
            " 11:02 ShutdownGame:"
            };

            var parser = new LogParserService(BuildFakeConfiguration(), logLines);
            var result = parser.Parse();

            result.Should().HaveCount(1);
            result[0].TotalKills.Should().Be(1); // ainda conta 1 kill
            result[0].Events.Should().BeEmpty(); // mas não gera evento porque não foi parseado corretamente
        }

        private IConfiguration BuildFakeConfiguration()
        {
            var settings = new Dictionary<string, string>
            {
                { "logPath", "fake.log" }
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();
        }

    }

}