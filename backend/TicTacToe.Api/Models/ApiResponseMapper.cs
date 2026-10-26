using TicTacToe.Application;
using TicTacToe.Domain;

namespace TicTacToe.Api.Models;

public static class ApiResponseMapper
{
    public static GameResponse ToResponse(GameState game) => new(
        game.GameId,
        (string?[])game.Board.Clone(),
        game.CurrentPlayer,
        game.Mode,
        game.Status,
        game.Winner,
        (int[])game.WinningCells.Clone(),
        game.MoveHistory.Select(move => new MoveResponse(move.MoveNumber, move.Player, move.Row, move.Column)).ToArray(),
        ToResponse(game.Scoreboard));

    public static GamePageResponse ToResponse(GamePage page) =>
        new(page.Items.Select(ToResponse).ToArray(), page.Page, page.PageSize, page.TotalCount);

    public static ScoreboardResponse ToResponse(Scoreboard scoreboard) =>
        new(scoreboard.XWins, scoreboard.OWins, scoreboard.Draws);
}
