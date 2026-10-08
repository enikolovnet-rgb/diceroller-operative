using System.Net;
using DiceRoller.BuildingBlocks.Web;
using DiceRoller.Operative.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace DiceRoller.Operative.IntegrationTests;

public sealed class AuthenticationTests(OperativeApiFactory factory)
{
    private const string RollsPath = "/api/v1/rolls";

    public static TheoryData<string, string, string> RejectedTokens()
    {
        var data = new TheoryData<string, string, string>();
        foreach (var method in new[] { "GET", "POST" })
        {
            data.Add(method, "none", JwtAuthenticationExtensions.MissingTokenCode);
            data.Add(method, "expired", JwtAuthenticationExtensions.ExpiredTokenCode);
            data.Add(method, "wrong-signature", JwtAuthenticationExtensions.InvalidTokenCode);
            data.Add(method, "wrong-issuer", JwtAuthenticationExtensions.InvalidTokenCode);
            data.Add(method, "wrong-audience", JwtAuthenticationExtensions.InvalidTokenCode);
            data.Add(method, "wrong-algorithm", JwtAuthenticationExtensions.InvalidTokenCode);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(RejectedTokens))]
    public async Task Rolls_RejectedToken_Returns401WithStandardBody(string method, string token, string expectedCode)
    {
        var userId = Guid.CreateVersion7();
        var client = factory.CreateClient(token switch
        {
            "none" => null,
            "expired" => TestTokens.Expired(userId),
            "wrong-signature" => TestTokens.WrongSignature(userId),
            "wrong-issuer" => TestTokens.WrongIssuer(userId),
            "wrong-audience" => TestTokens.WrongAudience(userId),
            "wrong-algorithm" => TestTokens.WrongAlgorithm(userId),
            _ => throw new ArgumentOutOfRangeException(nameof(token), token, null),
        });

        var response = await client.SendAsync(
            new HttpRequestMessage(new HttpMethod(method), RollsPath),
            TestContext.Current.CancellationToken);

        await response.ShouldBeProblemAsync(StatusCodes.Status401Unauthorized, expectedCode);
        response.Headers.WwwAuthenticate.ShouldContain(header => header.Scheme == "Bearer");
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task List_ValidTokenWithoutUserIdSubject_Returns403WithStandardBody(string subject)
    {
        var client = factory.CreateClient(TestTokens.WithSubject(subject));

        var response = await client.GetAsync(RollsPath, TestContext.Current.CancellationToken);

        await response.ShouldBeProblemAsync(StatusCodes.Status403Forbidden, JwtAuthenticationExtensions.ForbiddenCode);
    }

    [Fact]
    public async Task List_ValidToken_Returns200()
    {
        var client = factory.CreateClient(Guid.CreateVersion7());

        var response = await client.GetAsync(RollsPath, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
