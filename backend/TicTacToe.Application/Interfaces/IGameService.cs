using TicTacToe.Domain;

namespace TicTacToe.Application;

public interface IGameService
{
    Task<GameState> CreateAsync(GameMode mode, CancellationToken cancellationToken);
    Task<GameState?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<GamePage> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<GameState> PlayAsync(Guid id, Player player, int row, int column, CancellationToken cancellationToken);
    Task<GameState> UndoAsync(Guid id, CancellationToken cancellationToken);
    Task<GameState> ResetAsync(Guid id, CancellationToken cancellationToken);
    Task<Scoreboard> GetScoreboardAsync(CancellationToken cancellationToken);
    Task<Scoreboard> ResetScoreboardAsync(CancellationToken cancellationToken);
}
