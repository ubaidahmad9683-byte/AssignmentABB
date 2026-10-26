namespace TicTacToe.Infrastructure;

public sealed class UserEntity
{
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public required string Role { get; set; }
}
