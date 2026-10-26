namespace TicTacToe.Infrastructure;

public sealed class ScoreboardEntity
{
    public int Id { get; set; } = 1;
    public int XWins { get; set; }
    public int OWins { get; set; }
    public int Draws { get; set; }
}
