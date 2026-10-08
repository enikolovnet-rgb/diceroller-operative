using DiceRoller.BuildingBlocks.Domain;
using DiceRoller.Operative.Domain.DiceRolls;
using DiceRoller.Operative.UnitTests.Fakes;
using Microsoft.Extensions.Time.Testing;

namespace DiceRoller.Operative.UnitTests.Domain;

public sealed class DiceRollTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 8, 12, 30, 0, TimeSpan.Zero);
    private static readonly Guid UserId = Guid.CreateVersion7();

    private readonly FakeTimeProvider _timeProvider = new(Now);

    [Fact]
    public void Create_ValidArguments_SetsDiceSumUserAndTime()
    {
        var roll = DiceRoll.Create(UserId, new FakeDiceRoller(3, 5), _timeProvider);

        roll.UserId.ShouldBe(UserId);
        roll.Die1.ShouldBe(DieValue.Create(3));
        roll.Die2.ShouldBe(DieValue.Create(5));
        roll.Sum.ShouldBe(8);
        roll.RolledAtUtc.ShouldBe(Now.UtcDateTime);
        roll.RolledAtUtc.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void Create_ValidArguments_AssignsVersion7IdFromCurrentTime()
    {
        var roll = DiceRoll.Create(UserId, new FakeDiceRoller(1, 6), _timeProvider);

        roll.Id.Version.ShouldBe(7);
        TimestampOf(roll.Id).ShouldBe(Now);
    }

    [Fact]
    public void Create_EmptyUserId_ThrowsInvalidUser()
    {
        var exception = Should.Throw<DomainException>(
            () => DiceRoll.Create(Guid.Empty, new FakeDiceRoller(3, 5), _timeProvider));

        exception.Error.Code.ShouldBe(DiceRollErrors.InvalidUser.Code);
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(3, 7)]
    public void Create_RollerReturnsOutOfRange_ThrowsInvalidDieValue(int first, int second)
    {
        var exception = Should.Throw<DomainException>(
            () => DiceRoll.Create(UserId, new FakeDiceRoller(first, second), _timeProvider));

        exception.Error.Code.ShouldBe(DiceRollErrors.InvalidDieValue.Code);
    }

    // A version 7 Guid starts with a 48-bit big-endian Unix timestamp in milliseconds.
    private static DateTimeOffset TimestampOf(Guid id)
    {
        Span<byte> bytes = stackalloc byte[16];
        id.TryWriteBytes(bytes, bigEndian: true, out _);

        var milliseconds = 0L;
        for (var i = 0; i < 6; i++)
        {
            milliseconds = (milliseconds << 8) | bytes[i];
        }

        return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
    }
}
