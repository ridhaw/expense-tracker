using ExpenseTracker.Api.Data;
using Npgsql;

namespace ExpenseTracker.Api.Tests;

public sealed class ConnectionStringsTests
{
    [Fact]
    public void Keeps_a_regular_connection_string()
    {
        const string value = "Host=db;Database=expenses;Username=u;Password=p";
        Assert.Equal(value, ConnectionStrings.Normalize(value));
    }

    [Fact]
    public void Converts_a_postgres_url()
    {
        var builder = new NpgsqlConnectionStringBuilder(
            ConnectionStrings.Normalize("postgres://alice:s%40cret@db.example.com:6543/expenses"));

        Assert.Equal("db.example.com", builder.Host);
        Assert.Equal(6543, builder.Port);
        Assert.Equal("expenses", builder.Database);
        Assert.Equal("alice", builder.Username);
        Assert.Equal("s@cret", builder.Password);
    }

    [Fact]
    public void Missing_value_fails_loudly()
    {
        Assert.Throws<InvalidOperationException>(() => ConnectionStrings.Normalize(null));
    }
}
