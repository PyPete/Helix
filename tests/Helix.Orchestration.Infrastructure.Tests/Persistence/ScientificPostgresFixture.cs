using Helix.Orchestration.Infrastructure.Persistence;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Helix.Orchestration.Infrastructure.Tests.Persistence;

/// <summary>
/// Owns a disposable database with the migration owner and runtime role separated.
/// It never consults ambient PostgreSQL environment variables.
/// </summary>
public sealed class ScientificPostgresFixture : IAsyncLifetime
{
    private const string OwnerRole = "helix_scientific_owner";
    private const string RuntimeRole = "helix_scientific_runtime";
    private const string UntrustedRole = "helix_scientific_untrusted";
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("helix_scientific_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public DbConnectionFactory? ConnectionFactory { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            await _container.StartAsync();
            ConnectionFactory = new DbConnectionFactory(_container.GetConnectionString());
            await using var connection = ConnectionFactory.Create();
            await connection.OpenAsync();

            await ExecuteAsync(connection, $"CREATE ROLE {OwnerRole} NOLOGIN NOINHERIT; CREATE ROLE {RuntimeRole} NOLOGIN NOINHERIT; CREATE ROLE {UntrustedRole} NOLOGIN NOINHERIT; GRANT {OwnerRole}, {UntrustedRole} TO CURRENT_USER; GRANT {RuntimeRole} TO {OwnerRole} WITH ADMIN OPTION; GRANT CREATE ON SCHEMA public TO {OwnerRole}; SET ROLE {OwnerRole};");
            foreach (var migrationName in MigrationNames)
            {
                var migrationPath = Path.Combine(AppContext.BaseDirectory, "migrations", migrationName);
                if (!File.Exists(migrationPath))
                    throw new FileNotFoundException("Required scientific migration was not copied to the test output.", migrationPath);

                await ExecuteAsync(connection, await File.ReadAllTextAsync(migrationPath));
            }
        }
        catch (Exception exception)
        {
            ConnectionFactory = null;
            throw new InvalidOperationException("Disposable scientific PostgreSQL initialization failed; SQL verification cannot be skipped.", exception);
        }
    }

    public async Task<NpgsqlConnection> OpenRuntimeConnectionAsync()
    {
        var connection = ConnectionFactory?.Create() ?? throw new InvalidOperationException("Scientific PostgreSQL fixture was not initialized.");
        await connection.OpenAsync();
        await ExecuteAsync(connection, $"SET ROLE {RuntimeRole};");
        return connection;
    }

    public async Task<NpgsqlConnection> OpenUntrustedConnectionAsync()
    {
        var connection = ConnectionFactory?.Create() ?? throw new InvalidOperationException("Scientific PostgreSQL fixture was not initialized.");
        await connection.OpenAsync();
        await ExecuteAsync(connection, $"SET ROLE {UntrustedRole};");
        return connection;
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    private static readonly string[] MigrationNames =
    [
        "006_scientific_apparatus.sql",
        "007_scientific_procedures_plans.sql",
        "007_scientific_setup_least_privilege.sql",
        "008_scientific_samples.sql",
        "009_scientific_transformations.sql",
        "010_scientific_policy_freeze.sql",
        "011_scientific_runtime_immutable_repair.sql"
    ];

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}

[CollectionDefinition(nameof(ScientificPostgresCollection))]
public sealed class ScientificPostgresCollection : ICollectionFixture<ScientificPostgresFixture>;
