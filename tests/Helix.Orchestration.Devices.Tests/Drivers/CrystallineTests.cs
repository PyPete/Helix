using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Interfaces;
using Helix.Orchestration.Devices.Model;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class CrystallineTests
{
    private static Crystalline CreateDriver(
        ICrystallineSqlClient? sqlClient = null,
        IDeviceTransport? transport = null)
    {
        sqlClient ??= Substitute.For<ICrystallineSqlClient>();
        transport ??= Substitute.For<IDeviceTransport>();
        return new Crystalline(sqlClient, transport, NullLogger<Crystalline>.Instance);
    }

    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    // ---- CreateSession capability ----

    [Fact]
    public async Task CreateSession_Success_ReturnsSuccess()
    {
        var sql = Substitute.For<ICrystallineSqlClient>();
        sql.CreateSession("192.168.1.10").Returns(true);
        var driver = CreateDriver(sqlClient: sql);

        var result = await driver.InvokeCapabilityAsync("CreateSession",
            Json("{\"endPoint\":\"192.168.1.10\"}"));

        Assert.True(result.Success);
        sql.Received(1).CreateSession("192.168.1.10");
    }

    [Fact]
    public async Task CreateSession_Failure_ReturnsFailure()
    {
        var sql = Substitute.For<ICrystallineSqlClient>();
        sql.CreateSession(Arg.Any<string>()).Returns(false);
        var driver = CreateDriver(sqlClient: sql);

        var result = await driver.InvokeCapabilityAsync("CreateSession",
            Json("{\"endPoint\":\"192.168.1.10\"}"));

        Assert.False(result.Success);
        Assert.Contains("Could not connect", result.ErrorMessage);
    }

    // ---- CreateSubscription capability ----

    [Fact]
    public async Task CreateSubscription_Success_ReturnsSuccess()
    {
        var sql = Substitute.For<ICrystallineSqlClient>();
        sql.SubscribeToReactor("R1").Returns(true);
        sql.FindVariableNames("R1").Returns(new Dictionary<int, (string Name, string Unit)>
        {
            { 1, ("Temperature", "C") },
            { 2, ("Pressure", "bar") }
        });
        var driver = CreateDriver(sqlClient: sql);

        var result = await driver.InvokeCapabilityAsync("CreateSubscription",
            Json("{\"reactor\":\"R1\"}"));

        Assert.True(result.Success);
        sql.Received(1).SubscribeToReactor("R1");
        sql.Received(1).FindVariableNames("R1");
    }

    [Fact]
    public async Task CreateSubscription_Failure_ReturnsFailure()
    {
        var sql = Substitute.For<ICrystallineSqlClient>();
        sql.SubscribeToReactor(Arg.Any<string>()).Returns(false);
        sql.FindVariableNames(Arg.Any<string>()).Returns(new Dictionary<int, (string Name, string Unit)>());
        var driver = CreateDriver(sqlClient: sql);

        var result = await driver.InvokeCapabilityAsync("CreateSubscription",
            Json("{\"reactor\":\"R1\"}"));

        Assert.False(result.Success);
    }

    // ---- Close capability ----

    [Fact]
    public async Task Close_CallsCloseSession_ReturnsSuccess()
    {
        var sql = Substitute.For<ICrystallineSqlClient>();
        var driver = CreateDriver(sqlClient: sql);

        var result = await driver.InvokeCapabilityAsync("Close", Json("{}"));

        Assert.True(result.Success);
        sql.Received(1).CloseSession();
    }

    // ---- FindActiveSystems capability ----

    [Fact]
    public async Task FindActiveSystems_ReturnsReactorList()
    {
        var sql = Substitute.For<ICrystallineSqlClient>();
        sql.FindReactors().Returns(new[] { "ReactorA", "ReactorB" });
        var driver = CreateDriver(sqlClient: sql);

        var result = await driver.InvokeCapabilityAsync("FindActiveSystems", Json("{}"));

        Assert.True(result.Success);
        Assert.NotNull(result.ReturnValue);
        var json = result.ReturnValue!.Value.ToString();
        Assert.Contains("ReactorA", json);
        Assert.Contains("ReactorB", json);
    }

    // ---- IsDevice capability ----

    [Fact]
    public async Task IsDevice_ReactorLive_ReturnsSuccess()
    {
        var sql = Substitute.For<ICrystallineSqlClient>();
        sql.CreateSession(Arg.Any<string>()).Returns(true);
        sql.SubscribeToReactor("R1").Returns(true);
        sql.FindVariableNames("R1").Returns(new Dictionary<int, (string Name, string Unit)>());
        sql.ReactorLive("R1").Returns(true);
        var driver = CreateDriver(sqlClient: sql);
        await driver.InvokeCapabilityAsync("CreateSession", Json("{\"endPoint\":\"192.168.1.10\"}"));
        await driver.InvokeCapabilityAsync("CreateSubscription", Json("{\"reactor\":\"R1\"}"));

        var result = await driver.InvokeCapabilityAsync("IsDevice", Json("{}"));

        Assert.True(result.Success);
    }

    [Fact]
    public async Task IsDevice_NoSession_ReturnsFailure()
    {
        var driver = CreateDriver();

        var result = await driver.InvokeCapabilityAsync("IsDevice", Json("{}"));

        Assert.False(result.Success);
    }

    // ---- StreamTrendsAsync ----

    [Fact]
    public async Task StreamTrendsAsync_WithActiveReactor_EmitsTrendSamples()
    {
        var sql = Substitute.For<ICrystallineSqlClient>();
        sql.CreateSession(Arg.Any<string>()).Returns(true);
        sql.SubscribeToReactor("R1").Returns(true);
        sql.FindVariableNames("R1").Returns(new Dictionary<int, (string Name, string Unit)>());
        sql.GetRuntimeDataForReactor("R1").Returns(new[]
        {
            ("Temperature", 25.5, "C"),
            ("Pressure", 1.2, "bar")
        }.AsEnumerable());
        var driver = CreateDriver(sqlClient: sql);
        await driver.InvokeCapabilityAsync("CreateSession", Json("{\"endPoint\":\"192.168.1.10\"}"));
        await driver.InvokeCapabilityAsync("CreateSubscription", Json("{\"reactor\":\"R1\"}"));

        var samples = new List<TrendSample>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await foreach (var s in driver.StreamTrendsAsync(cts.Token))
        {
            samples.Add(s);
            if (samples.Count >= 2) break;
        }

        Assert.NotEmpty(samples);
        Assert.All(samples, s => Assert.StartsWith("crystalline.", s.TrendName));
    }

    // ---- Does NOT call Transport.SendReceiveAsync ----

    [Fact]
    public async Task Driver_DoesNotCallTransport()
    {
        var transport = Substitute.For<IDeviceTransport>();
        var sql = Substitute.For<ICrystallineSqlClient>();
        sql.CreateSession(Arg.Any<string>()).Returns(true);
        var driver = new Crystalline(sql, transport, NullLogger<Crystalline>.Instance);

        await driver.InvokeCapabilityAsync("CreateSession", Json("{\"endPoint\":\"192.168.1.10\"}"));
        await driver.InvokeCapabilityAsync("Close", Json("{}"));

        await transport.DidNotReceive().SendReceiveAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
    }
}
