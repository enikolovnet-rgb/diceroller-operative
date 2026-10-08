using System.Net.Http.Json;
using DiceRoller.BuildingBlocks.Contracts;
using DiceRoller.BuildingBlocks.Web;
using DiceRoller.Operative.Application.DiceRolls;
using DiceRoller.Operative.Domain.DiceRolls;
using DiceRoller.Operative.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace DiceRoller.Operative.IntegrationTests;

/// <summary>Filtering, sorting and paging translated by EF Core and executed by SQL Server.</summary>
public sealed class RollsQueryTests(OperativeApiFactory factory)
{
    private const string RollsPath = "/api/v1/rolls";

    [Fact]
    public async Task List_MonthFilterSumDescDateAscPage2_ReturnsExpectedPage()
    {
        var (userId, rolls) = await SeedOctober2026Async();
        var client = factory.CreateClient(userId);

        var page = await ListAsync(client, "year=2026&month=10&sortBySum=desc&sortByDate=asc&page=2&pageSize=3");

        // Full order: r5 (12, 10-02), r2 (12, 10-05), r7 (9), r1 (7, 10-01), r3 (7, 10-03), r6 (7, 10-20), r4 (2).
        page.Items.Select(roll => roll.Id).ShouldBe([rolls.R1.Id, rolls.R3.Id, rolls.R6.Id]);
        page.Page.ShouldBe(2);
        page.PageSize.ShouldBe(3);
        page.TotalCount.ShouldBe(7);
        page.TotalPages.ShouldBe(3);
    }

    [Fact]
    public async Task List_MonthFilterSumAscDateDescPage1_ReturnsExpectedPage()
    {
        var (userId, rolls) = await SeedOctober2026Async();
        var client = factory.CreateClient(userId);

        var page = await ListAsync(client, "year=2026&month=10&sortBySum=asc&sortByDate=desc&page=1&pageSize=4");

        // Full order: r4 (2), r6 (7, 10-20), r3 (7, 10-03), r1 (7, 10-01), r7 (9), r2 (12, 10-05), r5 (12, 10-02).
        page.Items.Select(roll => roll.Id).ShouldBe([rolls.R4.Id, rolls.R6.Id, rolls.R3.Id, rolls.R1.Id]);
        page.TotalCount.ShouldBe(7);
    }

    public static TheoryData<string, string[]> InvalidQueries() => new()
    {
        { "month=2", ["Month"] },
        { "year=2026&day=8", ["Day"] },
        { "year=2026&month=2&day=30", ["Day"] },
        { "page=0", ["Page"] },
        { "pageSize=101", ["PageSize"] },
        { "year=2026&day=8&page=0&pageSize=101", ["Day", "Page", "PageSize"] },
    };

    [Theory]
    [MemberData(nameof(InvalidQueries))]
    public async Task List_InvalidQuery_Returns400WithPerFieldErrors(string queryString, string[] expectedFields)
    {
        var client = factory.CreateClient(Guid.CreateVersion7());

        var response = await client.GetAsync($"{RollsPath}?{queryString}", TestContext.Current.CancellationToken);

        var body = await response.ShouldBeProblemAsync(StatusCodes.Status400BadRequest, ValidationFilter.ErrorCode);
        var fields = body.GetProperty("errors").EnumerateObject().Select(property => property.Name);
        fields.ShouldBe(expectedFields, ignoreOrder: true);
    }

    /// <summary>
    /// Seven rolls inside October 2026 for a fresh user, plus rolls the filter must exclude: the same user just
    /// outside the month on both sides and a year earlier, and another user inside the month.
    /// No two included rolls share both sum and timestamp, so the order never depends on how SQL Server sorts Guids.
    /// </summary>
    private async Task<(Guid UserId, OctoberRolls Rolls)> SeedOctober2026Async()
    {
        var userId = Guid.CreateVersion7();
        var rolls = new OctoberRolls(
            R1: TestRolls.At(userId, new DateTime(2026, 10, 1, 0, 0, 0), sum: 7),
            R2: TestRolls.At(userId, new DateTime(2026, 10, 5, 9, 0, 0), sum: 12),
            R3: TestRolls.At(userId, new DateTime(2026, 10, 3, 9, 0, 0), sum: 7),
            R4: TestRolls.At(userId, new DateTime(2026, 10, 31, 23, 59, 59), sum: 2),
            R5: TestRolls.At(userId, new DateTime(2026, 10, 2, 9, 0, 0), sum: 12),
            R6: TestRolls.At(userId, new DateTime(2026, 10, 20, 9, 0, 0), sum: 7),
            R7: TestRolls.At(userId, new DateTime(2026, 10, 10, 9, 0, 0), sum: 9));

        await factory.SeedAsync(
            rolls.R1, rolls.R2, rolls.R3, rolls.R4, rolls.R5, rolls.R6, rolls.R7,
            TestRolls.At(userId, new DateTime(2026, 11, 1, 0, 0, 0), sum: 12),
            TestRolls.At(userId, new DateTime(2026, 9, 30, 23, 59, 59), sum: 12),
            TestRolls.At(userId, new DateTime(2025, 10, 15, 9, 0, 0), sum: 12),
            TestRolls.At(Guid.CreateVersion7(), new DateTime(2026, 10, 15, 9, 0, 0), sum: 7));

        return (userId, rolls);
    }

    private static async Task<PagedResponse<DiceRollDto>> ListAsync(HttpClient client, string queryString) =>
        (await client.GetFromJsonAsync<PagedResponse<DiceRollDto>>(
            $"{RollsPath}?{queryString}",
            TestContext.Current.CancellationToken)).ShouldNotBeNull();

    private sealed record OctoberRolls(
        DiceRoll R1, DiceRoll R2, DiceRoll R3, DiceRoll R4, DiceRoll R5, DiceRoll R6, DiceRoll R7);
}
