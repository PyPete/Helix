using Helix.Orchestration.Infrastructure.Persistence;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Helix.Orchestration.Infrastructure.Tests.Persistence;

public sealed class PostgresRepositoriesTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    private DbConnectionFactory _connectionFactory = null!;

    public PostgresRepositoriesTests()
    {
        _container = new PostgreSqlBuilder("postgres:16")
            .WithDatabase("helix_orchestration")
            .WithUsername("helix")
            .WithPassword("helix_dev_password")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _connectionFactory = new DbConnectionFactory(_container.GetConnectionString());

        var migrationPath = Path.Combine(AppContext.BaseDirectory, "migrations", "001_initial.sql");
        var sql = await File.ReadAllTextAsync(migrationPath);

        await using var conn = _connectionFactory.Create();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task PostgresExperimentRepository_SaveAndGetByRunspaceId_Works()
    {
        var repo = new PostgresExperimentRepository(_connectionFactory);
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

    [Fact]
    public async Task PostgresCampaignRepository_SaveAndGetById_Works()
    {
        var repo = new PostgresCampaignRepository(_connectionFactory);

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
