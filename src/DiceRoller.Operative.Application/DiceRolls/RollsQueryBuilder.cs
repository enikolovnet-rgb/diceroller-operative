using DiceRoller.Operative.Domain.DiceRolls;

namespace DiceRoller.Operative.Application.DiceRolls;

/// <summary>
/// Turns a validated <see cref="RollsQuery"/> into LINQ: filter by owner and date range, sort, then page.
/// Pure LINQ, so it runs the same over EF Core and over an in-memory list.
/// </summary>
public static class RollsQueryBuilder
{
    public static RollsQueryPlan Build(IQueryable<DiceRoll> rolls, Guid userId, RollsQuery query)
    {
        ArgumentNullException.ThrowIfNull(rolls);
        ArgumentNullException.ThrowIfNull(query);

        var matching = Filter(rolls, userId, query);
        var page = Sort(matching, query)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize);

        return new RollsQueryPlan(matching, page);
    }

    /// <summary>
    /// The half-open UTC range <c>[Start, End)</c> selected by the date filters, or <see langword="null"/> when there are none.
    /// <c>End</c> is <see langword="null"/> when the range runs to the end of year 9999.
    /// </summary>
    public static (DateTime Start, DateTime? End)? DateRange(RollsQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Year is not { } year)
        {
            return null;
        }

        if (query.Month is not { } month)
        {
            var yearStart = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (yearStart, year == DateTime.MaxValue.Year ? null : yearStart.AddYears(1));
        }

        if (query.Day is not { } day)
        {
            var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var isLastMonth = year == DateTime.MaxValue.Year && month == 12;
            return (monthStart, isLastMonth ? null : monthStart.AddMonths(1));
        }

        var dayStart = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
        return (dayStart, dayStart == DateTime.MaxValue.Date ? null : dayStart.AddDays(1));
    }

    private static IQueryable<DiceRoll> Filter(IQueryable<DiceRoll> rolls, Guid userId, RollsQuery query)
    {
        rolls = rolls.Where(roll => roll.UserId == userId);

        if (DateRange(query) is not var (start, end))
        {
            return rolls;
        }

        rolls = rolls.Where(roll => roll.RolledAtUtc >= start);

        if (end is { } endExclusive)
        {
            rolls = rolls.Where(roll => roll.RolledAtUtc < endExclusive);
        }

        return rolls;
    }

    private static IOrderedQueryable<DiceRoll> Sort(IQueryable<DiceRoll> rolls, RollsQuery query)
    {
        var dateDirection = query.SortByDate ?? SortDirection.Desc;
        IOrderedQueryable<DiceRoll> ordered;

        if (query.SortBySum is { } sumDirection)
        {
            ordered = sumDirection == SortDirection.Asc
                ? rolls.OrderBy(roll => roll.Sum)
                : rolls.OrderByDescending(roll => roll.Sum);

            ordered = dateDirection == SortDirection.Asc
                ? ordered.ThenBy(roll => roll.RolledAtUtc)
                : ordered.ThenByDescending(roll => roll.RolledAtUtc);
        }
        else
        {
            ordered = dateDirection == SortDirection.Asc
                ? rolls.OrderBy(roll => roll.RolledAtUtc)
                : rolls.OrderByDescending(roll => roll.RolledAtUtc);
        }

        return ordered.ThenBy(roll => roll.Id);
    }
}
