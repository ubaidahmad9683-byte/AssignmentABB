using TicTacToe.Domain;

namespace TicTacToe.Application;

public sealed record GamePage(IReadOnlyList<GameState> Items, int Page, int PageSize, int TotalCount);
