using Dapper;
using Npgsql;

namespace Helix.Orchestration.Infrastructure.Persistence;

public sealed class DbConnectionFactory
{
    private readonly string _connectionString;

    static DbConnectionFactory()
    {
        // Allow Dapper to map snake_case DB columns to PascalCase C# record properties
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public DbConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public NpgsqlConnection Create()
        => new(_connectionString);
}
