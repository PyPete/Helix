using Helix.Orchestration.Infrastructure.Persistence;
using Helix.Orchestration.Core.ScientificSetup;
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

    [Fact]
    public async Task ScientificSetup_Create_ReplaysEquivalentRequestAndRejectsConflict()
    {
        if (_fixture.ConnectionFactory == null || !_fixture.IsAvailable)
            return;

        var repository = new PostgresScientificSetupRepository(_fixture.ConnectionFactory);
        var operationId = Guid.NewGuid();
        var apparatus = CreateApparatus();

        var created = await repository.CreateAsync(apparatus, "scientist-a", operationId);
        var replayed = await repository.CreateAsync(CreateApparatus(), "scientist-a", operationId);

        Assert.Equal(created.Apparatus.ApparatusId, replayed.Apparatus.ApparatusId);
        Assert.Equal(created.Receipt, replayed.Receipt);
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.CreateAsync(
            apparatus with { Name = "Changed apparatus" }, "scientist-a", operationId));
    }

    [Fact]
    public async Task ScientificSetup_Create_ConcurrentDuplicateReturnsOneDurableSetup()
    {
        if (_fixture.ConnectionFactory == null || !_fixture.IsAvailable)
            return;

        var operationId = Guid.NewGuid();
        var apparatus = CreateApparatus();
        var first = new PostgresScientificSetupRepository(_fixture.ConnectionFactory);
        var second = new PostgresScientificSetupRepository(_fixture.ConnectionFactory);

        var results = await Task.WhenAll(
            first.CreateAsync(apparatus, "scientist-concurrent", operationId),
            second.CreateAsync(apparatus, "scientist-concurrent", operationId));

        Assert.Equal(results[0].Apparatus.ApparatusId, results[1].Apparatus.ApparatusId);
        Assert.Equal(results[0].Receipt, results[1].Receipt);
    }

    [Fact]
    public async Task ScientificSetup_DraftSaveUsesExpectedTokenAndReconstructsHistoricalContent()
    {
        if (_fixture.ConnectionFactory == null || !_fixture.IsAvailable)
            return;

        var repository = new PostgresScientificSetupRepository(_fixture.ConnectionFactory);
        var created = await repository.CreateAsync(CreateApparatus(), "scientist-draft", Guid.NewGuid());
        var saved = await repository.SaveDraftAsync(created.Apparatus with { Name = "Saved apparatus" }, created.Token, "scientist-draft", Guid.NewGuid());
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.SaveDraftAsync(saved.Apparatus, created.Token, "scientist-draft", Guid.NewGuid()));

        var published = await repository.PublishAsync(created.Apparatus.ApparatusId, null, "immutable revision", "scientist-draft", Guid.NewGuid());
        var draft = await repository.ReadDraftAsync(created.Apparatus.ApparatusId, "scientist-draft");
        var adopted = await repository.AdoptRevisionAsync(created.Apparatus.ApparatusId, published.Revision.RevisionId, draft.Token, "scientist-draft", Guid.NewGuid());

        Assert.Equal(published.Revision.RevisionId, adopted.Apparatus.SourceRevisionId);
        Assert.Equal("Saved apparatus", adopted.Apparatus.Name);
        var reconstructed = await repository.ReadAsync(created.Apparatus.ApparatusId, published.Revision.RevisionId, "scientist-draft");
        Assert.NotNull(reconstructed);
        Assert.Equal(published.Revision.ContentHash, reconstructed!.ContentHash);
        Assert.Contains("Saved apparatus", reconstructed.CanonicalContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ScientificSetup_RuntimeRoleCannotDeleteAndLeavesDataUnchanged()
    {
        if (_fixture.ConnectionFactory == null || !_fixture.IsAvailable)
            return;

        var repository = new PostgresScientificSetupRepository(_fixture.ConnectionFactory);
        var created = await repository.CreateAsync(CreateApparatus(), "scientist-runtime", Guid.NewGuid());
        await using var connection = _fixture.ConnectionFactory.Create();
        await connection.OpenAsync();
        await using (var setRole = new NpgsqlCommand("SET ROLE helix_scientific_runtime;", connection))
            await setRole.ExecuteNonQueryAsync();
        await using (var delete = new NpgsqlCommand("DELETE FROM scientific_apparatus WHERE apparatus_id = @id;", connection))
        {
            delete.Parameters.AddWithValue("id", created.Apparatus.ApparatusId);
            await Assert.ThrowsAsync<PostgresException>(() => delete.ExecuteNonQueryAsync());
        }
        await using (var resetRole = new NpgsqlCommand("RESET ROLE;", connection))
            await resetRole.ExecuteNonQueryAsync();

        var stored = await repository.OperationAsync(created.Receipt.OperationId, "scientist-runtime");
        Assert.NotNull(stored);
    }

    private static ApparatusDefinition CreateApparatus() => new(
        Guid.NewGuid(),
        "Example apparatus",
        [new ApparatusComponent(Guid.Parse("e42b4597-7b99-40bd-9124-f183f3ac6d7e"), "Vessel", ["Source"])]);
}

