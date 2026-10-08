using DiceRoller.BuildingBlocks.Domain;
using DiceRoller.Operative.Domain.DiceRolls;

namespace DiceRoller.Operative.UnitTests.Domain;

public sealed class DieValueTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void Create_ValueInRange_Succeeds(int value)
    {
        DieValue.Create(value).Value.ShouldBe(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void Create_ValueOutOfRange_ThrowsInvalidDieValue(int value)
    {
        var exception = Should.Throw<DomainException>(() => DieValue.Create(value));

        exception.Error.Code.ShouldBe(DiceRollErrors.InvalidDieValue.Code);
    }

    [Fact]
    public void Create_SameValue_ProducesEqualValues()
    {
        DieValue.Create(4).ShouldBe(DieValue.Create(4));
    }
}
