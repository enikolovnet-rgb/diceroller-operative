using DiceRoller.BuildingBlocks.Domain;

namespace DiceRoller.Operative.Domain.DiceRolls;

public static class DiceRollErrors
{
    public static readonly Error NotFound =
        Error.NotFound("DiceRoll.NotFound", "The dice roll was not found.");

    public static readonly Error InvalidDieValue =
        Error.Validation("DiceRoll.InvalidDieValue", $"A die value must be between {DieValue.MinValue} and {DieValue.MaxValue}.");

    public static readonly Error InvalidUser =
        Error.Validation("DiceRoll.InvalidUser", "A dice roll must belong to a user.");
}
