using Npgsql;

namespace Helix.Orchestration.Infrastructure.Persistence;

public sealed class DbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public NpgsqlConnection Create()
        => new(_connectionString);
}
