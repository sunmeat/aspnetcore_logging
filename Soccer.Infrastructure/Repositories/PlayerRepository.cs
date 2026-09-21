using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using Soccer.Domain.Entities;
using Soccer.Domain.Interfaces;
using Soccer.Infrastructure.Logging; // source-generated методи

namespace Soccer.Infrastructure.Repositories
{
    public class PlayerRepository : IRepository<Player>
    {
        private readonly CollectionReference collection;
        private readonly ILogger<PlayerRepository> logger; // логер

        public PlayerRepository(
            FirestoreDb db,
            ILogger<PlayerRepository> logger) // інжектимо через DI
        {
            collection = db.Collection("players");
            this.logger = logger;
        }

        public async Task<IEnumerable<Player>> GetAll()
        {
            // ===== LOG: початок запиту до Firestore (Debug) =====
            logger.FirestoreQueryStarted("players");

            try
            {
                QuerySnapshot snapshot = await collection.GetSnapshotAsync();

                var result = snapshot.Documents
                    .Select(MapDocument)
                    .OrderBy(player => player.Id)
                    .ToList();

                // ===== LOG: запит успішно завершено =====
                logger.FirestoreQueryCompleted("players", result.Count);

                return result;
            }
            catch (Exception ex)
            {
                // ===== LOG: помилка Firestore (Error + Exception) =====
                logger.FirestoreOperationFailed(ex, "players");
                throw;
            }
        }

        public async Task<Player?> Get(int id)
        {
            // ===== LOG: отримання документа за Id =====
            logger.LogDebug("Getting player document {DocumentId}", id);

            try
            {
                DocumentSnapshot document =
                    await collection
                        .Document(id.ToString())
                        .GetSnapshotAsync();

                if (!document.Exists)
                {
                    // ===== LOG: документ не знайдено =====
                    logger.LogDebug("Player document {DocumentId} not found", id);
                    return null;
                }

                return MapDocument(document);
            }
            catch (Exception ex)
            {
                // ===== LOG: помилка Firestore =====
                logger.FirestoreOperationFailed(ex, "players");
                throw;
            }
        }

        public async Task<Player?> Get(string name)
        {
            // ===== LOG: пошук за іменем =====
            logger.LogDebug("Searching player by name {Name}", name);

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
                logger.FirestoreOperationFailed(ex, "players");
                throw;
            }
        }

        public async Task Create(Player player)
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
                    name = player.Name,
                    age = player.Age,
                    position = player.Position,
                    team_id = player.TeamId
                });

                // ===== LOG: документ успішно створено (Information) =====
                // Structured: { "DocumentId": "27", "Collection": "players" }
                logger.DocumentCreated(newId.ToString(), "players");
            }
            catch (Exception ex)
            {
                // ===== LOG: помилка створення =====
                logger.FirestoreOperationFailed(ex, "players");
                throw;
            }
        }

        public async Task Update(Player player)
        {
            try
            {
                DocumentReference document =
                    collection.Document(player.Id.ToString());

                await document.SetAsync(new
                {
                    id = player.Id,
                    name = player.Name,
                    age = player.Age,
                    position = player.Position,
                    team_id = player.TeamId
                });

                // ===== LOG: документ оновлено =====
                logger.LogInformation(
                    "Document {DocumentId} updated in collection {Collection}",
                    player.Id,
                    "players");
            }
            catch (Exception ex)
            {
                logger.FirestoreOperationFailed(ex, "players");
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

                // ===== LOG: документ видалено (Information) =====
                logger.DocumentDeleted(id.ToString(), "players");
            }
            catch (Exception ex)
            {
                logger.FirestoreOperationFailed(ex, "players");
                throw;
            }
        }

        private static Player MapDocument(
            DocumentSnapshot document)
        {
            Dictionary<string, object> data =
                document.ToDictionary();

            return new Player
            {
                Id = Convert.ToInt32(data["id"]),
                Name = data["name"]?.ToString(),
                Age = Convert.ToInt32(data["age"]),
                Position = data["position"]?.ToString(),
                TeamId = data["team_id"] == null
                    ? null
                    : Convert.ToInt32(data["team_id"])
            };
        }
    }
}