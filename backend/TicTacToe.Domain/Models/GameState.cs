namespace TicTacToe.Domain;

public sealed record MoveRecord(int MoveNumber, Player Player, int Row, int Column);
public sealed record Scoreboard(int XWins, int OWins, int Draws);

public sealed record GameState(
    Guid GameId,
    string?[] Board,
    Player CurrentPlayer,
    GameMode Mode,
    GameStatus Status,
    Player? Winner,
    int[] WinningCells,
    IReadOnlyList<MoveRecord> MoveHistory,
    Scoreboard Scoreboard);
