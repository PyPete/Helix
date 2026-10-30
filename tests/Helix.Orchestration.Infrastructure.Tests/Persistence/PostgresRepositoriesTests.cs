using Helix.Orchestration.Infrastructure.Persistence;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Helix.Orchestration.Infrastructure.Tests.Persistence;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer? _container;
    private readonly string? _connectionString;
    public DbConnectionFactory? ConnectionFactory { get; private set; }
    public bool IsAvailable { get; private set; }

    public PostgresFixture()
    {
        // CI service container path: PGHOST is set by the GitHub Actions postgres service
        var pgHost = Environment.GetEnvironmentVariable("PGHOST");
        if (!string.IsNullOrEmpty(pgHost))
        {
            var pgPort = Environment.GetEnvironmentVariable("PGPORT") ?? "5432";
            var pgDb = Environment.GetEnvironmentVariable("PGDATABASE") ?? "helix_orchestration";
            var pgUser = Environment.GetEnvironmentVariable("PGUSER") ?? "helix";
            var pgPassword = Environment.GetEnvironmentVariable("PGPASSWORD") ?? "helix_dev_password";
            _connectionString = $"Host={pgHost};Port={pgPort};Database={pgDb};Username={pgUser};Password={pgPassword}";
            _container = null;
            IsAvailable = true;
            return;
        }

        try
        {
            _container = new PostgreSqlBuilder("postgres:16")
                .WithDatabase("helix_orchestration")
                .WithUsername("helix")
                .WithPassword("helix_dev_password")
                .Build();
            IsAvailable = true;
        }
        catch
        {
            _container = null;
            IsAvailable = false;
        }
    }

    public async Task InitializeAsync()
    {
        if (!IsAvailable)
            throw new InvalidOperationException("PostgreSQL test container could not be configured.");

        try
        {
            string connStr;
            if (_container != null)
            {
                await _container.StartAsync();
                connStr = _container.GetConnectionString();
            }
            else
            {
                connStr = _connectionString!;
            }

            ConnectionFactory = new DbConnectionFactory(connStr);

            var migrationsPath = Path.Combine(AppContext.BaseDirectory, "migrations");
            var migrationPaths = Directory.EnumerateFiles(migrationsPath, "*.sql")
                .OrderBy(Path.GetFileName, StringComparer.Ordinal)
                .ToArray();
            if (migrationPaths.Length == 0)
                throw new DirectoryNotFoundException($"No migrations were found at {migrationsPath}");

            await using var conn = ConnectionFactory.Create();
            await conn.OpenAsync();
            foreach (var migrationPath in migrationPaths)
            {
                var sql = await File.ReadAllTextAsync(migrationPath);
                await using var cmd = new NpgsqlCommand(sql, conn);
                await cmd.ExecuteNonQueryAsync();
            }
        }
        catch (Exception exception)
        {
            IsAvailable = false;
            ConnectionFactory = null;
            throw new InvalidOperationException("PostgreSQL fixture initialization failed; required SQL tests cannot run.", exception);
        }
    }

    public async Task DisposeAsync()
    {
        if (_container == null) return;
        try
        {
            await _container.DisposeAsync();
        }
        catch
        {
            // Suppress disposal errors
        }
    }
}

[CollectionDefinition(nameof(PostgresRepositoriesTestsCollection))]
public class PostgresRepositoriesTestsCollection : ICollectionFixture<PostgresFixture>
{
}

[Collection(nameof(PostgresRepositoriesTestsCollection))]
public sealed class PostgresRepositoriesTests
{
    private readonly PostgresFixture _fixture;

    public PostgresRepositoriesTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PostgresExperimentRepository_SaveAndGetByRunspaceId_Works()
    {
        if (_fixture.ConnectionFactory == null || !_fixture.IsAvailable)
            return;

        var repo = new PostgresExperimentRepository(_fixture.ConnectionFactory);
        var record = new ExperimentRecord(
            RunspaceId: "runspace-1",
            ExperimentId: "runspace-1",
            CampaignId: null,
            DefinitionId: "def-1",
            Name: "Experiment 1",
            Status: "Completed",
            CreatedAt: DateTimeOffset.UtcNow,
            StartedAt: DateTimeOffset.UtcNow,
            EndedAt: DateTimeOffset.UtcNow,
            FaultMessage: null,
            DefinitionJson: "{\"nodes\":[]}");

        await repo.SaveAsync(record);

        var loaded = await repo.GetByRunspaceIdAsync("runspace-1");

        Assert.NotNull(loaded);
        Assert.Equal("def-1", loaded!.DefinitionId);
        Assert.Equal("Completed", loaded.Status);
    }

    [Fact]
    public async Task PostgresCampaignRepository_SaveAndGetById_Works()
    {
        if (_fixture.ConnectionFactory == null || !_fixture.IsAvailable)
            return;

        var repo = new PostgresCampaignRepository(_fixture.ConnectionFactory);

        var campaign = new CampaignRecord(
            CampaignId: "camp-2",
            Name: "Campaign 2",
            Description: "desc",
            Status: "Draft",
            CreatedAt: DateTimeOffset.UtcNow,
            StartedAt: null,
            EndedAt: null,
            Iterations: new List<CampaignIterationRecord>
            {
                new(
                    CampaignId: "camp-2",
                    IterationIndex: 0,
                    ParametersJson: "{\"temp\":50}",
                    RunspaceId: null,
                    Status: "Pending",
                    Message: null,
                    CreatedAt: DateTimeOffset.UtcNow,
                    StartedAt: null,
                    EndedAt: null)
            });

        await repo.SaveAsync(campaign);

        var loaded = await repo.GetByIdAsync("camp-2");

        Assert.NotNull(loaded);
        Assert.Equal("Campaign 2", loaded!.Name);
        Assert.Single(loaded.Iterations);
        Assert.Equal("Pending", loaded.Iterations[0].Status);
    }
}

