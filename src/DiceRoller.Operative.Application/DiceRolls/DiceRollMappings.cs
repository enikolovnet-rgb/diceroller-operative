using DiceRoller.Operative.Domain.DiceRolls;

namespace DiceRoller.Operative.Application.DiceRolls;

public static class DiceRollMappings
{
    public static DiceRollDto ToDto(this DiceRoll roll)
    {
        ArgumentNullException.ThrowIfNull(roll);

        return new DiceRollDto(roll.Id, roll.Die1.Value, roll.Die2.Value, roll.Sum, roll.RolledAtUtc);
    }
}
