using Npgsql;

namespace ExpenseTracker.Api.Data;

public static class ConnectionStrings
{
    /// <summary>
    /// Accepte une chaîne Npgsql classique ou une URL <c>postgres://user:pass@host:port/db</c>,
    /// le format fourni par la plupart des hébergeurs (Render, Railway, Heroku).
    /// </summary>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("ConnectionStrings:Default n'est pas configurée.");
        if (!value.StartsWith("postgres://") && !value.StartsWith("postgresql://"))
            return value;

        var uri = new Uri(value);
        var credentials = uri.UserInfo.Split(':', 2);
        return new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : null,
            SslMode = SslMode.Prefer,
        }.ConnectionString;
    }
}
