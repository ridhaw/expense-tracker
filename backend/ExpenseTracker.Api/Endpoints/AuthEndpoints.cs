using ExpenseTracker.Api.Auth;
using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentification");

        group.MapPost("/register", async (
            RegisterRequest request, AppDbContext db, IPasswordHasher<User> hasher, TokenService tokens, TimeProvider clock) =>
        {
            var email = request.Email.Trim().ToLowerInvariant();
            if (await db.Users.AnyAsync(u => u.Email == email))
                return Results.Problem("Un compte existe déjà avec cet email.", statusCode: StatusCodes.Status409Conflict);

            var user = new User { Email = email, CreatedAt = clock.GetUtcNow() };
            user.PasswordHash = hasher.HashPassword(user, request.Password);
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var (token, expiresAt) = tokens.Issue(user);
            return Results.Created("/api/auth/me", new AuthResponse(token, expiresAt, user.Email));
        }).WithValidation();

        group.MapPost("/login", async (
            LoginRequest request, AppDbContext db, IPasswordHasher<User> hasher, TokenService tokens) =>
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
            // Même réponse pour un email inconnu et un mauvais mot de passe : on ne révèle pas quels comptes existent.
            if (user is null || hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
                return Results.Problem("Email ou mot de passe incorrect.", statusCode: StatusCodes.Status401Unauthorized);

            var (token, expiresAt) = tokens.Issue(user);
            return Results.Ok(new AuthResponse(token, expiresAt, user.Email));
        }).WithValidation();

        return group;
    }
}
