using DiceRoller.BuildingBlocks.Contracts;
using DiceRoller.Operative.Application.DiceRolls;
using DiceRoller.Operative.Domain.DiceRolls;
using DiceRoller.Operative.UnitTests.Fakes;
using SortDirection = DiceRoller.Operative.Application.DiceRolls.SortDirection;

namespace DiceRoller.Operative.UnitTests.Application;

public sealed class RollsQueryBuilderTests
{
    private static readonly TimeSpan LastTick = TimeSpan.FromDays(1) - TimeSpan.FromTicks(1);

    // ---- Filtering ----

    [Fact]
    public void Build_Year_IncludesFirstAndLastInstantAndExcludesNeighbours()
    {
        var rolls = Rolls(
            Utc(2025, 12, 31) + LastTick,
            Utc(2026, 1, 1),
            Utc(2026, 7, 15, 12),
            Utc(2026, 12, 31) + LastTick,
            Utc(2027, 1, 1));

        var times = MatchingTimes(rolls, new RollsQuery { Year = 2026 });

        times.ShouldBe([Utc(2026, 1, 1), Utc(2026, 7, 15, 12), Utc(2026, 12, 31) + LastTick], ignoreOrder: true);
    }

    [Fact]
    public void Build_YearMonth_IncludesFirstAndLastInstantAndExcludesNeighbours()
    {
        var rolls = Rolls(
            Utc(2026, 2, 28) + LastTick,
            Utc(2026, 3, 1),
            Utc(2026, 3, 31) + LastTick,
            Utc(2026, 4, 1));

        var times = MatchingTimes(rolls, new RollsQuery { Year = 2026, Month = 3 });

        times.ShouldBe([Utc(2026, 3, 1), Utc(2026, 3, 31) + LastTick], ignoreOrder: true);
    }

    [Fact]
    public void Build_December_EndsAtStartOfNextYear()
    {
        var rolls = Rolls(
            Utc(2026, 11, 30) + LastTick,
            Utc(2026, 12, 1),
            Utc(2026, 12, 31) + LastTick,
            Utc(2027, 1, 1));

        var times = MatchingTimes(rolls, new RollsQuery { Year = 2026, Month = 12 });

        times.ShouldBe([Utc(2026, 12, 1), Utc(2026, 12, 31) + LastTick], ignoreOrder: true);
    }

    [Theory]
    [InlineData(2024, 29)]
    [InlineData(2026, 28)]
    public void Build_February_EndsAfterLastDayOfThatYearsFebruary(int year, int lastDay)
    {
        var rolls = Rolls(
            Utc(year, 1, 31) + LastTick,
            Utc(year, 2, 1),
            Utc(year, 2, lastDay) + LastTick,
            Utc(year, 3, 1));

        var times = MatchingTimes(rolls, new RollsQuery { Year = year, Month = 2 });

        times.ShouldBe([Utc(year, 2, 1), Utc(year, 2, lastDay) + LastTick], ignoreOrder: true);
    }

    [Fact]
    public void Build_Day_IncludesWholeDayOnly()
    {
        var rolls = Rolls(
            Utc(2026, 2, 28) + LastTick,
            Utc(2026, 3, 1),
            Utc(2026, 3, 1, 12),
            Utc(2026, 3, 1) + LastTick,
            Utc(2026, 3, 2));

        var times = MatchingTimes(rolls, new RollsQuery { Year = 2026, Month = 3, Day = 1 });

        times.ShouldBe([Utc(2026, 3, 1), Utc(2026, 3, 1, 12), Utc(2026, 3, 1) + LastTick], ignoreOrder: true);
    }

