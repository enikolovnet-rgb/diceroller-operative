using DiceRoller.Operative.Domain.DiceRolls;

namespace DiceRoller.Operative.Application.DiceRolls;

/// <param name="Matching">The user's rolls that pass the filters; count this for the total.</param>
/// <param name="Page">The requested page of <paramref name="Matching"/>, in a deterministic order.</param>
public sealed record RollsQueryPlan(IQueryable<DiceRoll> Matching, IQueryable<DiceRoll> Page);
