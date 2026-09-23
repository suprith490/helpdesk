using HelpDesk.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Tests.TestSupport;

/// <summary>
/// Spins up an isolated, in-memory SQLite database that lives for the
/// lifetime of a single test. Each test class creates one of these so tests
/// never interfere with each other or with the developer's real database.
/// </summary>
public sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteTestDatabase()
    {
        // A normal ":memory:" database is destroyed as soon as the last
        // connection closes, so we keep one connection open for the whole test.
        // Pooling must be disabled: otherwise Microsoft.Data.Sqlite reuses the
        // same pooled connection (and therefore the same in-memory database)
        // for the next test, which would leak data between tests.
        _connection = new SqliteConnection("DataSource=:memory:;Pooling=False;");
        _connection.Open();

        Context = CreateContext();
        Context.Database.EnsureCreated();
    }

    public AppDbContext Context { get; }

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new AppDbContext(options);
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
