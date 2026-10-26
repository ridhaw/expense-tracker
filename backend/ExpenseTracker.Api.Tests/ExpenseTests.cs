using System.Net;
using System.Net.Http.Json;
using ExpenseTracker.Api.Domain;
using ExpenseTracker.Api.Endpoints;

namespace ExpenseTracker.Api.Tests;

[Collection(nameof(ApiCollection))]
public sealed class ExpenseTests(ApiFactory api)
{
    private static ExpenseRequest Expense(string label, decimal amount, Category category, string date) =>
        new(label, amount, category, DateOnly.Parse(date));

    private static async Task<ExpenseResponse> CreateAsync(HttpClient client, ExpenseRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/expenses", request, Json.Options);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ExpenseResponse>(Json.Options))!;
    }

    [Fact]
    public async Task Create_read_update_delete()
    {
        var client = await api.CreateUserClientAsync();
        var created = await CreateAsync(client, Expense("Courses", 84.250m, Category.Alimentation, "2026-10-03"));

        var fetched = await client.GetFromJsonAsync<ExpenseResponse>($"/api/expenses/{created.Id}", Json.Options);
        Assert.Equal(created, fetched);

        var update = await client.PutAsJsonAsync($"/api/expenses/{created.Id}",
            Expense("Courses du mois", 90m, Category.Alimentation, "2026-10-04"), Json.Options);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<ExpenseResponse>(Json.Options);
        Assert.Equal(90m, updated!.Amount);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/expenses/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/expenses/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task Users_cannot_see_or_change_each_others_expenses()
    {
        var alice = await api.CreateUserClientAsync();
        var bob = await api.CreateUserClientAsync();
        var aliceExpense = await CreateAsync(alice, Expense("Loyer", 900m, Category.Logement, "2026-10-01"));

        Assert.Empty((await bob.GetFromJsonAsync<List<ExpenseResponse>>("/api/expenses", Json.Options))!);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync($"/api/expenses/{aliceExpense.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.DeleteAsync($"/api/expenses/{aliceExpense.Id}")).StatusCode);
        Assert.Single((await alice.GetFromJsonAsync<List<ExpenseResponse>>("/api/expenses", Json.Options))!);
    }

    [Fact]
    public async Task Monthly_summary_groups_by_category_and_ignores_other_months()
    {
        var client = await api.CreateUserClientAsync();
        await CreateAsync(client, Expense("Courses", 50m, Category.Alimentation, "2026-09-02"));
        await CreateAsync(client, Expense("Marché", 30.5m, Category.Alimentation, "2026-09-20"));
        await CreateAsync(client, Expense("Bus", 12m, Category.Transport, "2026-09-30"));
        await CreateAsync(client, Expense("Octobre", 999m, Category.Loisirs, "2026-10-01"));

        var summary = await client.GetFromJsonAsync<MonthlySummary>("/api/expenses/summary?month=2026-09", Json.Options);

        Assert.Equal(92.5m, summary!.Total);
        Assert.Equal(3, summary.Count);
        Assert.Equal(new CategoryTotal(Category.Alimentation, 80.5m, 2), summary.ByCategory[0]);
        Assert.Equal(new CategoryTotal(Category.Transport, 12m, 1), summary.ByCategory[1]);
    }

    [Fact]
    public async Task List_can_be_filtered_by_month()
    {
        var client = await api.CreateUserClientAsync();
        await CreateAsync(client, Expense("Septembre", 10m, Category.Autre, "2026-09-15"));
        await CreateAsync(client, Expense("Octobre", 20m, Category.Autre, "2026-10-15"));

        var october = await client.GetFromJsonAsync<List<ExpenseResponse>>("/api/expenses?month=2026-10", Json.Options);
        Assert.Equal("Octobre", Assert.Single(october!).Label);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/expenses?month=octobre")).StatusCode);
    }

    [Theory]
    [InlineData("", 10)]
    [InlineData("Montant nul", 0)]
    [InlineData("Montant négatif", -5)]
    public async Task Invalid_expenses_are_rejected(string label, decimal amount)
    {
        var client = await api.CreateUserClientAsync();
        var response = await client.PostAsJsonAsync("/api/expenses",
            Expense(label, amount, Category.Autre, "2026-10-01"), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
