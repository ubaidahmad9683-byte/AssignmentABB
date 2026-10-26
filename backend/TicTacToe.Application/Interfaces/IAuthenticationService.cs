namespace TicTacToe.Application;

public interface IAuthenticationService
{
    Task<string?> CreateTokenAsync(string username, string password, CancellationToken cancellationToken);
}
