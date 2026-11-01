using System.Diagnostics;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Helix.Orchestration.Infrastructure.Tests.Persistence;

public sealed class ScientificMigrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("helix_migration_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("CREATE ROLE helix_scientific_runtime NOLOGIN; GRANT helix_scientific_runtime TO CURRENT_USER; CREATE TABLE legacy_fixture (id integer PRIMARY KEY, value text NOT NULL); INSERT INTO legacy_fixture VALUES (1, 'preserve');", connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    [Fact]
    public async Task Disposable_runner_applies_replays_and_rejects_injected_crash_without_receipt()
    {
        var crashed = await RunRunnerAsync("011_scientific_runtime_immutable_repair.sql");
        Assert.NotEqual(0, crashed.ExitCode);

        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT count(*) FROM helix_schema_migrations WHERE migration_id = '011_scientific_runtime_immutable_repair.sql';", connection);
        Assert.Equal(0L, (long)(await command.ExecuteScalarAsync())!);

        var replay = await RunRunnerAsync();
        Assert.True(replay.ExitCode == 0, replay.Output);
        var repeat = await RunRunnerAsync();
        Assert.True(repeat.ExitCode == 0, repeat.Output);
        Assert.Equal(1L, (long)(await command.ExecuteScalarAsync())!);
        command.CommandText = "SELECT value FROM legacy_fixture WHERE id = 1;";
        Assert.Equal("preserve", (string)(await command.ExecuteScalarAsync())!);

        var alteredMigrations = Path.Combine(Path.GetTempPath(), $"helix-migrations-{Guid.NewGuid():N}");
        Directory.CreateDirectory(alteredMigrations);
        try
        {
            foreach (var source in Directory.EnumerateFiles(Path.Combine(FindRepositoryRoot(), "postgres", "migrations"), "*.sql"))
                File.Copy(source, Path.Combine(alteredMigrations, Path.GetFileName(source)));
            await File.AppendAllTextAsync(Path.Combine(alteredMigrations, "011_scientific_runtime_immutable_repair.sql"), "\n-- checksum mutation");
            var changedChecksum = await RunRunnerAsync(migrationDirectory: alteredMigrations);
            Assert.NotEqual(0, changedChecksum.ExitCode);
        }
        finally
        {
            Directory.Delete(alteredMigrations, recursive: true);
        }
    }

    private async Task<(int ExitCode, string Output)> RunRunnerAsync(string? failAfter = null, string? migrationDirectory = null)
    {
        var arguments = $"-NoProfile -File scripts/verification/phase01-migrate.ps1 -Mode CheckAndApply -ContainerName {_container.Id} -Database helix_migration_test -MigrationUser postgres -RuntimeUser helix_scientific_runtime -AllowDisposableTarget";
        if (failAfter is not null)
            arguments += $" -FailAfterSqlMigration {failAfter}";
        if (migrationDirectory is not null)
            arguments += $" -MigrationDirectory '{migrationDirectory}'";
        using var process = Process.Start(new ProcessStartInfo("powershell.exe", arguments)
        {
            WorkingDirectory = FindRepositoryRoot(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        })!;
        var output = await process.StandardOutput.ReadToEndAsync() + await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, output);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "scripts", "verification", "phase01-migrate.ps1")))
                return directory.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate the repository migration runner.");
    }
}
