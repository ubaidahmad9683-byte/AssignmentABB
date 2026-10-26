namespace TicTacToe.Application;

public interface ITokenIssuer
{
    string Issue(string username, string role);
}
