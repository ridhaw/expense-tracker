namespace ExpenseTracker.Api.Domain;

public enum Category
{
    Alimentation,
    Transport,
    Logement,
    Sante,
    Loisirs,
    Factures,
    Autre,
}

public sealed class User
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Email { get; init; }
    public string PasswordHash { get; set; } = "";
    public DateTimeOffset CreatedAt { get; init; }
    public List<Expense> Expenses { get; } = [];
}

public sealed class Expense
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public required string Label { get; set; }
    public decimal Amount { get; set; }
    public Category Category { get; set; }
    public DateOnly Date { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
}
