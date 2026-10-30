using Microsoft.Extensions.Logging.Abstractions;
using Opc.Ua;

namespace Helix.Orchestration.Devices.Transports.Opc;

/// <summary>
/// Factory for creating OPC UA sessions.
/// </summary>
public interface IOpcSessionFactory
{
    IOpcSession Create(OpcTransportOptions options, ApplicationConfiguration applicationConfiguration);
}

internal sealed class OpcFoundationSessionFactory : IOpcSessionFactory
{
    public IOpcSession Create(OpcTransportOptions options, ApplicationConfiguration applicationConfiguration)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(applicationConfiguration);

        return new OpcFoundationSession(
            options.EndpointUrl,
            applicationConfiguration,
            NullLogger<OpcFoundationSession>.Instance);
    }
}
