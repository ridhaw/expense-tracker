using System.ComponentModel.DataAnnotations;
using ExpenseTracker.Api.Domain;

namespace ExpenseTracker.Api.Endpoints;

public sealed record RegisterRequest(
    [property: Required, EmailAddress, MaxLength(254)] string Email,
    [property: Required, MinLength(8), MaxLength(128)] string Password);

public sealed record LoginRequest([property: Required] string Email, [property: Required] string Password);

public sealed record AuthResponse(string Token, DateTimeOffset ExpiresAt, string Email);

public sealed record ExpenseRequest(
    [property: Required, MinLength(1), MaxLength(120)] string Label,
    [property: Range(0.001, 1_000_000_000)] decimal Amount,
    Category Category,
    DateOnly Date);

public sealed record ExpenseResponse(Guid Id, string Label, decimal Amount, Category Category, DateOnly Date)
{
    public static ExpenseResponse From(Expense e) => new(e.Id, e.Label, e.Amount, e.Category, e.Date);
}

public sealed record CategoryTotal(Category Category, decimal Total, int Count);

public sealed record MonthlySummary(string Month, decimal Total, int Count, IReadOnlyList<CategoryTotal> ByCategory);
