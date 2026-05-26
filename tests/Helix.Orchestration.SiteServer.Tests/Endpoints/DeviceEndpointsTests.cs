using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Helix.Orchestration.SiteServer.Tests.Endpoints;

public class DeviceEndpointsTests : IClassFixture<HelixWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DeviceEndpointsTests(HelixWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetDevices_Returns200WithThreeSimDevices()
    {
        var response = await _client.GetAsync("/api/devices");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var devices = await response.Content.ReadFromJsonAsync<DeviceDto[]>();
        Assert.NotNull(devices);
        Assert.Equal(3, devices!.Length);
        Assert.Contains(devices, d => d.DeviceKind == "Reactor");
        Assert.Contains(devices, d => d.DeviceKind == "Pump");
        Assert.Contains(devices, d => d.DeviceKind == "Spectrometer");
    }

    [Fact]
    public async Task GetDevices_EachDeviceHasCapabilities()
    {
        var response = await _client.GetAsync("/api/devices");
        response.EnsureSuccessStatusCode();

        var devices = await response.Content.ReadFromJsonAsync<DeviceDto[]>();
        Assert.NotNull(devices);

        foreach (var device in devices!)
        {
            Assert.NotEmpty(device.Capabilities);
        }
    }

    private sealed record DeviceDto(
        string DeviceId,
        string DeviceName,
        string DeviceKind,
        string[] Capabilities);
}
