using DiceRoller.Operative.Application.DiceRolls;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DiceRoller.Operative.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RollsQueryValidator>();

        services.AddScoped<RollDiceHandler>();
        services.AddScoped<GetRollHandler>();
        services.AddScoped<GetRollsHandler>();

        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
