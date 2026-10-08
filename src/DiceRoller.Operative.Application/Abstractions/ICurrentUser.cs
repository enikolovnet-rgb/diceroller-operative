namespace DiceRoller.Operative.Application.Abstractions;

/// <summary>The authenticated caller. The only source of a user id; requests never carry one.</summary>
public interface ICurrentUser
{
    /// <summary>The <c>sub</c> claim of the validated token.</summary>
    Guid UserId { get; }
}
