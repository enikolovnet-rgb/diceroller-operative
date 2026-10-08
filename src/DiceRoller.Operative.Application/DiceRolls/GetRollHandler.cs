using DiceRoller.BuildingBlocks.Domain;
using DiceRoller.Operative.Application.Abstractions;
using DiceRoller.Operative.Domain.DiceRolls;
using MediatR;

namespace DiceRoller.Operative.Application.DiceRolls;

public sealed class GetRollHandler(IDiceRollRepository rolls, ICurrentUser currentUser)
    : IRequestHandler<GetRollQuery, Result<DiceRollDto>>
{
    public async Task<Result<DiceRollDto>> Handle(GetRollQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Scoped to the caller, so another user's roll is indistinguishable from a missing one.
        var roll = await rolls.GetByIdAsync(request.Id, currentUser.UserId, cancellationToken);

        return roll is null ? DiceRollErrors.NotFound : roll;
    }
}
