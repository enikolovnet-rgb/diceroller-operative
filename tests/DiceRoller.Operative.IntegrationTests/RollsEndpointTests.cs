using System.Net;
using System.Net.Http.Json;
using DiceRoller.BuildingBlocks.Contracts;
using DiceRoller.Operative.Application.DiceRolls;
using DiceRoller.Operative.Domain.DiceRolls;
using DiceRoller.Operative.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace DiceRoller.Operative.IntegrationTests;

public sealed class RollsEndpointTests(OperativeApiFactory factory)
{
    private const string RollsPath = "/api/v1/rolls";

    [Fact]
    public async Task Roll_ValidToken_Returns201WithLocationAndValidDice()
    {
        var client = factory.CreateClient(Guid.CreateVersion7());

        var response = await client.PostAsync(RollsPath, content: null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var roll = await response.Content.ReadFromJsonAsync<DiceRollDto>(TestContext.Current.CancellationToken);
        roll.ShouldNotBeNull();
        roll.Die1.ShouldBeInRange(1, 6);
        roll.Die2.ShouldBeInRange(1, 6);
        roll.Sum.ShouldBe(roll.Die1 + roll.Die2);

        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location.AbsolutePath.ShouldBe($"{RollsPath}/{roll.Id}");
        var fetched = await client.GetFromJsonAsync<DiceRollDto>(response.Headers.Location, TestContext.Current.CancellationToken);
        fetched.ShouldBe(roll);
    }

    [Fact]
    public async Task List_TwoUsers_EachSeesOnlyOwnRolls()
    {
        var alice = factory.CreateClient(Guid.CreateVersion7());
        var bob = factory.CreateClient(Guid.CreateVersion7());
        var aliceRolls = new[] { await RollAsync(alice), await RollAsync(alice) };
        var bobRoll = await RollAsync(bob);

        var aliceList = await ListAsync(alice);
        var bobList = await ListAsync(bob);

        aliceList.TotalCount.ShouldBe(2);
        aliceList.Items.Select(roll => roll.Id).ShouldBe(aliceRolls.Select(roll => roll.Id), ignoreOrder: true);
        bobList.TotalCount.ShouldBe(1);
        bobList.Items.ShouldHaveSingleItem().Id.ShouldBe(bobRoll.Id);
    }

    [Fact]
    public async Task GetById_AnotherUsersRoll_Returns404()
    {
        var owner = factory.CreateClient(Guid.CreateVersion7());
        var stranger = factory.CreateClient(Guid.CreateVersion7());
        var roll = await RollAsync(owner);

        var response = await stranger.GetAsync($"{RollsPath}/{roll.Id}", TestContext.Current.CancellationToken);

        await response.ShouldBeProblemAsync(StatusCodes.Status404NotFound, DiceRollErrors.NotFound.Code);
        (await owner.GetAsync($"{RollsPath}/{roll.Id}", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404()
    {
        var client = factory.CreateClient(Guid.CreateVersion7());

        var response = await client.GetAsync($"{RollsPath}/{Guid.CreateVersion7()}", TestContext.Current.CancellationToken);

        await response.ShouldBeProblemAsync(StatusCodes.Status404NotFound, DiceRollErrors.NotFound.Code);
    }

    private static async Task<DiceRollDto> RollAsync(HttpClient client)
    {
        var response = await client.PostAsync(RollsPath, content: null, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<DiceRollDto>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
    }

    private static async Task<PagedResponse<DiceRollDto>> ListAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<PagedResponse<DiceRollDto>>(RollsPath, TestContext.Current.CancellationToken)).ShouldNotBeNull();
}
