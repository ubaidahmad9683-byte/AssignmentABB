namespace TicTacToe.Infrastructure;

public sealed class GameEntity
{
    public Guid Id { get; set; }
    public required string StateJson { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
