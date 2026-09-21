using Microsoft.Extensions.Logging;

namespace Soccer.Infrastructure.Logging;

public static partial class InfrastructureLogMessages
{
    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Debug,
        Message = "Firestore query started for collection {Collection}")]
    public static partial void FirestoreQueryStarted(this ILogger logger, string collection);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Debug,
        Message = "Firestore query completed for collection {Collection}. Documents: {Count}")]
    public static partial void FirestoreQueryCompleted(this ILogger logger, string collection, int count);

    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Error,
        Message = "Firestore operation failed for collection {Collection}")]
    public static partial void FirestoreOperationFailed(this ILogger logger, Exception ex, string collection);

    [LoggerMessage(
        EventId = 2004,
        Level = LogLevel.Information,
        Message = "Document {DocumentId} created in collection {Collection}")]
    public static partial void DocumentCreated(this ILogger logger, string documentId, string collection);

    [LoggerMessage(
        EventId = 2005,
        Level = LogLevel.Information,
        Message = "Document {DocumentId} deleted from collection {Collection}")]
    public static partial void DocumentDeleted(this ILogger logger, string documentId, string collection);
}