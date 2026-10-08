using System.Globalization;
using DiceRoller.BuildingBlocks.Domain;

namespace DiceRoller.Operative.Domain.DiceRolls;

/// <summary>The face of a six-sided die, from 1 to 6.</summary>
public sealed record DieValue
{
    public const int MinValue = 1;
    public const int MaxValue = 6;

    private DieValue(int value)
    {
        Value = value;
    }

    public int Value { get; }

    /// <exception cref="DomainException"><see cref="DiceRollErrors.InvalidDieValue"/> when the value is outside 1–6.</exception>
    public static DieValue Create(int value)
    {
        Guard.Against(value is < MinValue or > MaxValue, DiceRollErrors.InvalidDieValue);

        return new DieValue(value);
    }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
