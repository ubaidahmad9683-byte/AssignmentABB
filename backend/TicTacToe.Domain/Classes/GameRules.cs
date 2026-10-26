namespace TicTacToe.Domain;

public static class GameRules
{
    private static readonly int[][] WinningLines =
    [
        [0, 1, 2], [3, 4, 5], [6, 7, 8], [0, 3, 6],
        [1, 4, 7], [2, 5, 8], [0, 4, 8], [2, 4, 6]
    ];

    public static GameState New(Guid id, GameMode mode, Scoreboard scores) =>
        new(id, new string?[9], Player.X, mode, GameStatus.InProgress, null, [], [], scores);

    public static GameState Play(GameState state, Player player, int row, int column)
    {
        if (row is < 0 or > 2 || column is < 0 or > 2)
            throw new ArgumentOutOfRangeException(nameof(row), "Row and column must be between 0 and 2.");
        if (state.Status != GameStatus.InProgress)
            throw new InvalidOperationException("The game is already complete.");
        if (player != state.CurrentPlayer)
            throw new InvalidOperationException($"It is {state.CurrentPlayer}'s turn.");
        if (state.Mode == GameMode.Computer && player != Player.X)
            throw new InvalidOperationException("Only player X can make moves in Computer mode.");

        var next = Move(state, player, row * 3 + column);
        if (next.Status == GameStatus.InProgress && next.Mode == GameMode.Computer)
            next = Move(next, Player.O, ChooseComputerMove(next.Board));
        return next;
    }

    public static GameState Undo(GameState state)
    {
        if (state.Status != GameStatus.InProgress)
            throw new InvalidOperationException("Undo is disabled after a game is complete.");
        var count = state.Mode == GameMode.Computer ? 2 : 1;
        if (state.MoveHistory.Count < count)
            throw new InvalidOperationException("There are no moves to undo.");

        var remaining = state.MoveHistory.Take(state.MoveHistory.Count - count).ToArray();
        var rebuilt = New(state.GameId, state.Mode, state.Scoreboard);
        foreach (var move in remaining)
            rebuilt = Move(rebuilt, move.Player, move.Row * 3 + move.Column);
        return rebuilt;
    }

    public static GameState Reset(GameState game) => New(game.GameId, game.Mode, game.Scoreboard);

    public static int ChooseComputerMove(IReadOnlyList<string?> board)
    {
        ArgumentNullException.ThrowIfNull(board);
        if (board.Count != 9) throw new ArgumentException("Board must have nine cells.", nameof(board));
        var move = FindImmediateMove(board, Player.O);
        if (move >= 0) return move;
        move = FindImmediateMove(board, Player.X);
        if (move >= 0) return move;
        if (board[4] is null) return 4;
        foreach (var corner in new[] { 0, 2, 6, 8 })
            if (board[corner] is null) return corner;
        for (var cell = 0; cell < 9; cell++)
            if (board[cell] is null) return cell;
        throw new InvalidOperationException("No moves remain for the computer.");
    }

    private static GameState Move(GameState state, Player player, int cell)
    {
        if (state.Board[cell] is not null)
            throw new InvalidOperationException("That cell is already occupied.");
        var board = (string?[])state.Board.Clone();
        board[cell] = player.ToString();
        var history = state.MoveHistory.Append(new MoveRecord(state.MoveHistory.Count + 1, player, cell / 3, cell % 3)).ToArray();
        var line = WinningLines.FirstOrDefault(cells =>
            board[cells[0]] is not null && board[cells[0]] == board[cells[1]] && board[cells[1]] == board[cells[2]]);
        if (line is not null)
            return state with { Board = board, MoveHistory = history, Status = GameStatus.Won, Winner = player, WinningCells = line };
        if (history.Length == 9)
            return state with { Board = board, MoveHistory = history, Status = GameStatus.Draw };
        return state with { Board = board, MoveHistory = history, CurrentPlayer = Other(player) };
    }

    private static int FindImmediateMove(IReadOnlyList<string?> board, Player player)
    {
        foreach (var line in WinningLines)
        {
            var owned = 0;
            var empty = -1;
            foreach (var cell in line)
            {
                if (board[cell] == player.ToString()) owned++;
                else if (board[cell] is null) empty = cell;
            }
            if (owned == 2 && empty >= 0) return empty;
        }
        return -1;
    }

    private static Player Other(Player player) => player == Player.X ? Player.O : Player.X;
}
