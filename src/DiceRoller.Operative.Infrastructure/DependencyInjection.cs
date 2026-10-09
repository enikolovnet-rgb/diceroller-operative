using DiceRoller.Operative.Application.Abstractions;
using DiceRoller.Operative.Domain.DiceRolls;
using DiceRoller.Operative.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DiceRoller.Operative.Infrastructure;

public static class DependencyInjection
{
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Reads <c>ConnectionStrings:Operative</c>.</param>
    /// <param name="readinessTags">Tags for the database health check, so it joins the host's readiness probe.</param>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IEnumerable<string> readinessTags)
    {
        services.AddOptions<DatabaseOptions>()
            .Configure(options =>
                options.ConnectionString = configuration.GetConnectionString(DatabaseOptions.ConnectionStringName) ?? string.Empty)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                $"ConnectionStrings:{DatabaseOptions.ConnectionStringName} is required.")
            .ValidateOnStart();

        services.AddDbContext<OperativeDbContext>((provider, options) =>
            options.UseSqlServer(
                provider.GetRequiredService<IOptions<DatabaseOptions>>().Value.ConnectionString,
                sql => sql.EnableRetryOnFailure()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<OperativeDbContext>());
        services.AddScoped<IDiceRollRepository, DiceRollRepository>();
        services.AddSingleton<IDiceRoller, CryptoDiceRoller>();

        services.AddHealthChecks()
            .AddDbContextCheck<OperativeDbContext>("database", tags: readinessTags);

        return services;
    }

    /// <summary>Applies pending migrations. Call only in Development and in the container.</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OperativeDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }
}
