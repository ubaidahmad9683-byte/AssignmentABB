using Microsoft.Extensions.Logging;
using TicTacToe.Domain;

namespace TicTacToe.Application;

public sealed class GameService(IGameRepository repository, ICacheAside cache, ILogger<GameService> logger) : IGameService
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(2);
    private const string ScoreboardKey = "scoreboard:v1";

    public async Task<GameState> CreateAsync(GameMode mode, CancellationToken cancellationToken)
    {
        var state = GameRules.New(Guid.NewGuid(), mode, await repository.GetScoreboardAsync(cancellationToken));
        return await repository.CreateGameAsync(state, cancellationToken);
    }

    public async Task<GameState?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var state = await cache.GetOrCreateAsync($"game:{id:N}", ct => repository.FindGameAsync(id, ct), CacheLifetime, cancellationToken);
        if (state is null) return null;
        return state with { Scoreboard = await GetScoreboardAsync(cancellationToken) };
    }

    public Task<GamePage> ListAsync(int page, int pageSize, CancellationToken cancellationToken) =>
        repository.ListGamesAsync(page, pageSize, cancellationToken);

    public async Task<GameState> PlayAsync(Guid id, Player player, int row, int column, CancellationToken cancellationToken)
    {
        var current = await repository.FindGameAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Game session was not found.");
        var updated = GameRules.Play(current, player, row, column);
        var result = await repository.UpdateGameAsync(updated, cancellationToken) ?? throw new KeyNotFoundException("Game session was not found.");
        await InvalidateAsync(id, cancellationToken);
        if (updated.Status != GameStatus.InProgress) await cache.RemoveAsync(ScoreboardKey, cancellationToken);
        return result;
    }

    public async Task<GameState> UndoAsync(Guid id, CancellationToken cancellationToken)
    {
        var current = await repository.FindGameAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Game session was not found.");
        var updated = GameRules.Undo(current);
        var result = await repository.UpdateGameAsync(updated, cancellationToken) ?? throw new KeyNotFoundException("Game session was not found.");
        await InvalidateAsync(id, cancellationToken);
        return result;
    }

    public async Task<GameState> ResetAsync(Guid id, CancellationToken cancellationToken)
    {
        var current = await repository.FindGameAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Game session was not found.");
        var result = await repository.UpdateGameAsync(GameRules.Reset(current), cancellationToken) ?? throw new KeyNotFoundException("Game session was not found.");
        await InvalidateAsync(id, cancellationToken);
        return result;
    }

    public Task<Scoreboard> GetScoreboardAsync(CancellationToken cancellationToken) =>
        cache.GetOrCreateAsync(ScoreboardKey, repository.GetScoreboardAsync, CacheLifetime, cancellationToken);

    public async Task<Scoreboard> ResetScoreboardAsync(CancellationToken cancellationToken)
    {
        var scoreboard = await repository.ResetScoreboardAsync(cancellationToken);
        await cache.RemoveAsync(ScoreboardKey, cancellationToken);
        logger.LogInformation("Scoreboard reset");
        return scoreboard;
    }

    private async Task InvalidateAsync(Guid id, CancellationToken cancellationToken) =>
        await cache.RemoveAsync($"game:{id:N}", cancellationToken);
}
