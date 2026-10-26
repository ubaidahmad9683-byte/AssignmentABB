using TicTacToe.Domain;

namespace TicTacToe.Api.Models;

/// <summary>Request to create a new game in the selected mode.</summary>
public sealed record CreateGameRequest(GameMode Mode);

/// <summary>Request to submit one player's move using zero-based coordinates.</summary>
public sealed record SubmitMoveRequest(Player Player, int Row, int Column);

/// <summary>A move record returned in game state.</summary>
public sealed record MoveResponse(int MoveNumber, Player Player, int Row, int Column);

/// <summary>Score totals returned to API clients.</summary>
public sealed record ScoreboardResponse(int XWins, int OWins, int Draws);

/// <summary>The game state returned to API clients.</summary>
public sealed record GameResponse(
    Guid GameId,
    string?[] Board,
    Player CurrentPlayer,
    GameMode Mode,
    GameStatus Status,
    Player? Winner,
    int[] WinningCells,
    IReadOnlyList<MoveResponse> MoveHistory,
    ScoreboardResponse Scoreboard);

/// <summary>A paginated set of game states.</summary>
public sealed record GamePageResponse(IReadOnlyList<GameResponse> Items, int Page, int PageSize, int TotalCount);

/// <summary>Credentials used to request a JWT access token.</summary>
public sealed record TokenRequest(string Username, string Password);

/// <summary>JWT access token response.</summary>
public sealed record TokenResponse(string AccessToken, string TokenType);
