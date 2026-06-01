using System.Runtime.CompilerServices;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Interfaces;
using Helix.Orchestration.Devices.Model;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Crystalline reactor. Uses SQL Server transport via ICrystallineSqlClient.
/// The IDeviceTransport parameter satisfies the base class requirement but is never used for I/O.
/// </summary>
public sealed class Crystalline : DeviceAdapterBase
{
    private readonly ICrystallineSqlClient _sqlClient;

    private string _activeReactor = string.Empty;
    private string _ipAddress = string.Empty;
    private bool _active;
    private readonly Dictionary<string, (string TrendName, string Unit)> _trendDetails = new();

    public Crystalline(
        ICrystallineSqlClient sqlClient,
        IDeviceTransport transport,
        ILogger<Crystalline> logger)
        : base(transport, logger)
    {
        _sqlClient = sqlClient;
    }

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // CreateSession: connects to the Crystalline SQL database
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "CreateSession",
                "Connect to the Crystalline SQL Server database",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"endPoint\":{\"type\":\"string\"}},\"required\":[\"endPoint\"]}"),
            (parameters, _, _) =>
            {
                var endPoint = parameters.GetProperty("endPoint").GetString() ?? string.Empty;
                _ipAddress = endPoint;
                var ok = _sqlClient.CreateSession(endPoint);
                _active = ok;
                return Task.FromResult(ok
                    ? CapabilityInvocationResult.Successful()
                    : CapabilityInvocationResult.Failed("Could not connect to Crystalline database"));
            });

        // CreateSubscription: subscribes to a reactor
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "CreateSubscription",
                "Subscribe to a Crystalline reactor",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"reactor\":{\"type\":\"string\"}},\"required\":[\"reactor\"]}"),
            (parameters, _, _) =>
            {
                var reactor = parameters.GetProperty("reactor").GetString() ?? string.Empty;
                var ok = _sqlClient.SubscribeToReactor(reactor);
                _activeReactor = reactor;

                var variables = _sqlClient.FindVariableNames(reactor);
                foreach (var (_, (name, unit)) in variables)
                    _trendDetails.TryAdd(name, (name, unit));

                return Task.FromResult(ok
                    ? CapabilityInvocationResult.Successful()
                    : CapabilityInvocationResult.Failed("Could not subscribe to reactor"));
            });

        // Close: closes the SQL session
        builder.RegisterSimple(
            "Close",
            "Close the Crystalline SQL session",
            _ =>
            {
                _sqlClient.CloseSession();
                _active = false;
                return Task.FromResult(CapabilityInvocationResult.Successful());
            });

        // FindActiveSystems: returns the list of active reactors
        builder.RegisterSimple(
            "FindActiveSystems",
            "List all active Crystalline reactors in the database",
            _ =>
            {
                var reactors = _sqlClient.FindReactors().ToList();
                return Task.FromResult(CapabilityInvocationResult.Successful(
                    JsonSerializer.SerializeToElement(reactors)));
            });

        // IsDevice: checks if the reactor is live
        builder.RegisterSimple(
            "IsDevice",
            "Check whether the active Crystalline reactor is live",
            _ =>
            {
                if (!_active)
                    return Task.FromResult(CapabilityInvocationResult.Failed("Reactor is not live (no active session)"));
                var live = _sqlClient.ReactorLive(_activeReactor);
                return Task.FromResult(live
                    ? CapabilityInvocationResult.Successful()
                    : CapabilityInvocationResult.Failed("Reactor is not live"));
            });
    }

    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Image trends omitted from StreamTrendsAsync — images are not suitable as scalar TrendSample values.

        while (!cancellationToken.IsCancellationRequested)
        {
            if (!string.IsNullOrEmpty(_activeReactor))
            {
                IEnumerable<(string TrendName, double Value, string Unit)>? data = null;
                try
                {
                    data = _sqlClient.GetRuntimeDataForReactor(_activeReactor);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Crystalline: failed to poll GetRuntimeDataForReactor");
                }

                if (data is not null)
                {
                    var timestamp = DateTimeOffset.UtcNow;
                    foreach (var (trendName, value, unit) in data)
                    {
                        yield return new TrendSample
                        {
                            TrendName = $"crystalline.{trendName}",
                            Value = value,
                            Timestamp = timestamp,
                            Unit = unit
                        };
                    }
                }
            }

            var cancelled = false;
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
            if (cancelled) yield break;
        }
    }
}