    [Fact]
    public void Build_LastDayOfYear_EndsAtStartOfNextYear()
    {
        var rolls = Rolls(Utc(2026, 12, 31), Utc(2026, 12, 31) + LastTick, Utc(2027, 1, 1));

        var times = MatchingTimes(rolls, new RollsQuery { Year = 2026, Month = 12, Day = 31 });

        times.ShouldBe([Utc(2026, 12, 31), Utc(2026, 12, 31) + LastTick], ignoreOrder: true);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(12, null)]
    [InlineData(12, 31)]
    public void Build_Year9999_HasOpenEndAndDoesNotOverflow(int? month, int? day)
    {
        var lastMoment = new DateTime(9999, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc);
        var rolls = Rolls(Utc(9998, 12, 31) + LastTick, Utc(9999, 12, 31), lastMoment);

        var times = MatchingTimes(rolls, new RollsQuery { Year = 9999, Month = month, Day = day });

        times.ShouldBe([Utc(9999, 12, 31), lastMoment], ignoreOrder: true);
    }

    [Theory]
    [InlineData(2026, null, null, 2026, 1, 1, 2027, 1, 1)]
    [InlineData(2026, 12, null, 2026, 12, 1, 2027, 1, 1)]
    [InlineData(2024, 2, null, 2024, 2, 1, 2024, 3, 1)]
    [InlineData(2026, 12, 31, 2026, 12, 31, 2027, 1, 1)]
    public void DateRange_Filters_IsHalfOpenUtcRange(
        int year, int? month, int? day, int startYear, int startMonth, int startDay, int endYear, int endMonth, int endDay)
    {
        var range = RollsQueryBuilder.DateRange(new RollsQuery { Year = year, Month = month, Day = day });

        range.ShouldNotBeNull();
        range.Value.Start.ShouldBe(Utc(startYear, startMonth, startDay));
        range.Value.Start.Kind.ShouldBe(DateTimeKind.Utc);
        range.Value.End.ShouldBe(Utc(endYear, endMonth, endDay));
    }

    [Fact]
    public void DateRange_NoFilters_IsNull()
    {
        RollsQueryBuilder.DateRange(new RollsQuery()).ShouldBeNull();
    }

    [Fact]
    public void Build_NoFilters_ReturnsAllOfTheUsersRolls()
    {
        var rolls = Rolls(Utc(1990, 1, 1), Utc(2026, 3, 1), Utc(2040, 6, 6));

        MatchingTimes(rolls, new RollsQuery()).Count.ShouldBe(3);
    }

    [Fact]
    public void Build_OtherUsersRolls_AreNeverReturnedOrCounted()
    {
        var otherUser = Guid.CreateVersion7();
        var mine = TestRolls.At(Utc(2026, 3, 1, 10));
        List<DiceRoll> rolls =
        [
            mine,
            TestRolls.At(Utc(2026, 3, 1, 11), userId: otherUser),
            TestRolls.At(Utc(2026, 3, 1, 12), userId: otherUser),
        ];

        var plan = RollsQueryBuilder.Build(rolls.AsQueryable(), TestRolls.UserId, new RollsQuery { Year = 2026 });

        plan.Matching.Count().ShouldBe(1);
        plan.Page.ShouldHaveSingleItem().ShouldBe(mine);
    }

    // ---- Sorting ----

    // Two rolls per sum, all at distinct times, so every combination gives a distinct, predictable order.
    private static readonly (string Label, int Sum, int Hour)[] SortFixture =
    [
        ("A", 7, 10),
        ("B", 7, 12),
        ("C", 4, 11),
        ("D", 12, 9),
        ("E", 4, 13),
        ("F", 12, 14),
    ];

