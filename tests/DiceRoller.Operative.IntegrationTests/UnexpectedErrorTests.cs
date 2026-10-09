using DiceRoller.BuildingBlocks.Contracts;
using DiceRoller.BuildingBlocks.Domain;
using DiceRoller.BuildingBlocks.Web;
using DiceRoller.Operative.Application.Abstractions;
using DiceRoller.Operative.Application.DiceRolls;
using DiceRoller.Operative.Domain.DiceRolls;
using DiceRoller.Operative.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DiceRoller.Operative.IntegrationTests;

public sealed class UnexpectedErrorTests(OperativeApiFactory factory)
{
    private const string FailureMessage = "Simulated failure: Server=secret-host;Password=do-not-leak";

    [Fact]
    public async Task List_RepositoryThrows_Returns500WithStandardBodyAndNoExceptionDetails()
    {
        await using var failing = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddScoped<IDiceRollRepository, ThrowingRepository>()));
        var client = failing.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", TestTokens.Valid(Guid.CreateVersion7()));

        var response = await client.GetAsync("/api/v1/rolls", TestContext.Current.CancellationToken);

        var body = await response.ShouldBeProblemAsync(StatusCodes.Status500InternalServerError, Error.Unexpected().Code);
        body.TryGetProperty(GlobalExceptionHandler.ExceptionKey, out _).ShouldBeFalse();
        body.GetRawText().ShouldNotContain("do-not-leak");
    }

    private sealed class ThrowingRepository : IDiceRollRepository
    {
        public void Add(DiceRoll roll) =>
            throw new InvalidOperationException(FailureMessage);

        public Task<DiceRollDto?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken) =>
            throw new InvalidOperationException(FailureMessage);

        public Task<PagedResponse<DiceRollDto>> GetPageAsync(Guid userId, RollsQuery query, CancellationToken cancellationToken) =>
            throw new InvalidOperationException(FailureMessage);
    }
}
