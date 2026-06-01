using System.Data;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class AtlasReactionSystemTests
{
    private static AtlasReactionSystem CreateDriver(
        IDirectoryInfo? directoryInfo = null,
        IReadCsvFile? csvReader = null,
        IGetProcess? processChecker = null,
        IDeviceTransport? transport = null)
    {
        directoryInfo ??= Substitute.For<IDirectoryInfo>();
        csvReader ??= Substitute.For<IReadCsvFile>();
        processChecker ??= Substitute.For<IGetProcess>();
        transport ??= Substitute.For<IDeviceTransport>();
        return new AtlasReactionSystem(
            directoryInfo,
            csvReader,
            processChecker,
            transport,
            NullLogger<AtlasReactionSystem>.Instance);
    }

    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    // ---- Open capability ----

    [Fact]
    public async Task Open_ValidPath_ReturnsSuccess()
    {
        var dir = Substitute.For<IDirectoryInfo>();
        dir.CheckFolderExists("C:\\Data").Returns(true);
        var driver = CreateDriver(directoryInfo: dir);

        var result = await driver.InvokeCapabilityAsync("Open", Json("{\"dataLogPath\":\"C:\\\\Data\"}"));

        Assert.True(result.Success);
        dir.Received(1).SetPath("C:\\Data");
    }

    [Fact]
    public async Task Open_InvalidPath_ReturnsFailure()
    {
        var dir = Substitute.For<IDirectoryInfo>();
        dir.CheckFolderExists(Arg.Any<string>()).Returns(false);
        var driver = CreateDriver(directoryInfo: dir);

        var result = await driver.InvokeCapabilityAsync("Open", Json("{\"dataLogPath\":\"C:\\\\NonExistent\"}"));

        Assert.False(result.Success);
        Assert.Contains("does not exist", result.ErrorMessage);
    }

    // ---- IsDevice capability ----

    [Fact]
    public async Task IsDevice_AtlasRunning_ReturnsSuccess()
    {
        var proc = Substitute.For<IGetProcess>();
        proc.FindProcessByName("Atlas").Returns((true, "Atlas - MyMethod.atx"));
        var driver = CreateDriver(processChecker: proc);

        var result = await driver.InvokeCapabilityAsync("IsDevice", Json("{}"));

        Assert.True(result.Success);
        Assert.NotNull(result.ReturnValue);
    }

    [Fact]
    public async Task IsDevice_AtlasNotRunning_ReturnsFailure()
    {
        var proc = Substitute.For<IGetProcess>();
        proc.FindProcessByName("Atlas").Returns((false, string.Empty));
        var driver = CreateDriver(processChecker: proc);

        var result = await driver.InvokeCapabilityAsync("IsDevice", Json("{}"));

        Assert.False(result.Success);
        Assert.Contains("not running", result.ErrorMessage);
    }

    // ---- Close capability ----

    [Fact]
    public async Task Close_ReturnsSuccess()
    {
        var driver = CreateDriver();

        var result = await driver.InvokeCapabilityAsync("Close", Json("{}"));

        Assert.True(result.Success);
    }

    // ---- GetLatestData capability ----

    [Fact]
    public async Task GetLatestData_WithOpenPath_ReturnsData()
    {
        var dir = Substitute.For<IDirectoryInfo>();
        dir.CheckFolderExists("C:\\Data").Returns(true);
        var csv = Substitute.For<IReadCsvFile>();
        var dt = BuildCsvTable(new[] { "Time", "Step", "Status", "Temp", "Pressure" },
            new[] { "1", "2", "running", "25.5", "1.2" });
        csv.Read("C:\\Data").Returns(dt);
        var driver = CreateDriver(directoryInfo: dir, csvReader: csv);
        await driver.InvokeCapabilityAsync("Open", Json("{\"dataLogPath\":\"C:\\\\Data\"}"));

        var result = await driver.InvokeCapabilityAsync("GetLatestData", Json("{}"));

        Assert.True(result.Success);
        Assert.NotNull(result.ReturnValue);
        var json = result.ReturnValue!.Value.ToString();
        Assert.Contains("Temp", json);
        Assert.Contains("25.5", json);
    }

    [Fact]
    public async Task GetLatestData_WithoutOpenPath_ReturnsFailure()
    {
        var driver = CreateDriver();

        var result = await driver.InvokeCapabilityAsync("GetLatestData", Json("{}"));

        Assert.False(result.Success);
        Assert.Contains("Open first", result.ErrorMessage);
    }

    // ---- StreamTrendsAsync ----

    [Fact]
    public async Task StreamTrendsAsync_WithOpenPath_EmitsTrendSamples()
    {
        var dir = Substitute.For<IDirectoryInfo>();
        dir.CheckFolderExists("C:\\Data").Returns(true);
        var csv = Substitute.For<IReadCsvFile>();
        var dt = BuildCsvTable(
            new[] { "Time", "Step", "Status", "Temperature", "Pressure" },
            new[] { "1", "2", "running", "25.5", "1.2" },
            new[] { "2", "3", "running", "26.0", "1.3" },
            new[] { "3", "4", "running", "26.5", "1.4" });
        csv.Read("C:\\Data").Returns(dt);
        var driver = CreateDriver(directoryInfo: dir, csvReader: csv);
        await driver.InvokeCapabilityAsync("Open", Json("{\"dataLogPath\":\"C:\\\\Data\"}"));

        var samples = new List<TrendSample>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await foreach (var s in driver.StreamTrendsAsync(cts.Token))
        {
            samples.Add(s);
            if (samples.Count >= 2) break;
        }

        Assert.NotEmpty(samples);
        Assert.All(samples, s => Assert.StartsWith("atlasreactionsystem.", s.TrendName));
    }

    // ---- Does NOT call Transport.SendReceiveAsync ----

    [Fact]
    public async Task Driver_DoesNotCallTransport()
    {
        var transport = Substitute.For<IDeviceTransport>();
        var dir = Substitute.For<IDirectoryInfo>();
        dir.CheckFolderExists(Arg.Any<string>()).Returns(true);
        var driver = new AtlasReactionSystem(
            dir,
            Substitute.For<IReadCsvFile>(),
            Substitute.For<IGetProcess>(),
            transport,
            NullLogger<AtlasReactionSystem>.Instance);

        await driver.InvokeCapabilityAsync("Open", Json("{\"dataLogPath\":\"C:\\\\Data\"}"));
        await driver.InvokeCapabilityAsync("Close", Json("{}"));

        await transport.DidNotReceive().SendReceiveAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
    }

    // ---- Helpers ----

    private static DataTable BuildCsvTable(string[] columns, params string[][] rows)
    {
        var dt = new DataTable();
        foreach (var col in columns)
            dt.Columns.Add(col);
        foreach (var row in rows)
        {
            var dr = dt.NewRow();
            for (var i = 0; i < row.Length && i < columns.Length; i++)
                dr[i] = row[i];
            dt.Rows.Add(dr);
        }
        return dt;
    }
}
