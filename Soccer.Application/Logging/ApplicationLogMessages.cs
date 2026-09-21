using Microsoft.Extensions.Logging;

namespace Soccer.Application.Logging;

/// <summary>
/// Source-generated logging methods для Application-шару.
/// EventId дозволяє однозначно ідентифікувати тип події в системах моніторингу.
/// </summary>
public static partial class ApplicationLogMessages
{
    // ===== Player =====

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Player {PlayerId} created")]
    public static partial void PlayerCreated(this ILogger logger, int playerId);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Warning,
        Message = "Player {PlayerId} not found")]
    public static partial void PlayerNotFound(this ILogger logger, int playerId);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Error,
        Message = "Failed to create player {PlayerId}")]
    public static partial void PlayerCreationFailed(this ILogger logger, Exception ex, int playerId);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Information,
        Message = "Player {PlayerId} updated")]
    public static partial void PlayerUpdated(this ILogger logger, int playerId);

    [LoggerMessage(
        EventId = 1005,
        Level = LogLevel.Information,
        Message = "Player {PlayerId} deleted")]
    public static partial void PlayerDeleted(this ILogger logger, int playerId);

    // ===== Team =====

    [LoggerMessage(
        EventId = 1101,
        Level = LogLevel.Information,
        Message = "Team {TeamId} created")]
    public static partial void TeamCreated(this ILogger logger, int teamId);

    [LoggerMessage(
        EventId = 1102,
        Level = LogLevel.Warning,
        Message = "Team {TeamId} not found")]
    public static partial void TeamNotFound(this ILogger logger, int teamId);

    [LoggerMessage(
        EventId = 1103,
        Level = LogLevel.Error,
        Message = "Failed to create team {TeamId}")]
    public static partial void TeamCreationFailed(this ILogger logger, Exception ex, int teamId);
}