    [Theory]
    [InlineData(null, null, "FEBCAD")] // default: date desc
    [InlineData(null, SortDirection.Desc, "FEBCAD")]
    [InlineData(null, SortDirection.Asc, "DACBEF")]
    [InlineData(SortDirection.Asc, null, "ECBAFD")] // date tiebreak defaults to desc
    [InlineData(SortDirection.Asc, SortDirection.Desc, "ECBAFD")]
    [InlineData(SortDirection.Asc, SortDirection.Asc, "CEABDF")]
    [InlineData(SortDirection.Desc, null, "FDBAEC")]
    [InlineData(SortDirection.Desc, SortDirection.Desc, "FDBAEC")]
    [InlineData(SortDirection.Desc, SortDirection.Asc, "DFABCE")]
    public void Build_SortCombination_OrdersBySumThenDate(SortDirection? sortBySum, SortDirection? sortByDate, string expected)
    {
        var labels = SortFixture.ToDictionary(
            item => TestRolls.WithSum(item.Sum, Utc(2026, 3, 10, item.Hour)),
            item => item.Label);

        var page = RollsQueryBuilder
            .Build(labels.Keys.AsQueryable(), TestRolls.UserId, new RollsQuery { SortBySum = sortBySum, SortByDate = sortByDate, PageSize = PagedQuery.MaxPageSize })
            .Page
            .ToList();

        string.Concat(page.Select(roll => labels[roll])).ShouldBe(expected);
    }

    [Fact]
    public void Build_BothSorts_SumDominatesEvenWhenDateDisagrees()
    {
        var oldHighSum = TestRolls.WithSum(12, Utc(2020, 1, 1));
        var newLowSum = TestRolls.WithSum(2, Utc(2026, 1, 1));

        var page = RollsQueryBuilder
            .Build(new[] { newLowSum, oldHighSum }.AsQueryable(), TestRolls.UserId,
                new RollsQuery { SortBySum = SortDirection.Desc, SortByDate = SortDirection.Desc })
            .Page
            .ToList();

        page.ShouldBe([oldHighSum, newLowSum]);
    }

    public static TheoryData<SortDirection?, SortDirection?> AllSortCombinations()
    {
        SortDirection?[] directions = [null, SortDirection.Asc, SortDirection.Desc];
        var data = new TheoryData<SortDirection?, SortDirection?>();
        foreach (var bySum in directions)
        {
            foreach (var byDate in directions)
            {
                data.Add(bySum, byDate);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllSortCombinations))]
    public void Build_EqualSumAndTimestamp_OrdersByIdAscending(SortDirection? sortBySum, SortDirection? sortByDate)
    {
        var instant = Utc(2026, 3, 10, 8);
        var rolls = Enumerable.Range(0, 12).Select(_ => TestRolls.WithSum(7, instant)).ToList();

        var page = RollsQueryBuilder
            .Build(rolls.AsQueryable(), TestRolls.UserId, new RollsQuery { SortBySum = sortBySum, SortByDate = sortByDate, PageSize = PagedQuery.MaxPageSize })
            .Page
            .ToList();

        page.ShouldBe(rolls.OrderBy(roll => roll.Id).ToList());
    }

    [Fact]
    public void Build_InputOrder_DoesNotAffectResult()
    {
        var rolls = CollidingRolls(15);
        var query = new RollsQuery { SortBySum = SortDirection.Asc, PageSize = 100 };

        var forward = RollsQueryBuilder.Build(rolls.AsQueryable(), TestRolls.UserId, query).Page.ToList();
        var reversed = RollsQueryBuilder.Build(Enumerable.Reverse(rolls).AsQueryable(), TestRolls.UserId, query).Page.ToList();

        reversed.ShouldBe(forward);
    }

    // ---- Paging ----

    [Theory]
    [MemberData(nameof(AllSortCombinations))]
    public void Build_PagingWithCollidingKeys_PagesPartitionTheFullOrder(SortDirection? sortBySum, SortDirection? sortByDate)
    {
        const int count = 23;
        const int pageSize = 5;
        var rolls = CollidingRolls(count);
        var all = RollsQueryBuilder
            .Build(rolls.AsQueryable(), TestRolls.UserId,
                new RollsQuery { SortBySum = sortBySum, SortByDate = sortByDate, PageSize = PagedQuery.MaxPageSize })
            .Page
            .ToList();

        var paged = Enumerable.Range(1, 5)
            .SelectMany(page => RollsQueryBuilder
                .Build(rolls.AsQueryable(), TestRolls.UserId,
                    new RollsQuery { SortBySum = sortBySum, SortByDate = sortByDate, Page = page, PageSize = pageSize })
                .Page
                .ToList())
            .ToList();

        all.Count.ShouldBe(count);
        paged.ShouldBe(all);
        paged.Distinct().Count().ShouldBe(count);
    }

