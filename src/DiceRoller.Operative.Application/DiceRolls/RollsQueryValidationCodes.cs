namespace DiceRoller.Operative.Application.DiceRolls;

public static class RollsQueryValidationCodes
{
    public const string InvalidYear = "RollsQuery.InvalidYear";
    public const string InvalidMonth = "RollsQuery.InvalidMonth";
    public const string MonthRequiresYear = "RollsQuery.MonthRequiresYear";
    public const string InvalidDay = "RollsQuery.InvalidDay";
    public const string DayRequiresYearAndMonth = "RollsQuery.DayRequiresYearAndMonth";
    public const string InvalidDate = "RollsQuery.InvalidDate";
    public const string InvalidSortBySum = "RollsQuery.InvalidSortBySum";
    public const string InvalidSortByDate = "RollsQuery.InvalidSortByDate";
}
