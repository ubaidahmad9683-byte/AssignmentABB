using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TicTacToe.Api.Models;
using TicTacToe.Application;

namespace TicTacToe.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthenticationService authentication) : ControllerBase
{
    [HttpPost("token")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> CreateToken(TokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
                { ["credentials"] = ["Username and password are required."] }));

        var token = await authentication.CreateTokenAsync(request.Username, request.Password, cancellationToken);
        return token is null
            ? Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid credentials")
            : Ok(new TokenResponse(token, "Bearer"));
    }
}
