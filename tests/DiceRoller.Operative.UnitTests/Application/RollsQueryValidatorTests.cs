using DiceRoller.BuildingBlocks.Contracts;
using DiceRoller.Operative.Application.DiceRolls;
using FluentValidation.Results;
using SortDirection = DiceRoller.Operative.Application.DiceRolls.SortDirection;

namespace DiceRoller.Operative.UnitTests.Application;

public sealed class RollsQueryValidatorTests
{
    private readonly RollsQueryValidator _validator = new();

    public static TheoryData<RollsQuery> ValidQueries => new()
    {
        new RollsQuery(),
        new RollsQuery { Year = 2026 },
        new RollsQuery { Year = 2026, Month = 3 },
        new RollsQuery { Year = 2026, Month = 3, Day = 31 },
        new RollsQuery { Year = 2024, Month = 2, Day = 29 },
        new RollsQuery { Year = 1, Month = 1, Day = 1 },
        new RollsQuery { Year = 9999, Month = 12, Day = 31 },
        new RollsQuery { SortBySum = SortDirection.Asc, SortByDate = SortDirection.Desc },
        new RollsQuery { Page = 1, PageSize = 1 },
        new RollsQuery { Page = 50, PageSize = PagedQuery.MaxPageSize },
    };

    [Theory]
    [MemberData(nameof(ValidQueries))]
    public void Validate_ValidQuery_HasNoErrors(RollsQuery query)
    {
        _validator.Validate(query).Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_MonthWithoutYear_ReportsMonthRequiresYear()
    {
        var errors = Validate(new RollsQuery { Month = 3 });

        ShouldContain(errors, nameof(RollsQuery.Month), RollsQueryValidationCodes.MonthRequiresYear);
    }

    [Fact]
    public void Validate_DayWithoutMonth_ReportsDayRequiresYearAndMonth()
    {
        var errors = Validate(new RollsQuery { Year = 2026, Day = 1 });

        ShouldContain(errors, nameof(RollsQuery.Day), RollsQueryValidationCodes.DayRequiresYearAndMonth);
    }

    [Fact]
    public void Validate_DayWithoutYear_ReportsBothRequirements()
    {
        var errors = Validate(new RollsQuery { Month = 3, Day = 1 });

        ShouldContain(errors, nameof(RollsQuery.Month), RollsQueryValidationCodes.MonthRequiresYear);
        ShouldContain(errors, nameof(RollsQuery.Day), RollsQueryValidationCodes.DayRequiresYearAndMonth);
    }

    [Theory]
    [InlineData(2026, 2, 30)]
    [InlineData(2025, 2, 29)]
    [InlineData(2100, 2, 29)]
    [InlineData(2026, 4, 31)]
    public void Validate_DateThatDoesNotExist_ReportsInvalidDate(int year, int month, int day)
    {
        var errors = Validate(new RollsQuery { Year = year, Month = month, Day = day });

        ShouldContain(errors, nameof(RollsQuery.Day), RollsQueryValidationCodes.InvalidDate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10000)]
    public void Validate_YearOutOfRange_ReportsInvalidYear(int year)
    {
        var errors = Validate(new RollsQuery { Year = year });

        ShouldContain(errors, nameof(RollsQuery.Year), RollsQueryValidationCodes.InvalidYear);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Validate_MonthOutOfRange_ReportsInvalidMonth(int month)
    {
        var errors = Validate(new RollsQuery { Year = 2026, Month = month });

        ShouldContain(errors, nameof(RollsQuery.Month), RollsQueryValidationCodes.InvalidMonth);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void Validate_DayOutOfRange_ReportsInvalidDayOnly(int day)
    {
        var errors = Validate(new RollsQuery { Year = 2026, Month = 1, Day = day });

        ShouldContain(errors, nameof(RollsQuery.Day), RollsQueryValidationCodes.InvalidDay);
        errors.ShouldNotContain(error => error.ErrorCode == RollsQueryValidationCodes.InvalidDate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_PageBelowOne_ReportsSharedInvalidPage(int page)
    {
        var errors = Validate(new RollsQuery { Page = page });

        ShouldContain(errors, nameof(RollsQuery.Page), PagedQueryValidator.InvalidPageCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(PagedQuery.MaxPageSize + 1)]
    public void Validate_PageSizeOutOfRange_ReportsSharedInvalidPageSize(int pageSize)
    {
        var errors = Validate(new RollsQuery { PageSize = pageSize });

        ShouldContain(errors, nameof(RollsQuery.PageSize), PagedQueryValidator.InvalidPageSizeCode);
    }

    [Fact]
    public void Validate_UndefinedSortDirections_ReportsInvalidSorts()
    {
        var errors = Validate(new RollsQuery { SortBySum = (SortDirection)5, SortByDate = (SortDirection)(-1) });

        ShouldContain(errors, nameof(RollsQuery.SortBySum), RollsQueryValidationCodes.InvalidSortBySum);
        ShouldContain(errors, nameof(RollsQuery.SortByDate), RollsQueryValidationCodes.InvalidSortByDate);
    }

    [Fact]
    public void Validate_AnyFailure_HasCodeAndMessage()
    {
        var errors = Validate(new RollsQuery
        {
            Year = 0,
            Month = 13,
            Day = 32,
            SortBySum = (SortDirection)9,
            SortByDate = (SortDirection)9,
            Page = 0,
            PageSize = 0,
        });

        errors.ShouldNotBeEmpty();
        errors.ShouldAllBe(error => !string.IsNullOrWhiteSpace(error.ErrorCode) && !string.IsNullOrWhiteSpace(error.ErrorMessage));
    }

    private List<ValidationFailure> Validate(RollsQuery query) => _validator.Validate(query).Errors;

    private static void ShouldContain(List<ValidationFailure> errors, string propertyName, string errorCode) =>
        errors.ShouldContain(
            error => error.PropertyName == propertyName && error.ErrorCode == errorCode,
            $"Expected {errorCode} on {propertyName}; got: {string.Join(", ", errors.Select(e => $"{e.PropertyName}:{e.ErrorCode}"))}");
}
