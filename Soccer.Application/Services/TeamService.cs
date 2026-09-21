using Microsoft.Extensions.Logging;
using Soccer.Application.DTO;
using Soccer.Application.Interfaces;
using Soccer.Application.Logging;  
using Soccer.Common.Exceptions;
using Soccer.Domain.Entities;
using Soccer.Domain.Interfaces;

namespace Soccer.Application.Services
{
    public class TeamService : IEntityService<TeamDTO>
    {
        private readonly IRepository<Team> teams;
        private readonly ILogger<TeamService> logger; // логер

        public TeamService(
            IRepository<Team> teams,
            ILogger<TeamService> logger) // DI
        {
            this.teams = teams;
            this.logger = logger;
        }

        public async Task Create(TeamDTO teamDto)
        {
            var team = new Team
            {
                Id = teamDto.Id,
                Name = teamDto.Name,
                Coach = teamDto.Coach
            };

            try
            {
                await teams.Create(team);

                // ===== LOG: успішне створення команди (EventId 1101) =====
                logger.TeamCreated(team.Id);
            }
            catch (Exception ex)
            {
                // ===== LOG: помилка створення (EventId 1103 + Exception) =====
                logger.TeamCreationFailed(ex, team.Id);
                throw;
            }
        }

        public async Task Update(TeamDTO teamDto)
        {
            var team = new Team
            {
                Id = teamDto.Id,
                Name = teamDto.Name,
                Coach = teamDto.Coach
            };

            try
            {
                await teams.Update(team);

                // ===== LOG: успішне оновлення =====
                logger.LogInformation("Team {TeamId} updated", team.Id);
            }
            catch (Exception ex)
            {
                // ===== LOG: помилка оновлення =====
                logger.LogError(ex, "Failed to update team {TeamId}", team.Id);
                throw;
            }
        }

        public async Task Delete(int id)
        {
            try
            {
                await teams.Delete(id);

                // ===== LOG: успішне видалення =====
                logger.LogInformation("Team {TeamId} deleted", id);
            }
            catch (Exception ex)
            {
                // ===== LOG: помилка видалення =====
                logger.LogError(ex, "Failed to delete team {TeamId}", id);
                throw;
            }
        }

        public async Task<TeamDTO> Get(int id)
        {
            var team = await teams.Get(id);

            if (team == null)
            {
                // ===== LOG: команда не знайдена (Warning + EventId 1102) =====
                logger.TeamNotFound(id);
                throw new ValidationException("Немає такого клуба!");
            }

            return new TeamDTO
            {
                Id = team.Id,
                Name = team.Name,
                Coach = team.Coach
            };
        }

        public async Task<IEnumerable<TeamDTO>> GetAll()
        {
            // ===== LOG: початок отримання всіх команд =====
            logger.LogDebug("Getting all teams");

            var teamsList = await teams.GetAll();

            // ===== LOG: результат =====
            logger.LogInformation("Retrieved {Count} teams", teamsList.Count());

            return teamsList.Select(team => new TeamDTO
            {
                Id = team.Id,
                Name = team.Name,
                Coach = team.Coach
            });
        }
    }
}