namespace DiceRoller.Operative.Domain.DiceRolls;

/// <summary>Produces the face of a single six-sided die.</summary>
public interface IDiceRoller
{
    /// <summary>Rolls one die. <see cref="DieValue.Create"/> rejects anything outside 1–6.</summary>
    int Roll();
}
