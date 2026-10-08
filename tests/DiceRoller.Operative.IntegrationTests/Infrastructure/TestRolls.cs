using DiceRoller.Operative.Domain.DiceRolls;
using Microsoft.Extensions.Time.Testing;

namespace DiceRoller.Operative.IntegrationTests.Infrastructure;

public static class TestRolls
{
    /// <summary>A roll by <paramref name="userId"/> at an exact UTC instant, with dice that add up to <paramref name="sum"/>.</summary>
    public static DiceRoll At(Guid userId, DateTime utc, int sum)
    {
        var die1 = Math.Min(sum - 1, 6);
        var time = new FakeTimeProvider(new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)));

        return DiceRoll.Create(userId, new FakeDiceRoller(die1, sum - die1), time);
    }

    /// <summary>Returns the given values in order, one per <see cref="Roll"/>.</summary>
    private sealed class FakeDiceRoller(params int[] values) : IDiceRoller
    {
        private readonly Queue<int> _values = new(values);

        public int Roll() => _values.Dequeue();
    }
}
