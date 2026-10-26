using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TicTacToe.Application;

namespace TicTacToe.Api;

public sealed class JwtTokenIssuer(IConfiguration configuration) : ITokenIssuer
{
    public string Issue(string username, string role)
    {
        var key = configuration["Authentication:Jwt:SigningKey"]
            ?? throw new InvalidOperationException("JWT signing key is missing.");
        var issuer = configuration["Authentication:Jwt:Issuer"]!;
        var audience = configuration["Authentication:Jwt:Audience"]!;
        var expirationMinutes = configuration.GetValue<int>("Authentication:Jwt:LifetimeMinutes", 30);
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience,
            [new Claim(ClaimTypes.Name, username), new Claim(ClaimTypes.Role, role)],
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
