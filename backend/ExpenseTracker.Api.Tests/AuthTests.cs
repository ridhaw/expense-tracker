using System.Net;
using System.Net.Http.Json;
using ExpenseTracker.Api.Endpoints;

namespace ExpenseTracker.Api.Tests;

[Collection(nameof(ApiCollection))]
public sealed class AuthTests(ApiFactory api)
{
    [Fact]
    public async Task Register_then_login_returns_a_token()
    {
        var client = api.CreateClient();
        var email = $"login-{Guid.NewGuid():N}@test.dev";

        var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "motdepasse-solide"));
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email.ToUpperInvariant(), "motdepasse-solide"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>(Json.Options);
        Assert.False(string.IsNullOrWhiteSpace(auth!.Token));
        Assert.Equal(email, auth.Email);
    }

    [Fact]
    public async Task Register_with_existing_email_is_a_conflict()
    {
        var client = api.CreateClient();
        var email = $"dup-{Guid.NewGuid():N}@test.dev";
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "motdepasse-solide"));

        var again = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "autre-motdepasse"));
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Theory]
    [InlineData("pas-un-email", "motdepasse-solide")]
    [InlineData("ok@test.dev", "court")]
    public async Task Register_validates_input(string email, string password)
    {
        var response = await api.CreateClient().PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Wrong_password_and_unknown_email_get_the_same_answer()
    {
        var client = api.CreateClient();
        var email = $"pw-{Guid.NewGuid():N}@test.dev";
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "motdepasse-solide"));

        var wrongPassword = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "mauvais-motdepasse"));
        var unknownEmail = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("inconnu@test.dev", "motdepasse-solide"));

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        var first = await wrongPassword.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        var second = await unknownEmail.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        Assert.Equal(first!.Detail, second!.Detail);
    }

    [Fact]
    public async Task Expenses_require_authentication()
    {
        var response = await api.CreateClient().GetAsync("/api/expenses");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
