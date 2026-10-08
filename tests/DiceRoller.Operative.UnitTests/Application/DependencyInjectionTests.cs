using DiceRoller.BuildingBlocks.Contracts;
using DiceRoller.Operative.Application;
using DiceRoller.Operative.Application.Abstractions;
using DiceRoller.Operative.Application.DiceRolls;
using DiceRoller.Operative.Domain.DiceRolls;
using DiceRoller.Operative.UnitTests.Fakes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace DiceRoller.Operative.UnitTests.Application;

public sealed class DependencyInjectionTests
{
    private readonly Mock<IDiceRollRepository> _repository = new();

    [Fact]
    public async Task AddApplication_SendEachRequest_ReachesItsHandler()
    {
        _repository
            .Setup(repository => repository.GetPageAsync(TestRolls.UserId, It.IsAny<RollsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResponse<DiceRollDto>([], 1, 10, 0));
        await using var provider = BuildProvider();
        var sender = provider.GetRequiredService<ISender>();
        var cancellationToken = TestContext.Current.CancellationToken;

        var rolled = await sender.Send(new RollDiceCommand(), cancellationToken);
        var single = await sender.Send(new GetRollQuery(Guid.CreateVersion7()), cancellationToken);
        var page = await sender.Send(new RollsQuery(), cancellationToken);

        rolled.IsSuccess.ShouldBeTrue();
        single.Error.Code.ShouldBe(DiceRollErrors.NotFound.Code);
        page.Value.TotalCount.ShouldBe(0);
    }

    private ServiceProvider BuildProvider()
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(user => user.UserId).Returns(TestRolls.UserId);

        var services = new ServiceCollection();
        services.AddApplication();
        services.AddSingleton(_repository.Object);
        services.AddSingleton(currentUser.Object);
        services.AddSingleton<IDiceRoller>(new FakeDiceRoller(1, 6));

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }
}
