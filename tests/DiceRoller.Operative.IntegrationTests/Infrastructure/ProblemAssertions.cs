using System.Text.Json;

namespace DiceRoller.Operative.IntegrationTests.Infrastructure;

public static class ProblemAssertions
{
    /// <summary>
    /// Asserts the standard RFC 9457 error body every failure must have, and returns it for further checks.
    /// </summary>
    public static async Task<JsonElement> ShouldBeProblemAsync(this HttpResponseMessage response, int status, string errorCode)
    {
        ((int)response.StatusCode).ShouldBe(status);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var body = document.RootElement.Clone();

        body.GetProperty("status").GetInt32().ShouldBe(status);
        body.GetProperty("title").GetString().ShouldNotBeNullOrWhiteSpace();
        body.GetProperty("detail").GetString().ShouldNotBeNullOrWhiteSpace();
        body.GetProperty("errorCode").GetString().ShouldBe(errorCode);
        body.TryGetProperty("errors", out _).ShouldBeTrue();
        body.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();

        return body;
    }
}
