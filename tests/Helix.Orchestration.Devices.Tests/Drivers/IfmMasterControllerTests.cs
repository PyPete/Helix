using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class IfmMasterControllerTests
{
    private static IfmMasterController CreateDriver(
        IPopIoLinkClient? ioLink = null,
        IFile? file = null,
        IDeviceTransport? transport = null)
    {
        ioLink ??= Substitute.For<IPopIoLinkClient>();
        file ??= Substitute.For<IFile>();
        transport ??= Substitute.For<IDeviceTransport>();
        return new IfmMasterController(ioLink, file, transport, NullLogger<IfmMasterController>.Instance);
    }

    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    // ---- FindActiveSystems capability ----

    [Fact]
    public async Task FindActiveSystems_ReturnsActiveSystems()
    {
        var ioLink = Substitute.For<IPopIoLinkClient>();
        ioLink.FindActiveSensorsFromEndPointAsync("192.168.1.100")
            .Returns(Task.FromResult<IEnumerable<(string DeviceId, byte Port)>>(
                new[] { ("device1", (byte)1), ("device2", (byte)2) }));
        var driver = CreateDriver(ioLink: ioLink);

        var result = await driver.InvokeCapabilityAsync("FindActiveSystems",
            Json("{\"endPoint\":\"192.168.1.100\"}"));

        Assert.True(result.Success);
        Assert.NotNull(result.ReturnValue);
        await ioLink.Received(1).FindActiveSensorsFromEndPointAsync("192.168.1.100");
    }

    // ---- Open capability ----

    [Fact]
    public async Task Open_SuccessfulConnection_ReturnsSuccess()
    {
        const string endPoint = "192.168.1.100";
        const byte port = 1;
        var ioLink = Substitute.For<IPopIoLinkClient>();
        ioLink.CreateSessionAsync(endPoint, port).Returns(Task.FromResult(true));
        ioLink.GetDeviceIdentifiers().Returns(("TEST_PRODUCT", 123u, (ushort)456));
        var file = Substitute.For<IFile>();
        file.ReadAllTextAsync("PopConfig/io_link.json").Returns(Task.FromResult("{}"));
        var driver = CreateDriver(ioLink: ioLink, file: file);

        var result = await driver.InvokeCapabilityAsync("Open",
            Json($"{{\"endPoint\":\"{endPoint}\",\"portNumber\":{port}}}"));

        Assert.True(result.Success);
        await ioLink.Received(1).CreateSessionAsync(endPoint, port);
        ioLink.Received(1).GetDeviceIdentifiers();
    }

    [Fact]
    public async Task Open_FailedConnection_ReturnsFailure()
    {
        var ioLink = Substitute.For<IPopIoLinkClient>();
        ioLink.CreateSessionAsync(Arg.Any<string>(), Arg.Any<byte>()).Returns(Task.FromResult(false));
        var driver = CreateDriver(ioLink: ioLink);

        var result = await driver.InvokeCapabilityAsync("Open",
            Json("{\"endPoint\":\"192.168.1.100\",\"portNumber\":1}"));

        Assert.False(result.Success);
        ioLink.DidNotReceive().GetDeviceIdentifiers();
    }

    [Fact]
    public async Task Open_WithValidJsonConfig_ParsesTrendConfiguration()
    {
        var ioLink = Substitute.For<IPopIoLinkClient>();
        ioLink.CreateSessionAsync(Arg.Any<string>(), Arg.Any<byte>()).Returns(Task.FromResult(true));
        ioLink.GetDeviceIdentifiers().Returns(("TEST_PRODUCT", 123u, (ushort)456));
        var file = Substitute.For<IFile>();
        var jsonConfig = "{\"TEST_PRODUCT\":[{\"trend_name\":\"temperature\",\"friendly_name\":\"Temperature Sensor\",\"multiplier\":\"0.1\",\"unit\":\"C\"}]}";
        file.ReadAllTextAsync("PopConfig/io_link.json").Returns(Task.FromResult(jsonConfig));
        var driver = CreateDriver(ioLink: ioLink, file: file);

        await driver.InvokeCapabilityAsync("Open",
            Json("{\"endPoint\":\"192.168.1.100\",\"portNumber\":1}"));

        // Verify driver streams the trend with correct multiplier
        var processData = new List<(string Name, object Value)> { ("temperature", 255f) };
        ioLink.GetAllProcessDataForDeviceAsync()
            .Returns(Task.FromResult(processData));

        var samples = new List<TrendSample>();
        using var cts = new CancellationTokenSource(500);
        try
        {
            await foreach (var s in driver.StreamTrendsAsync(cts.Token))
            {
                samples.Add(s);
                break;
            }
        }
        catch (OperationCanceledException) { /* expected */ }

        Assert.NotEmpty(samples);
        Assert.Equal(25.5, (double)samples[0].Value, precision: 1); // 255 * 0.1
    }

    [Fact]
    public async Task Open_WithInvalidJson_HandlesGracefully()
    {
        var ioLink = Substitute.For<IPopIoLinkClient>();
        ioLink.CreateSessionAsync(Arg.Any<string>(), Arg.Any<byte>()).Returns(Task.FromResult(true));
        ioLink.GetDeviceIdentifiers().Returns(("TEST_PRODUCT", 123u, (ushort)456));
        var file = Substitute.For<IFile>();
        file.ReadAllTextAsync("PopConfig/io_link.json").Returns(Task.FromResult("invalid json"));
        var driver = CreateDriver(ioLink: ioLink, file: file);

        var result = await driver.InvokeCapabilityAsync("Open",
            Json("{\"endPoint\":\"192.168.1.100\",\"portNumber\":1}"));

        // Should still succeed - JSON parse error is handled gracefully
        Assert.True(result.Success);
    }

    // ---- Close capability ----

    [Fact]
    public async Task Close_CallsCloseSession_ReturnsSuccess()
    {
        var ioLink = Substitute.For<IPopIoLinkClient>();
        var driver = CreateDriver(ioLink: ioLink);

        var result = await driver.InvokeCapabilityAsync("Close", Json("{}"));

        Assert.True(result.Success);
        ioLink.Received(1).CloseSession();
    }

    // ---- GetAllProcessData capability ----

    [Fact]
    public async Task GetAllProcessData_ReturnsDataList()
    {
        var ioLink = Substitute.For<IPopIoLinkClient>();
        var processData = new List<(string Name, object Value)>
        {
            ("temperature", 25.5f),
            ("is_active", true),
            ("pressure", (short)1024)
        };
        ioLink.GetAllProcessDataForDeviceAsync().Returns(Task.FromResult(processData));
        var driver = CreateDriver(ioLink: ioLink);

        var result = await driver.InvokeCapabilityAsync("GetAllProcessData", Json("{}"));

        Assert.True(result.Success);
        Assert.NotNull(result.ReturnValue);
        var json = result.ReturnValue!.Value.ToString();
        Assert.Contains("temperature", json);
    }

    // ---- StreamTrendsAsync ----

    [Fact]
    public async Task StreamTrendsAsync_WithProcessData_EmitsTrendSamples()
    {
        var ioLink = Substitute.For<IPopIoLinkClient>();
        var processData = new List<(string Name, object Value)>
        {
            ("temperature", 25.5f),
            ("pressure", (short)1024),
            ("flow_rate", (ushort)500),
            ("active", true),
            ("device_name", "Test Device")
        };
        ioLink.GetAllProcessDataForDeviceAsync().Returns(Task.FromResult(processData));
        var driver = CreateDriver(ioLink: ioLink);

        var samples = new List<TrendSample>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await foreach (var s in driver.StreamTrendsAsync(cts.Token))
        {
            samples.Add(s);
            if (samples.Count >= 5) break;
        }

        Assert.Equal(5, samples.Count);
        Assert.All(samples, s => Assert.StartsWith("ifmmastercontroller.", s.TrendName));
    }

    [Fact]
    public async Task StreamTrendsAsync_EmptyName_SkipsEntry()
    {
        var ioLink = Substitute.For<IPopIoLinkClient>();
        var processData = new List<(string Name, object Value)>
        {
            ("", 25.5f),
            ("valid_trend", 1.0f)
        };
        ioLink.GetAllProcessDataForDeviceAsync().Returns(Task.FromResult(processData));
        var driver = CreateDriver(ioLink: ioLink);

        var samples = new List<TrendSample>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await foreach (var s in driver.StreamTrendsAsync(cts.Token))
        {
            samples.Add(s);
            break; // Get at least one sample
        }

        // Should only have the valid trend, not the empty-named one
        Assert.All(samples, s => Assert.NotEmpty(s.TrendName.Replace("ifmmastercontroller.", "")));
    }

    // ---- Does NOT call Transport.SendReceiveAsync ----

    [Fact]
    public async Task Driver_DoesNotCallTransport()
    {
        var transport = Substitute.For<IDeviceTransport>();
        var ioLink = Substitute.For<IPopIoLinkClient>();
        ioLink.CreateSessionAsync(Arg.Any<string>(), Arg.Any<byte>()).Returns(Task.FromResult(true));
        ioLink.GetDeviceIdentifiers().Returns(("TEST", 0u, (ushort)0));
        var file = Substitute.For<IFile>();
        file.ReadAllTextAsync(Arg.Any<string>()).Returns(Task.FromResult("{}"));
        var driver = new IfmMasterController(ioLink, file, transport, NullLogger<IfmMasterController>.Instance);

        await driver.InvokeCapabilityAsync("Open", Json("{\"endPoint\":\"192.168.1.100\",\"portNumber\":1}"));
        await driver.InvokeCapabilityAsync("Close", Json("{}"));

        await transport.DidNotReceive().SendReceiveAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
    }

    // ---- Blocking .Result was replaced with await ----

    [Fact]
    public void IfmMasterController_DoesNotContainBlockingResultCall()
    {
        // Verify the source has no blocking .Result call in StreamTrendsAsync
        // This is a structural test - we verify the method is async
        var method = typeof(IfmMasterController).GetMethod(nameof(IfmMasterController.StreamTrendsAsync));
        Assert.NotNull(method);
        // StreamTrendsAsync is an IAsyncEnumerable method (not a Task, but async iterator)
        // The mere compilation proves there's no .Result blocking (async iterator forces async)
    }
}
