using DiceRoller.BuildingBlocks.Contracts;
using FluentValidation;

namespace DiceRoller.Operative.Application.DiceRolls;

public sealed class RollsQueryValidator : AbstractValidator<RollsQuery>
{
    public const int MinYear = 1;
    public const int MaxYear = 9999;

    private static readonly PagedQueryValidator PagingValidator = new();

    public RollsQueryValidator()
    {
        RuleFor(query => query.Year)
            .InclusiveBetween(MinYear, MaxYear)
            .WithErrorCode(RollsQueryValidationCodes.InvalidYear)
            .WithMessage($"Year must be between {MinYear} and {MaxYear}.");

        RuleFor(query => query.Month)
            .InclusiveBetween(1, 12)
            .WithErrorCode(RollsQueryValidationCodes.InvalidMonth)
            .WithMessage("Month must be between 1 and 12.");

        RuleFor(query => query.Month)
            .Null()
            .When(query => query.Year is null)
            .WithErrorCode(RollsQueryValidationCodes.MonthRequiresYear)
            .WithMessage("Month requires a year.");

        RuleFor(query => query.Day)
            .InclusiveBetween(1, 31)
            .WithErrorCode(RollsQueryValidationCodes.InvalidDay)
            .WithMessage("Day must be between 1 and 31.");

        RuleFor(query => query.Day)
            .Null()
            .When(query => query.Year is null || query.Month is null)
            .WithErrorCode(RollsQueryValidationCodes.DayRequiresYearAndMonth)
            .WithMessage("Day requires a year and a month.");

        RuleFor(query => query.Day)
            .Must((query, day) => day <= DateTime.DaysInMonth(query.Year!.Value, query.Month!.Value))
            .When(query => query.Year is >= MinYear and <= MaxYear
                && query.Month is >= 1 and <= 12
                && query.Day is >= 1 and <= 31)
            .WithErrorCode(RollsQueryValidationCodes.InvalidDate)
            .WithMessage("Year, month and day do not form a valid date.");

        RuleFor(query => query.SortBySum)
            .IsInEnum()
            .WithErrorCode(RollsQueryValidationCodes.InvalidSortBySum)
            .WithMessage("SortBySum must be 'asc' or 'desc'.");

        RuleFor(query => query.SortByDate)
            .IsInEnum()
            .WithErrorCode(RollsQueryValidationCodes.InvalidSortByDate)
            .WithMessage("SortByDate must be 'asc' or 'desc'.");

        // PagedQuery is sealed, so the shared paging rules are reused by delegation; their codes,
        // messages and property names (Page, PageSize) are forwarded unchanged.
        RuleFor(query => query).Custom((query, context) =>
        {
            var paging = new PagedQuery { Page = query.Page, PageSize = query.PageSize };
            foreach (var failure in PagingValidator.Validate(paging).Errors)
            {
                context.AddFailure(failure);
            }
        });
    }
}
