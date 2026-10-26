using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using TicTacToe.Api.Models;
using TicTacToe.Application;
using TicTacToe.Domain;

namespace TicTacToe.Api.Controllers;

[ApiController]
[Route("api/games")]
[Produces("application/json")]
public sealed class GamesController(IGameService games) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<GameResponse>> Create(CreateGameRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Mode))
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
                { ["mode"] = ["Mode must be TwoPlayer or Computer."] }));
        var game = ApiResponseMapper.ToResponse(await games.CreateAsync(request.Mode, cancellationToken));
        return CreatedAtAction(nameof(GetById), new { id = game.GameId }, game);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<GamePageResponse>> GetAll(
        [FromQuery, Range(1, 1_000_000)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = ApiResponseMapper.ToResponse(await games.ListAsync(page, pageSize, cancellationToken));
        return Ok(result);
    }

    [HttpGet("{id:guid}", Name = nameof(GetById))]
    [AllowAnonymous]
    public async Task<ActionResult<GameResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var game = await games.GetAsync(id, cancellationToken);
        return game is null ? NotFound() : Ok(ApiResponseMapper.ToResponse(game));
    }

    [HttpPost("{id:guid}/moves")]
    [AllowAnonymous]
    public async Task<ActionResult<GameResponse>> Play(Guid id, SubmitMoveRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Player))
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
                { ["player"] = ["Player must be X or O."] }));
        return Ok(ApiResponseMapper.ToResponse(
            await games.PlayAsync(id, request.Player, request.Row, request.Column, cancellationToken)));
    }

    [HttpPost("{id:guid}/undo")]
    [AllowAnonymous]
    public async Task<ActionResult<GameResponse>> Undo(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponseMapper.ToResponse(await games.UndoAsync(id, cancellationToken)));

    [HttpPost("{id:guid}/reset")]
    [AllowAnonymous]
    public async Task<ActionResult<GameResponse>> Reset(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponseMapper.ToResponse(await games.ResetAsync(id, cancellationToken)));
}
