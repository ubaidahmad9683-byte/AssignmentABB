using System.Text.Json;
using System.Data;
using Microsoft.EntityFrameworkCore;
using TicTacToe.Application;
using TicTacToe.Domain;
using TicTacToe.Infrastructure;

namespace TicTacToe.Infrastructure.Repository.Classes;

public sealed class GameRepository(GameDbContext db, IRepository<GameEntity> games) : IGameRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<GameState?> FindGameAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.Games.AsNoTracking().SingleOrDefaultAsync(game => game.Id == id, cancellationToken);
        return entity is null ? null : WithScoreboard(Deserialize(entity.StateJson), await GetScoreboardAsync(cancellationToken));
    }

    public async Task<GamePage> ListGamesAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var count = await db.Games.AsNoTracking().CountAsync(cancellationToken);
        var rows = await db.Games.AsNoTracking()
            .OrderByDescending(game => game.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(game => game.StateJson)
            .ToListAsync(cancellationToken);
        var scores = await GetScoreboardAsync(cancellationToken);
        return new GamePage(rows.Select(json => WithScoreboard(Deserialize(json), scores)).ToArray(), page, pageSize, count);
    }

    public async Task<GameState> CreateGameAsync(GameState state, CancellationToken cancellationToken)
    {
        await games.AddAsync(new GameEntity
        {
            Id = state.GameId,
            StateJson = JsonSerializer.Serialize(state, JsonOptions),
            UpdatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);
        await games.SaveChangesAsync(cancellationToken);
        return WithScoreboard(state, await GetScoreboardAsync(cancellationToken));
    }

    public async Task<GameState?> UpdateGameAsync(GameState state, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var entity = await db.Games.SingleOrDefaultAsync(game => game.Id == state.GameId, cancellationToken);
        if (entity is null) return null;
        var oldState = Deserialize(entity.StateJson);
        var historyIsValidExtension = state.MoveHistory.Count >= oldState.MoveHistory.Count &&
            oldState.MoveHistory.SequenceEqual(state.MoveHistory.Take(oldState.MoveHistory.Count));
        var historyIsValidUndo = state.MoveHistory.Count < oldState.MoveHistory.Count &&
            oldState.MoveHistory.Take(state.MoveHistory.Count).SequenceEqual(state.MoveHistory);
        if (!historyIsValidExtension && !historyIsValidUndo)
            throw new InvalidOperationException("The game was changed by another request. Refresh and retry.");

        entity.StateJson = JsonSerializer.Serialize(state, JsonOptions);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        if (oldState.Status == GameStatus.InProgress && state.Status == GameStatus.Won)
        {
            var column = state.Winner == Player.X
                ? db.Scoreboards.Where(value => value.Id == 1).ExecuteUpdateAsync(update =>
                    update.SetProperty(value => value.XWins, value => value.XWins + 1), cancellationToken)
                : db.Scoreboards.Where(value => value.Id == 1).ExecuteUpdateAsync(update =>
                    update.SetProperty(value => value.OWins, value => value.OWins + 1), cancellationToken);
            if (await column == 0)
                db.Scoreboards.Add(new ScoreboardEntity
                {
                    XWins = state.Winner == Player.X ? 1 : 0,
                    OWins = state.Winner == Player.O ? 1 : 0
                });
        }
        else if (oldState.Status == GameStatus.InProgress && state.Status == GameStatus.Draw)
        {
            var affected = await db.Scoreboards.Where(value => value.Id == 1)
                .ExecuteUpdateAsync(update => update.SetProperty(value => value.Draws, value => value.Draws + 1), cancellationToken);
            if (affected == 0) db.Scoreboards.Add(new ScoreboardEntity { Draws = 1 });
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return WithScoreboard(state, await GetScoreboardAsync(cancellationToken));
    }

    public async Task<Scoreboard> GetScoreboardAsync(CancellationToken cancellationToken)
    {
        var entity = await db.Scoreboards.AsNoTracking().SingleOrDefaultAsync(value => value.Id == 1, cancellationToken);
        return entity is null ? new Scoreboard(0, 0, 0) : ToScoreboard(entity);
    }

    public async Task<Scoreboard> ResetScoreboardAsync(CancellationToken cancellationToken)
    {
        var scoreboard = await db.Scoreboards.SingleOrDefaultAsync(value => value.Id == 1, cancellationToken);
        if (scoreboard is null)
        {
            scoreboard = new ScoreboardEntity();
            db.Scoreboards.Add(scoreboard);
        }
        scoreboard.XWins = scoreboard.OWins = scoreboard.Draws = 0;
        await db.SaveChangesAsync(cancellationToken);
        return ToScoreboard(scoreboard);
    }

    private static GameState Deserialize(string json) =>
        JsonSerializer.Deserialize<GameState>(json, JsonOptions) ?? throw new InvalidDataException("Stored game data is invalid.");

    private static Scoreboard ToScoreboard(ScoreboardEntity entity) => new(entity.XWins, entity.OWins, entity.Draws);

    private static GameState WithScoreboard(GameState state, Scoreboard scoreboard) => state with { Scoreboard = scoreboard };
}

public sealed class UserRepository(GameDbContext db) : IUserRepository
{
    public async Task<ApplicationUser?> FindUserAsync(string username, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking()
            .Where(candidate => candidate.Username == username)
            .Select(candidate => new { candidate.Username, candidate.PasswordHash, candidate.Role })
            .SingleOrDefaultAsync(cancellationToken);
        return user is null ? null : new ApplicationUser(user.Username, user.PasswordHash, user.Role);
    }
}
