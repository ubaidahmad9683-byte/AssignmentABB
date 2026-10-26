namespace TicTacToe.Application;

public interface IUserRepository
{
    Task<ApplicationUser?> FindUserAsync(string username, CancellationToken cancellationToken);
}
