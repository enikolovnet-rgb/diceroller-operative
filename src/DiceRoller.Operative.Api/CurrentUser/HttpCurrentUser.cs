using DiceRoller.BuildingBlocks.Contracts;
using DiceRoller.Operative.Application.Abstractions;

namespace DiceRoller.Operative.Api.CurrentUser;

/// <summary>Reads the caller from the validated token's <c>sub</c> claim; never from the route, query or body.</summary>
internal sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid UserId =>
        TryGetUserId(httpContextAccessor.HttpContext?.User.FindFirst(JwtClaimNames.Subject)?.Value, out var userId)
            ? userId
            : throw new InvalidOperationException("The authenticated user has no valid 'sub' claim.");

    /// <summary>The rule the authorization policy enforces, so a request that reaches a handler always has a user id.</summary>
    public static bool TryGetUserId(string? subject, out Guid userId) =>
        Guid.TryParse(subject, out userId) && userId != Guid.Empty;
}
