using DiceRoller.BuildingBlocks.Contracts;
using DiceRoller.Operative.Application.Abstractions;
using DiceRoller.Operative.Application.DiceRolls;
using DiceRoller.Operative.Domain.DiceRolls;
using DiceRoller.Operative.UnitTests.Fakes;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace DiceRoller.Operative.UnitTests.Application;

public sealed class RollHandlersTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IDiceRollRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    public RollHandlersTests()
    {
        _currentUser.SetupGet(user => user.UserId).Returns(TestRolls.UserId);
    }

    [Fact]
    public async Task RollDice_ValidUser_PersistsRollForCurrentUserAndReturnsDto()
    {
        DiceRoll? saved = null;
        var stagedBeforeCommit = false;
        _repository
            .Setup(repository => repository.Add(It.IsAny<DiceRoll>()))
            .Callback<DiceRoll>(roll => saved = roll);
        _unitOfWork
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => stagedBeforeCommit = saved is not null)
            .Returns(Task.CompletedTask);
        var handler = CreateRollDiceHandler();

        var result = await handler.Handle(new RollDiceCommand(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        stagedBeforeCommit.ShouldBeTrue();
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        saved.ShouldNotBeNull();
        saved.UserId.ShouldBe(TestRolls.UserId);
        result.Value.ShouldBe(new DiceRollDto(saved.Id, 2, 5, 7, Now.UtcDateTime));
    }

    [Fact]
    public async Task RollDice_CommitFails_ExceptionPropagates()
    {
        _unitOfWork
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("commit failed"));
        var handler = CreateRollDiceHandler();

        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(new RollDiceCommand(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetRoll_RepositoryReturnsNull_ReturnsNotFound()
    {
        var id = Guid.CreateVersion7();
        var handler = new GetRollHandler(_repository.Object, _currentUser.Object);

        var result = await handler.Handle(new GetRollQuery(id), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(DiceRollErrors.NotFound.Code);
        _repository.Verify(repository => repository.GetByIdAsync(id, TestRolls.UserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetRoll_RepositoryReturnsRoll_ReturnsIt()
    {
        var dto = new DiceRollDto(Guid.CreateVersion7(), 3, 4, 7, Now.UtcDateTime);
        _repository
            .Setup(repository => repository.GetByIdAsync(dto.Id, TestRolls.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);
        var handler = new GetRollHandler(_repository.Object, _currentUser.Object);

        var result = await handler.Handle(new GetRollQuery(dto.Id), TestContext.Current.CancellationToken);

        result.Value.ShouldBe(dto);
    }

    [Fact]
    public async Task GetRolls_AnyQuery_ScopesToCurrentUserAndPassesQueryThrough()
    {
        var query = new RollsQuery { Year = 2026, Page = 2, PageSize = 5 };
        var page = new PagedResponse<DiceRollDto>([], 2, 5, 0);
        _repository
            .Setup(repository => repository.GetPageAsync(TestRolls.UserId, query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);
        var handler = new GetRollsHandler(_repository.Object, _currentUser.Object);

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Value.ShouldBe(page);
        _repository.Verify(
            repository => repository.GetPageAsync(It.Is<Guid>(id => id != TestRolls.UserId), It.IsAny<RollsQuery>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private RollDiceHandler CreateRollDiceHandler() =>
        new(_repository.Object, _unitOfWork.Object, _currentUser.Object, new FakeDiceRoller(2, 5), new FakeTimeProvider(Now));
}
