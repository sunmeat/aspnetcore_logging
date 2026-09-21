using Microsoft.Extensions.Logging;
using Soccer.Application.DTO;
using Soccer.Application.Interfaces;
using Soccer.Application.Logging; // source-generated методи
using Soccer.Common.Exceptions;
using Soccer.Domain.Entities;
using Soccer.Domain.Interfaces;

namespace Soccer.Application.Services
{
    public class PlayerService : IEntityService<PlayerDTO>
    {
        private readonly IRepository<Player> players;
        private readonly IRepository<Team> teams;
        private readonly ILogger<PlayerService> logger; // логер

        public PlayerService(
            IRepository<Player> players,
            IRepository<Team> teams,
            ILogger<PlayerService> logger) // !!! DI
        {
            this.players = players;
            this.teams = teams;
            this.logger = logger;
        }

        public async Task Create(PlayerDTO playerDto)
        {
            var player = new Player
            {
                Id = playerDto.Id,
                Name = playerDto.Name,
                Age = playerDto.Age,
                Position = playerDto.Position,
                TeamId = playerDto.TeamId
            };

            try
            {
                await players.Create(player);

                // ===== LOG: успішне створення (Information + EventId 1001) =====
                // Structured: { "Message": "Player 42 created", "PlayerId": 42 }
                logger.PlayerCreated(player.Id);
            }
            catch (Exception ex)
            {
                // ===== LOG: помилка створення (Error + Exception) =====
                // Exception передається окремим параметром → stack trace зберігається
                logger.PlayerCreationFailed(ex, player.Id);
                throw;
            }
        }

        public async Task Update(PlayerDTO playerDto)
        {
            var player = new Player
            {
                Id = playerDto.Id,
                Name = playerDto.Name,
                Age = playerDto.Age,
                Position = playerDto.Position,
                TeamId = playerDto.TeamId
            };

            try
            {
                await players.Update(player);

                // ===== LOG: успішне оновлення =====
                logger.PlayerUpdated(player.Id);
            }
            catch (Exception ex)
            {
                // ===== LOG: помилка оновлення =====
                logger.LogError(ex, "Failed to update player {PlayerId}", player.Id);
                throw;
            }
        }

        public async Task Delete(int id)
        {
            try
            {
                await players.Delete(id);

                // ===== LOG: успішне видалення =====
                logger.PlayerDeleted(id);
            }
            catch (Exception ex)
            {
                // ===== LOG: помилка видалення =====
                logger.LogError(ex, "Failed to delete player {PlayerId}", id);
                throw;
            }
        }

        public async Task<PlayerDTO> Get(int id)
        {
            var player = await players.Get(id);

            if (player == null)
            {
                // ===== LOG: гравець не знайдений (Warning + EventId 1002) =====
                logger.PlayerNotFound(id);
                throw new ValidationException("Немає такого гравця!");
            }

            string? teamName = null;

            if (player.TeamId.HasValue)
            {
                var team = await teams.Get(player.TeamId.Value);
                teamName = team?.Name;
            }

            return new PlayerDTO
            {
                Id = player.Id,
                Name = player.Name,
                Age = player.Age,
                Position = player.Position,
                TeamId = player.TeamId,
                Team = teamName
            };
        }

        public async Task<IEnumerable<PlayerDTO>> GetAll()
        {
            // ===== LOG: початок отримання всіх гравців (Debug) =====
            logger.LogDebug("Getting all players");

            var playersTask = players.GetAll();
            var teamsTask = teams.GetAll();

            await Task.WhenAll(playersTask, teamsTask);

            var playersList = (await playersTask).ToList();
            var teamsList = (await teamsTask).ToList();

            // ===== LOG: результат (Information) =====
            logger.LogInformation("Retrieved {Count} players", playersList.Count);

            var teamsDictionary = teamsList.ToDictionary(
                team => team.Id,
                team => team);

            return playersList.Select(player =>
            {
                Team? team = null;

                if (player.TeamId.HasValue)
                {
                    teamsDictionary.TryGetValue(
                        player.TeamId.Value,
                        out team);
                }

                return new PlayerDTO
                {
                    Id = player.Id,
                    Name = player.Name,
                    Age = player.Age,
                    Position = player.Position,
                    TeamId = player.TeamId,
                    Team = team?.Name
                };
            });
        }
    }
}