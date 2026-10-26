using ExpenseTracker.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Expense> Expenses => Set<Expense>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<User>(user =>
        {
            user.ToTable("users");
            user.HasKey(u => u.Id);
            user.Property(u => u.Email).HasMaxLength(254).IsRequired();
            user.HasIndex(u => u.Email).IsUnique();
            user.Property(u => u.PasswordHash).IsRequired();
            user.HasMany(u => u.Expenses).WithOne().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<Expense>(expense =>
        {
            expense.ToTable("expenses", t => t.HasCheckConstraint("ck_expenses_amount_positive", "\"Amount\" > 0"));
            expense.HasKey(e => e.Id);
            expense.Property(e => e.Label).HasMaxLength(120).IsRequired();
            expense.Property(e => e.Amount).HasPrecision(12, 3);
            expense.Property(e => e.Category).HasConversion<string>().HasMaxLength(20);
            expense.HasIndex(e => new { e.UserId, e.Date });
        });
    }
}
