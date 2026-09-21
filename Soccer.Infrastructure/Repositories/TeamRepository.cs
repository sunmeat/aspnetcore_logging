using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using Soccer.Domain.Entities;
using Soccer.Domain.Interfaces;
using Soccer.Infrastructure.Logging; 

namespace Soccer.Infrastructure.Repositories
{
    public class TeamRepository : IRepository<Team>
    {
        private readonly CollectionReference collection;
        private readonly ILogger<TeamRepository> logger; // логер

        public TeamRepository(
            FirestoreDb db,
            ILogger<TeamRepository> logger) // DI
        {
            collection = db.Collection("teams");
            this.logger = logger;
        }

        public async Task<IEnumerable<Team>> GetAll()
        {
            // ===== LOG: початок запиту до Firestore =====
            logger.FirestoreQueryStarted("teams");

            try
            {
                QuerySnapshot snapshot = await collection.GetSnapshotAsync();

                var result = snapshot.Documents
                    .Select(MapDocument)
                    .OrderBy(team => team.Id)
                    .ToList();

                // ===== LOG: запит успішно завершено =====
                logger.FirestoreQueryCompleted("teams", result.Count);

                return result;
            }
            catch (Exception ex)
            {
                // ===== LOG: помилка Firestore =====
                logger.FirestoreOperationFailed(ex, "teams");
                throw;
            }
        }

        public async Task<Team?> Get(int id)
        {
            // ===== LOG: отримання документа за Id =====
            logger.LogDebug("Getting team document {DocumentId}", id);

            try
            {
                DocumentSnapshot document =
                    await collection
                        .Document(id.ToString())
                        .GetSnapshotAsync();

                if (!document.Exists)
                {
                    logger.LogDebug("Team document {DocumentId} not found", id);
                    return null;
                }

                return MapDocument(document);
            }
            catch (Exception ex)
            {
                logger.FirestoreOperationFailed(ex, "teams");
                throw;
            }
        }

        public async Task<Team?> Get(string name)
        {
            // ===== LOG: пошук за іменем =====
            logger.LogDebug("Searching team by name {Name}", name);

            try
            {
                QuerySnapshot snapshot = await collection
                    .WhereEqualTo("name", name)
                    .Limit(1)
                    .GetSnapshotAsync();

                DocumentSnapshot? document =
                    snapshot.Documents.FirstOrDefault();

                if (document == null)
                    return null;

                return MapDocument(document);
            }
            catch (Exception ex)
            {
                logger.FirestoreOperationFailed(ex, "teams");
                throw;
            }
        }

        public async Task Create(Team team)
        {
            try
            {
                QuerySnapshot snapshot =
                    await collection.GetSnapshotAsync();

                int maxId = snapshot.Documents
                    .Select(document =>
                        Convert.ToInt32(document.ToDictionary()["id"]))
                    .DefaultIfEmpty(0)
                    .Max();

                int newId = maxId + 1;

                DocumentReference document =
                    collection.Document(newId.ToString());

                await document.SetAsync(new
                {
                    id = newId,
                    name = team.Name,
                    coach = team.Coach
                });

                // ===== LOG: документ успішно створено =====
                logger.DocumentCreated(newId.ToString(), "teams");
            }
            catch (Exception ex)
            {
                logger.FirestoreOperationFailed(ex, "teams");
                throw;
            }
        }

        public async Task Update(Team team)
        {
            try
            {
                DocumentReference document =
                    collection.Document(team.Id.ToString());

                await document.SetAsync(new
                {
                    id = team.Id,
                    name = team.Name,
                    coach = team.Coach
                });

                // ===== LOG: документ оновлено =====
                logger.LogInformation(
                    "Document {DocumentId} updated in collection {Collection}",
                    team.Id,
                    "teams");
            }
            catch (Exception ex)
            {
                logger.FirestoreOperationFailed(ex, "teams");
                throw;
            }
        }

        public async Task Delete(int id)
        {
            try
            {
                await collection
                    .Document(id.ToString())
                    .DeleteAsync();

                // ===== LOG: документ видалено =====
                logger.DocumentDeleted(id.ToString(), "teams");
            }
            catch (Exception ex)
            {
                logger.FirestoreOperationFailed(ex, "teams");
                throw;
            }
        }

        private static Team MapDocument(
            DocumentSnapshot document)
        {
            Dictionary<string, object> data =
                document.ToDictionary();

            return new Team
            {
                Id = Convert.ToInt32(data["id"]),
                Name = data["name"]?.ToString(),
                Coach = data["coach"]?.ToString()
            };
        }
    }
}