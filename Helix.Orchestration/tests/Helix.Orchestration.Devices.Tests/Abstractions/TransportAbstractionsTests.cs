using Helix.Orchestration.Devices.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Abstractions;

public sealed class TransportAbstractionsTests
{
    [Theory]
    [InlineData(TransportStatus.Disconnected, 0)]
    [InlineData(TransportStatus.Connecting, 1)]
    [InlineData(TransportStatus.Connected, 2)]
    [InlineData(TransportStatus.Faulted, 3)]
    [InlineData(TransportStatus.Reconnecting, 4)]
    public void TransportStatus_UsesExpectedEnumValues(TransportStatus status, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)status);
    }

    [Fact]
    public void TransportFaultException_PropagatesPropertiesAndInnerException()
    {
        var inner = new InvalidOperationException("boom");

        var exception = new TransportFaultException(
            "transport failed",
            "Serial",
            TransportStatus.Connected,
            inner);

        Assert.Equal("transport failed", exception.Message);
        Assert.Equal("Serial", exception.TransportType);
        Assert.Equal(TransportStatus.Connected, exception.PreviousStatus);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void TransportFaultException_AllowsNullInnerException()
    {
        var exception = new TransportFaultException(
            "fault without inner",
            "TCP",
            TransportStatus.Reconnecting);

        Assert.Equal("TCP", exception.TransportType);
        Assert.Equal(TransportStatus.Reconnecting, exception.PreviousStatus);
        Assert.Null(exception.InnerException);
    }
}
