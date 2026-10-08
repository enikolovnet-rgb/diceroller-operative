using System.Text.Json;
using System.Text.Json.Serialization;
using DiceRoller.BuildingBlocks.Contracts;
using DiceRoller.BuildingBlocks.Web;
using DiceRoller.Operative.Api.CurrentUser;
using DiceRoller.Operative.Application;
using DiceRoller.Operative.Application.Abstractions;
using DiceRoller.Operative.Infrastructure;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, [ServiceDefaultsExtensions.ReadyTag]);
builder.Services.AddJwtAuthentication(builder.Configuration);

// Stricter than "authenticated": the token's sub must be a user id, so ICurrentUser always has one.
var authenticatedUser = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .RequireAssertion(context =>
        HttpCurrentUser.TryGetUserId(context.User.FindFirst(JwtClaimNames.Subject)?.Value, out _))
    .Build();
builder.Services.AddAuthorizationBuilder()
    .SetDefaultPolicy(authenticatedUser)
    .SetFallbackPolicy(authenticatedUser);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

// sortBySum / sortByDate are "asc" | "desc" on the wire and in the OpenAPI document.
var enumConverter = new JsonStringEnumConverter(JsonNamingPolicy.CamelCase);
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(enumConverter));
builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options => options.JsonSerializerOptions.Converters.Add(enumConverter));

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("DOTNET_RUNNING_IN_CONTAINER"))
{
    await app.Services.MigrateDatabaseAsync();
}

app.UseServiceDefaults();

await app.RunAsync();

/// <summary>Exposed for <c>WebApplicationFactory</c> in the integration tests.</summary>
public partial class Program;
