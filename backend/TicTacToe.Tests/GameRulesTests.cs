using TicTacToe.Domain;

namespace TicTacToe.Tests;

public sealed class GameRulesTests
{
    private static readonly Scoreboard EmptyScoreboard = new(0, 0, 0);

    [Fact]
    public void ValidMoveAddsHistoryAndAlternatesTurn()
    {
        var game = GameRules.Play(GameRules.New(Guid.NewGuid(), GameMode.TwoPlayer, EmptyScoreboard), Player.X, 0, 2);
        Assert.Equal("X", game.Board[2]);
        Assert.Equal(Player.O, game.CurrentPlayer);
        Assert.Equal(new MoveRecord(1, Player.X, 0, 2), Assert.Single(game.MoveHistory));
    }

    [Fact]
    public void InvalidMoveDoesNotChangeOriginalState()
    {
        var game = GameRules.New(Guid.NewGuid(), GameMode.TwoPlayer, EmptyScoreboard);
        Assert.Throws<ArgumentOutOfRangeException>(() => GameRules.Play(game, Player.X, 3, 0));
        var afterX = GameRules.Play(game, Player.X, 0, 0);
        Assert.Throws<InvalidOperationException>(() => GameRules.Play(afterX, Player.X, 0, 1));
        Assert.Throws<InvalidOperationException>(() => GameRules.Play(afterX, Player.O, 0, 0));
        Assert.All(game.Board, cell => Assert.Null(cell));
    }

    [Theory]
    [InlineData("00-10-01-11-02")]
    [InlineData("00-01-10-11-20")]
    [InlineData("00-01-11-02-22")]
    public void DetectsRowColumnAndDiagonalWins(string sequence)
    {
        var state = GameRules.New(Guid.NewGuid(), GameMode.TwoPlayer, EmptyScoreboard);
        var moves = sequence.Split('-');
        foreach (var move in moves)
        {
            if (state.Status != GameStatus.InProgress) break;
            state = GameRules.Play(state, state.CurrentPlayer, move[0] - '0', move[1] - '0');
        }
        Assert.Equal(GameStatus.Won, state.Status);
        Assert.NotNull(state.Winner);
        Assert.Equal(3, state.WinningCells.Length);
    }

    [Fact]
    public void DetectsDrawAndRejectsFurtherMoves()
    {
        var moves = new (int Row, int Column)[]
        {
            (0, 0), (1, 1), (2, 2), (0, 2), (2, 0), (1, 0), (0, 1), (2, 1), (1, 2)
        };
        var state = Play(moves);
        Assert.Equal(GameStatus.Draw, state.Status);
        Assert.Throws<InvalidOperationException>(() => GameRules.Play(state, Player.X, 0, 0));
    }

    [Fact]
    public void ResetClearsMovesAndPreservesModeAndScoreboard()
    {
        var game = GameRules.Play(GameRules.New(Guid.NewGuid(), GameMode.TwoPlayer, new Scoreboard(1, 2, 3)), Player.X, 1, 1);
        var reset = GameRules.Reset(game);
        Assert.Equal(GameMode.TwoPlayer, reset.Mode);
        Assert.Equal(new Scoreboard(1, 2, 3), reset.Scoreboard);
        Assert.Empty(reset.MoveHistory);
        Assert.Equal(Player.X, reset.CurrentPlayer);
    }

    [Fact]
    public void UndoRemovesOneTwoPlayerMoveAndComputerPair()
    {
        var game = GameRules.New(Guid.NewGuid(), GameMode.TwoPlayer, EmptyScoreboard);
        game = GameRules.Play(game, Player.X, 0, 0);
        game = GameRules.Play(game, Player.O, 1, 1);
        var twoPlayer = GameRules.Undo(game);
        Assert.Single(twoPlayer.MoveHistory);
        Assert.Equal(Player.O, twoPlayer.CurrentPlayer);

        var computer = GameRules.New(Guid.NewGuid(), GameMode.Computer, EmptyScoreboard);
        computer = GameRules.Play(computer, Player.X, 0, 0);
        var undone = GameRules.Undo(computer);
        Assert.Empty(undone.MoveHistory);
        Assert.All(undone.Board, cell => Assert.Null(cell));
        Assert.Equal(Player.X, undone.CurrentPlayer);
    }

    [Fact]
    public void ComputerWinningAndBlockingMovesTakePriority()
    {
        Assert.Equal(2, GameRules.ChooseComputerMove(["O", "O", null, "X", "X", null, null, null, null]));
        Assert.Equal(2, GameRules.ChooseComputerMove(["X", "X", null, null, "O", null, null, null, null]));
        Assert.Equal(4, GameRules.ChooseComputerMove(["X", null, null, null, null, null, null, null, null]));
    }

    private static GameState Play((int Row, int Column)[] moves)
    {
        var state = GameRules.New(Guid.NewGuid(), GameMode.TwoPlayer, EmptyScoreboard);
        foreach (var (row, column) in moves)
            state = GameRules.Play(state, state.CurrentPlayer, row, column);
        return state;
    }
}
