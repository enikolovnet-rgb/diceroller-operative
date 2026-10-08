using DiceRoller.BuildingBlocks.Domain;
using DiceRoller.Operative.Application.Abstractions;
using DiceRoller.Operative.Domain.DiceRolls;

namespace DiceRoller.Operative.Application.DiceRolls;

public sealed class GetRollHandler(IDiceRollRepository rolls, ICurrentUser currentUser)
{
    public async Task<Result<DiceRollDto>> Handle(Guid id, CancellationToken cancellationToken)
    {
        // Scoped to the caller, so another user's roll is indistinguishable from a missing one.
        var roll = await rolls.GetByIdAsync(id, currentUser.UserId, cancellationToken);

        return roll is null ? DiceRollErrors.NotFound : roll;
    }
}
