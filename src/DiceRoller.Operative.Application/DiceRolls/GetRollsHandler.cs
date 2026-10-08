using DiceRoller.BuildingBlocks.Contracts;
using DiceRoller.BuildingBlocks.Domain;
using DiceRoller.Operative.Application.Abstractions;

namespace DiceRoller.Operative.Application.DiceRolls;

public sealed class GetRollsHandler(IDiceRollRepository rolls, ICurrentUser currentUser)
{
    public async Task<Result<PagedResponse<DiceRollDto>>> Handle(RollsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await rolls.GetPageAsync(currentUser.UserId, query, cancellationToken);
    }
}
