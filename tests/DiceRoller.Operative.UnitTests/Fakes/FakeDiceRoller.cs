using DiceRoller.Operative.Domain.DiceRolls;

namespace DiceRoller.Operative.UnitTests.Fakes;

/// <summary>Returns the given values in order, one per <see cref="Roll"/>.</summary>
public sealed class FakeDiceRoller(params int[] values) : IDiceRoller
{
    private readonly Queue<int> _values = new(values);

    public int Roll() => _values.Dequeue();
}
