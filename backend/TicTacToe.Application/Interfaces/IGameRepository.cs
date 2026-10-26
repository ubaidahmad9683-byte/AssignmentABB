using TicTacToe.Domain;

namespace TicTacToe.Application;

public interface IGameRepository
{
    Task<GameState?> FindGameAsync(Guid id, CancellationToken cancellationToken);
    Task<GamePage> ListGamesAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<GameState> CreateGameAsync(GameState state, CancellationToken cancellationToken);
    Task<GameState?> UpdateGameAsync(GameState state, CancellationToken cancellationToken);
    Task<Scoreboard> GetScoreboardAsync(CancellationToken cancellationToken);
    Task<Scoreboard> ResetScoreboardAsync(CancellationToken cancellationToken);
}
