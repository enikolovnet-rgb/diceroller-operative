using DiceRoller.BuildingBlocks.Domain;

namespace DiceRoller.Operative.Domain.DiceRolls;

/// <summary>Two dice rolled by one user at one moment.</summary>
public sealed class DiceRoll : Entity<Guid>
{
    private DiceRoll(Guid id, Guid userId, DieValue die1, DieValue die2, DateTime rolledAtUtc)
        : base(id)
    {
        UserId = userId;
        Die1 = die1;
        Die2 = die2;
        Sum = die1.Value + die2.Value;
        RolledAtUtc = rolledAtUtc;
    }

    private DiceRoll()
    {
        Die1 = null!;
        Die2 = null!;
    }

    /// <summary>The owner. A user id from UserAccess; there is no foreign key to another service's data.</summary>
    public Guid UserId { get; private set; }

    public DieValue Die1 { get; private set; }

    public DieValue Die2 { get; private set; }

    /// <summary>Derived from <see cref="Die1"/> and <see cref="Die2"/>; stored so it can be indexed and sorted on.</summary>
    public int Sum { get; private set; }

    public DateTime RolledAtUtc { get; private set; }

    /// <exception cref="DomainException">
    /// <see cref="DiceRollErrors.InvalidUser"/> when <paramref name="userId"/> is empty;
    /// <see cref="DiceRollErrors.InvalidDieValue"/> when the roller returns a value outside 1–6.
    /// </exception>
    public static DiceRoll Create(Guid userId, IDiceRoller roller, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(roller);
        ArgumentNullException.ThrowIfNull(time);

        Guard.Against(userId == Guid.Empty, DiceRollErrors.InvalidUser);

        var die1 = DieValue.Create(roller.Roll());
        var die2 = DieValue.Create(roller.Roll());
        var now = time.GetUtcNow();

        return new DiceRoll(Guid.CreateVersion7(now), userId, die1, die2, now.UtcDateTime);
    }
}
