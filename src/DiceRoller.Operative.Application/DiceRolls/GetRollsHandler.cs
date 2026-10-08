using DiceRoller.BuildingBlocks.Contracts;
using DiceRoller.BuildingBlocks.Domain;
using DiceRoller.Operative.Application.Abstractions;
using MediatR;

namespace DiceRoller.Operative.Application.DiceRolls;

public sealed class GetRollsHandler(IDiceRollRepository rolls, ICurrentUser currentUser)
    : IRequestHandler<RollsQuery, Result<PagedResponse<DiceRollDto>>>
{
    public async Task<Result<PagedResponse<DiceRollDto>>> Handle(RollsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await rolls.GetPageAsync(currentUser.UserId, request, cancellationToken);
    }
}