    [Theory]
    [InlineData(25, 10, 1, 25, 3, 10)]
    [InlineData(25, 10, 3, 25, 3, 5)]
    [InlineData(25, 10, 4, 25, 3, 0)]
    [InlineData(20, 10, 2, 20, 2, 10)]
    [InlineData(1, 1, 1, 1, 1, 1)]
    [InlineData(0, 10, 1, 0, 0, 0)]
    public void Build_Paging_ReportsTotalCountAndTotalPages(
        int rollCount, int pageSize, int page, int expectedTotalCount, int expectedTotalPages, int expectedItems)
    {
        var rolls = CollidingRolls(rollCount);
        var query = new RollsQuery { Page = page, PageSize = pageSize };

        var response = Execute(rolls, query);

        response.TotalCount.ShouldBe(expectedTotalCount);
        response.TotalPages.ShouldBe(expectedTotalPages);
        response.Items.Count.ShouldBe(expectedItems);
        response.Page.ShouldBe(page);
        response.PageSize.ShouldBe(pageSize);
    }

    [Fact]
    public void Build_FilterAndPaging_CountsOnlyMatchingRolls()
    {
        var march = Enumerable.Range(1, 12).Select(day => TestRolls.At(Utc(2026, 3, day))).ToList();
        var april = Enumerable.Range(1, 7).Select(day => TestRolls.At(Utc(2026, 4, day))).ToList();

        var response = Execute([.. march, .. april], new RollsQuery { Year = 2026, Month = 3, Page = 2, PageSize = 5 });

        response.TotalCount.ShouldBe(12);
        response.TotalPages.ShouldBe(3);
        response.Items.Select(item => item.RolledAtUtc)
            .ShouldBe(Enumerable.Range(3, 5).Reverse().Select(day => Utc(2026, 3, day)).ToList());
    }

    // ---- Helpers ----

    /// <summary>What the repository does with the plan: count the matches, read the page.</summary>
    private static PagedResponse<DiceRollDto> Execute(IEnumerable<DiceRoll> rolls, RollsQuery query)
    {
        var plan = RollsQueryBuilder.Build(rolls.AsQueryable(), TestRolls.UserId, query);

        return new PagedResponse<DiceRollDto>(
            plan.Page.Select(roll => roll.ToDto()).ToList(),
            query.Page,
            query.PageSize,
            plan.Matching.Count());
    }

    private static List<DateTime> MatchingTimes(IEnumerable<DiceRoll> rolls, RollsQuery query)
    {
        var plan = RollsQueryBuilder.Build(rolls.AsQueryable(), TestRolls.UserId, query with { PageSize = PagedQuery.MaxPageSize });
        var page = plan.Page.Select(roll => roll.RolledAtUtc).ToList();

        plan.Matching.Count().ShouldBe(page.Count);
        return page;
    }

    /// <summary>Rolls whose sums cycle through 3 values and times through 2 instants, so most sort keys collide.</summary>
    private static List<DiceRoll> CollidingRolls(int count) =>
        Enumerable.Range(0, count)
            .Select(i => TestRolls.WithSum((i % 3 * 3) + 4, Utc(2026, 3, 10, 8 + (i % 2))))
            .ToList();

    private static List<DiceRoll> Rolls(params DateTime[] times) => times.Select(time => TestRolls.At(time)).ToList();

    private static DateTime Utc(int year, int month, int day, int hour = 0) =>
        new(year, month, day, hour, 0, 0, DateTimeKind.Utc);
}
