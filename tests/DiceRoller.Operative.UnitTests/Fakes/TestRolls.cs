using DiceRoller.Operative.Domain.DiceRolls;
using Microsoft.Extensions.Time.Testing;

namespace DiceRoller.Operative.UnitTests.Fakes;

public static class TestRolls
{
    public static readonly Guid UserId = Guid.Parse("0199b8a0-0000-7000-8000-000000000001");

    /// <summary>A roll by <see cref="UserId"/> (or <paramref name="userId"/>) at an exact UTC instant.</summary>
    public static DiceRoll At(DateTime utc, int die1 = 1, int die2 = 1, Guid? userId = null)
    {
        var time = new FakeTimeProvider(new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)));

        return DiceRoll.Create(userId ?? UserId, new FakeDiceRoller(die1, die2), time);
    }

    /// <summary>A roll by <see cref="UserId"/> with the given sum, split across the two dice.</summary>
    public static DiceRoll WithSum(int sum, DateTime utc)
    {
        var die1 = Math.Min(sum - 1, 6);

        return At(utc, die1, sum - die1);
    }
}
