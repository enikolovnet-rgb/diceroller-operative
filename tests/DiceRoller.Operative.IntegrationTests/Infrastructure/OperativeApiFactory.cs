using System.Net.Http.Headers;
using DiceRoller.Operative.Domain.DiceRolls;
using DiceRoller.Operative.Infrastructure;
using DiceRoller.Operative.Infrastructure.Persistence;
using DiceRoller.Operative.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

[assembly: AssemblyFixture(typeof(OperativeApiFactory))]

namespace DiceRoller.Operative.IntegrationTests.Infrastructure;

/// <summary>
/// The API in memory against a real SQL Server container, shared by every test in the run.
/// Tests isolate their data by using fresh user ids, not by resetting the database.
/// </summary>
public sealed class OperativeApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _database = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public async ValueTask InitializeAsync()
    {
        await _database.StartAsync(TestContext.Current.CancellationToken);
        await Services.MigrateDatabaseAsync(TestContext.Current.CancellationToken);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }

    /// <summary>A client that sends a valid token whose <c>sub</c> is <paramref name="userId"/>.</summary>
    public HttpClient CreateClient(Guid userId) => CreateClient(TestTokens.Valid(userId));

    /// <summary>A client that sends <paramref name="token"/> as the Bearer token, or no token when it is null.</summary>
    public HttpClient CreateClient(string? token)
    {
        var client = CreateClient();
        if (token is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return client;
    }

    public async Task SeedAsync(params DiceRoll[] rolls)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OperativeDbContext>();
        db.DiceRolls.AddRange(rolls);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Not Development: no user-secrets can override the test key, and Program does not migrate on its own.
        builder.UseEnvironment("Testing");
        builder.UseSetting($"ConnectionStrings:{DatabaseOptions.ConnectionStringName}", _database.GetConnectionString());
        builder.UseSetting("Jwt:Issuer", TestTokens.Issuer);
        builder.UseSetting("Jwt:Audience", TestTokens.Audience);
        builder.UseSetting("Jwt:SigningKey", TestTokens.SigningKey);
    }
}
