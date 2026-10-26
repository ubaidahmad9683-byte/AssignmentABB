namespace TicTacToe.Application;

public sealed record ApplicationUser(string Username, string PasswordHash, string Role);
