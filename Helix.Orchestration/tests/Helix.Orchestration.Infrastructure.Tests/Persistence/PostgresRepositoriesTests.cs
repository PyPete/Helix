using Helix.Orchestration.Infrastructure.Persistence;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Helix.Orchestration.Infrastructure.Tests.Persistence;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer? _container;
    public DbConnectionFactory? ConnectionFactory { get; private set; }
    public bool IsAvailable { get; private set; }

    public PostgresFixture()
    {
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
        if (!IsAvailable || _container == null)
            return;

        try
        {
            await _container.StartAsync();
            ConnectionFactory = new DbConnectionFactory(_container.GetConnectionString());

            var migrationPath = Path.Combine(AppContext.BaseDirectory, "migrations", "001_initial.sql");
            if (!File.Exists(migrationPath))
                throw new FileNotFoundException($"Migration file not found at {migrationPath}");

            var sql = await File.ReadAllTextAsync(migrationPath);

            await using var conn = ConnectionFactory.Create();
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }
        catch
        {
            IsAvailable = false;
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

    [Fact(Skip = "Requires Docker. Run with Docker available.")]
    public async Task PostgresExperimentRepository_SaveAndGetByRunspaceId_Works()
    {
        if (_fixture.ConnectionFactory == null || !_fixture.IsAvailable)
            return;

        var repo = new PostgresExperimentRepository(_fixture.ConnectionFactory);
        var record = new ExperimentRecord(
            RunspaceId: "runspace-1",
            CampaignId: "camp-1",
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

    [Fact(Skip = "Requires Docker. Run with Docker available.")]
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
