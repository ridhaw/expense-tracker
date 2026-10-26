using System.Globalization;
using System.Security.Claims;
using ExpenseTracker.Api.Auth;
using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Endpoints;

public static class ExpenseEndpoints
{
    public static RouteGroupBuilder MapExpenseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/expenses").WithTags("Dépenses").RequireAuthorization();

        // Toutes les requêtes filtrent sur l'utilisateur du jeton : un utilisateur ne voit jamais les dépenses d'un autre.
        group.MapGet("/", async (ClaimsPrincipal user, AppDbContext db, string? month) =>
        {
            var query = db.Expenses.Where(e => e.UserId == user.UserId());
            if (month is not null)
            {
                if (!TryParseMonth(month, out var from, out var to))
                    return Results.Problem("Le mois doit être au format AAAA-MM.", statusCode: StatusCodes.Status400BadRequest);
                query = query.Where(e => e.Date >= from && e.Date < to);
            }
            var items = await query.OrderByDescending(e => e.Date).ThenByDescending(e => e.CreatedAt).ToListAsync();
            return Results.Ok(items.Select(ExpenseResponse.From));
        });

        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user, AppDbContext db) =>
            await db.Expenses.SingleOrDefaultAsync(e => e.Id == id && e.UserId == user.UserId()) is { } expense
                ? Results.Ok(ExpenseResponse.From(expense))
                : Results.NotFound());

        group.MapPost("/", async (ExpenseRequest request, ClaimsPrincipal user, AppDbContext db, TimeProvider clock) =>
        {
            var expense = new Expense
            {
                UserId = user.UserId(),
                Label = request.Label.Trim(),
                Amount = request.Amount,
                Category = request.Category,
                Date = request.Date,
                CreatedAt = clock.GetUtcNow(),
            };
            db.Expenses.Add(expense);
            await db.SaveChangesAsync();
            return Results.Created($"/api/expenses/{expense.Id}", ExpenseResponse.From(expense));
        }).WithValidation();

        group.MapPut("/{id:guid}", async (Guid id, ExpenseRequest request, ClaimsPrincipal user, AppDbContext db) =>
        {
            var expense = await db.Expenses.SingleOrDefaultAsync(e => e.Id == id && e.UserId == user.UserId());
            if (expense is null) return Results.NotFound();
            expense.Label = request.Label.Trim();
            expense.Amount = request.Amount;
            expense.Category = request.Category;
            expense.Date = request.Date;
            await db.SaveChangesAsync();
            return Results.Ok(ExpenseResponse.From(expense));
        }).WithValidation();

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal user, AppDbContext db) =>
        {
            var deleted = await db.Expenses.Where(e => e.Id == id && e.UserId == user.UserId()).ExecuteDeleteAsync();
            return deleted == 0 ? Results.NotFound() : Results.NoContent();
        });

        group.MapGet("/summary", async (ClaimsPrincipal user, AppDbContext db, string month) =>
        {
            if (!TryParseMonth(month, out var from, out var to))
                return Results.Problem("Le mois doit être au format AAAA-MM.", statusCode: StatusCodes.Status400BadRequest);

            var byCategory = await db.Expenses
                .Where(e => e.UserId == user.UserId() && e.Date >= from && e.Date < to)
                .GroupBy(e => e.Category)
                .Select(g => new CategoryTotal(g.Key, g.Sum(e => e.Amount), g.Count()))
                .ToListAsync();
            byCategory = byCategory.OrderByDescending(c => c.Total).ToList();

            return Results.Ok(new MonthlySummary(month, byCategory.Sum(c => c.Total), byCategory.Sum(c => c.Count), byCategory));
        });

        return group;
    }

    private static bool TryParseMonth(string month, out DateOnly from, out DateOnly to)
    {
        var ok = DateOnly.TryParseExact(month + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out from);
        to = ok ? from.AddMonths(1) : default;
        return ok;
    }
}
