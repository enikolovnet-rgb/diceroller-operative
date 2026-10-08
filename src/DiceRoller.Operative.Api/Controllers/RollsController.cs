using DiceRoller.BuildingBlocks.Contracts;
using DiceRoller.BuildingBlocks.Web;
using DiceRoller.Operative.Application.DiceRolls;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiceRoller.Operative.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/rolls")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
public sealed class RollsController(ISender sender) : ControllerBase
{
    private const string GetRollRouteName = "GetRoll";

    /// <summary>Rolls two dice for the caller.</summary>
    [HttpPost]
    [ProducesResponseType<DiceRollDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Roll(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RollDiceCommand(), cancellationToken);

        return result.Match(
            roll => result.ToCreatedResult(GetRollRouteName, new { id = roll.Id }),
            _ => result.ToActionResult());
    }

    /// <summary>One of the caller's rolls; 404 when it does not exist or belongs to someone else.</summary>
    [HttpGet("{id:guid}", Name = GetRollRouteName)]
    [ProducesResponseType<DiceRollDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        (await sender.Send(new GetRollQuery(id), cancellationToken)).ToActionResult();

    /// <summary>The caller's roll history, filtered, sorted and paged.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResponse<DiceRollDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<IActionResult> List([FromQuery] RollsQuery query, CancellationToken cancellationToken) =>
        (await sender.Send(query, cancellationToken)).ToActionResult();
}
