using DiceRoller.BuildingBlocks.Contracts;

namespace DiceRoller.Operative.Application.DiceRolls;

/// <summary>
/// Filters, sorting and paging for the caller's roll history. Deliberately has no user id:
/// the owner always comes from <see cref="Abstractions.ICurrentUser"/>.
/// </summary>
public sealed record RollsQuery
{
    public int? Year { get; init; }

    /// <summary>Requires <see cref="Year"/>.</summary>
    public int? Month { get; init; }

    /// <summary>Requires <see cref="Year"/> and <see cref="Month"/>.</summary>
    public int? Day { get; init; }

    /// <summary>When set, sum is the primary sort key and <see cref="SortByDate"/> breaks ties.</summary>
    public SortDirection? SortBySum { get; init; }

    public SortDirection? SortByDate { get; init; }

    public int Page { get; init; } = PagedQuery.DefaultPage;

    public int PageSize { get; init; } = PagedQuery.DefaultPageSize;
}
