namespace DiceRoller.Operative.Application.DiceRolls;

public sealed record DiceRollDto(Guid Id, int Die1, int Die2, int Sum, DateTime RolledAtUtc);
