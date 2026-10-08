using DiceRoller.BuildingBlocks.Domain;
using DiceRoller.Operative.Application.Abstractions;
using DiceRoller.Operative.Domain.DiceRolls;
using MediatR;

namespace DiceRoller.Operative.Application.DiceRolls;

public sealed class RollDiceHandler(
    IDiceRollRepository rolls,
    ICurrentUser currentUser,
    IDiceRoller diceRoller,
    TimeProvider timeProvider) : IRequestHandler<RollDiceCommand, Result<DiceRollDto>>
{
    public async Task<Result<DiceRollDto>> Handle(RollDiceCommand request, CancellationToken cancellationToken)
    {
        var roll = DiceRoll.Create(currentUser.UserId, diceRoller, timeProvider);

        await rolls.AddAsync(roll, cancellationToken);

        return roll.ToDto();
    }
}
