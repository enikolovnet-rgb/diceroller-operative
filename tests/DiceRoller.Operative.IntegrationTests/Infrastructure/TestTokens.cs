using System.Security.Claims;
using System.Text;
using DiceRoller.BuildingBlocks.Contracts;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DiceRoller.Operative.IntegrationTests.Infrastructure;

/// <summary>Mints tokens with the test key, issuer and audience the factory configures; no UserAccess involved.</summary>
public static class TestTokens
{
    public const string Issuer = "diceroller-tests";
    public const string Audience = "diceroller-operative-tests";
    public const string SigningKey = "integration-tests-signing-key-long-enough-for-hs256-and-hs512-0123456789";

    private const string OtherSigningKey = "a-different-key-that-the-service-does-not-trust-at-all-0123456789abcdef";

    public static string Valid(Guid userId) => Create(userId.ToString());

    /// <summary>Passes every token check, but <c>sub</c> is whatever is given, so it may not be a user id.</summary>
    public static string WithSubject(string subject) => Create(subject);

    public static string Expired(Guid userId)
    {
        var now = TimeProvider.System.GetUtcNow().UtcDateTime;

        // Well past the 30-second clock skew.
        return Create(userId.ToString(), issuedAt: now.AddMinutes(-10), expires: now.AddMinutes(-5));
    }

    public static string WrongSignature(Guid userId) => Create(userId.ToString(), signingKey: OtherSigningKey);

    public static string WrongIssuer(Guid userId) => Create(userId.ToString(), issuer: "someone-else");

    public static string WrongAudience(Guid userId) => Create(userId.ToString(), audience: "another-service");

    /// <summary>Signed with the right key, but HS512 instead of the only accepted algorithm, HS256.</summary>
    public static string WrongAlgorithm(Guid userId) => Create(userId.ToString(), algorithm: SecurityAlgorithms.HmacSha512);

    private static string Create(
        string subject,
        string signingKey = SigningKey,
        string issuer = Issuer,
        string audience = Audience,
        string algorithm = SecurityAlgorithms.HmacSha256,
        DateTime? issuedAt = null,
        DateTime? expires = null)
    {
        var now = TimeProvider.System.GetUtcNow().UtcDateTime;
        var issued = issuedAt ?? now;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim(JwtClaimNames.Subject, subject)]),
            Issuer = issuer,
            Audience = audience,
            IssuedAt = issued,
            NotBefore = issued,
            Expires = expires ?? now.AddMinutes(5),
            SigningCredentials = new SigningCredentials(key, algorithm),
        };

        return new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = false }.CreateToken(descriptor);
    }
}
