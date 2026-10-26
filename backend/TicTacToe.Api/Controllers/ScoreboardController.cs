using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.Models;
using TicTacToe.Application;

namespace TicTacToe.Api.Controllers;

[ApiController]
[Route("api/scoreboard")]
public sealed class ScoreboardController(IGameService games) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<ScoreboardResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ScoreboardResponse>> Get(CancellationToken cancellationToken) =>
        Ok(ApiResponseMapper.ToResponse(await games.GetScoreboardAsync(cancellationToken)));

    [HttpPost("reset")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<ScoreboardResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ScoreboardResponse>> Reset(CancellationToken cancellationToken) =>
        Ok(ApiResponseMapper.ToResponse(await games.ResetScoreboardAsync(cancellationToken)));
}
