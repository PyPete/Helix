using System.Reflection;
using System.Runtime.InteropServices;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.CDll;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Transports;

public sealed class CDllTransportTests
{
    [Fact]
    public async Task ConnectAsync_WhenLibraryDoesNotExist_ThrowsTransportFaultException()
    {
        var options = new CDllTransportOptions
        {
            DllPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
            DllName = "missing-device.dll"
        };
        var transport = new CDllDeviceTransport(options, NullLogger<CDllDeviceTransport>.Instance);

        var exception = await Assert.ThrowsAsync<TransportFaultException>(() => transport.ConnectAsync());

        Assert.Equal("C DLL", exception.TransportType);
        Assert.Equal(TransportStatus.Faulted, transport.Status);
    }

    [Fact]
    public void ResolveLibraryPath_WhenArchitectureSubfolderEnabled_ReturnsExpectedPath()
    {
        var options = new CDllTransportOptions
        {
            DllPath = @"C:\vendor",
            DllName = "device.dll",
            UseArchitectureSubfolder = true
        };
        var transport = new CDllDeviceTransport(options, NullLogger<CDllDeviceTransport>.Instance);

        var resolvedPath = InvokeResolveLibraryPath(transport);

        Assert.Equal(Path.Combine(@"C:\vendor", "runtimes", GetArchitectureFolder(), "native", "device.dll"), resolvedPath);
    }

    [Fact]
    public async Task IsConnected_ReturnsFalse_WhenHandleIsInvalid()
    {
        var transport = new CDllDeviceTransport(
            new CDllTransportOptions { DllName = "ignored.dll" },
            NullLogger<CDllDeviceTransport>.Instance);

        SetLibraryHandle(transport, new CDllDeviceTransport.SafeLibraryHandle(IntPtr.Zero));

        Assert.False(transport.IsConnected);

        await transport.DisposeAsync();
    }

    [Fact]
    public async Task IsConnected_ReturnsTrue_WhenHandleIsLoadedAndNoValidationIsRequired()
    {
        var nativeHandle = NativeLibrary.Load(GetKnownSystemLibraryName());
        var transport = new CDllDeviceTransport(
            new CDllTransportOptions { DllName = "ignored.dll" },
            NullLogger<CDllDeviceTransport>.Instance);

        SetLibraryHandle(transport, new CDllDeviceTransport.SafeLibraryHandle(nativeHandle));

        Assert.True(transport.IsConnected);

        await transport.DisposeAsync();
    }

    private static string InvokeResolveLibraryPath(CDllDeviceTransport transport)
    {
        var method = typeof(CDllDeviceTransport).GetMethod("ResolveLibraryPath", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (string)method.Invoke(transport, null)!;
    }

    private static void SetLibraryHandle(CDllDeviceTransport transport, CDllDeviceTransport.SafeLibraryHandle handle)
    {
        var field = typeof(CDllDeviceTransport).GetField("_libraryHandle", BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(transport, handle);
    }

    private static string GetArchitectureFolder() => RuntimeInformation.ProcessArchitecture switch
    {
        Architecture.X64 => "win-x64",
        Architecture.X86 => "win-x86",
        Architecture.Arm64 => "win-arm64",
        Architecture.Arm => "win-arm",
        _ => throw new PlatformNotSupportedException($"Unsupported architecture: {RuntimeInformation.ProcessArchitecture}")
    };

    private static string GetKnownSystemLibraryName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "kernel32.dll";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "libc.so.6";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "/usr/lib/libSystem.B.dylib";
        }

        throw new PlatformNotSupportedException("Unsupported operating system");
    }
}
