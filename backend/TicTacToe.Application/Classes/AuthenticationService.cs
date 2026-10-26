using Microsoft.AspNetCore.Identity;

namespace TicTacToe.Application;

public sealed class AuthenticationService(
    IUserRepository users,
    IPasswordHasher<ApplicationUser> passwordHasher,
    ITokenIssuer tokenIssuer) : IAuthenticationService
{
    public async Task<string?> CreateTokenAsync(string username, string password, CancellationToken cancellationToken)
    {
        var user = await users.FindUserAsync(username, cancellationToken);
        if (user is null) return null;
        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Failed ? null : tokenIssuer.Issue(user.Username, user.Role);
    }
}
