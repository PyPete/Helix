using System.Globalization;
using System.Text.Json;
using Helix.Orchestration.Core.Execution;
using Helix.Orchestration.Core.Safety;

namespace Helix.Orchestration.Agent.Execution;

/// <summary>
/// Obtains a fresh, fail-closed command-admission context at the Agent boundary.
/// A proposal is never authorized from historical evidence or an absent policy.
/// </summary>
public sealed class AdmissionContextProvider
{
    private static readonly TimeSpan DefaultFreshness = TimeSpan.FromSeconds(2);

    private readonly SafetyEngine _safetyEngine;
    private readonly IAdmissionStateSource _stateSource;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _freshness;

    public AdmissionContextProvider(
        SafetyEngine safetyEngine,
        IAdmissionStateSource? stateSource = null,
        TimeProvider? timeProvider = null,
        TimeSpan? freshness = null)
    {
        ArgumentNullException.ThrowIfNull(safetyEngine);

        _safetyEngine = safetyEngine;
        _stateSource = stateSource ?? UnavailableAdmissionStateSource.Instance;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _freshness = freshness ?? DefaultFreshness;

        if (_freshness <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(freshness), "Admission freshness must be positive.");
        }
    }

    /// <summary>
    /// Builds one immutable snapshot for every mandatory check. Failures to read
    /// policy, lease, budget, health, or safety state become <see cref="CommandCheckStatus.Unknown"/>.
    /// </summary>
    public async Task<CommandAdmissionContext> CreateAsync(
        CommandIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intent);

        var safetyState = await GetSafetyStateAsync(_timeProvider.GetUtcNow()).ConfigureAwait(false);
        var state = await GetStateAsync(intent, cancellationToken).ConfigureAwait(false);
        var now = _timeProvider.GetUtcNow();
        var stateIsFresh = IsFresh(state.OccurredAtUtc, now);

        var statuses = new Dictionary<string, CommandCheckStatus>(StringComparer.Ordinal)
        {
            ["capability"] = GetCapabilityStatus(intent, state, stateIsFresh),
            ["safety"] = GetFreshStatus(safetyState.Status, safetyState.OccurredAtUtc, now),
            ["lease"] = GetFreshStatus(state.LeaseStatus, state.OccurredAtUtc, now),
            ["resource"] = GetFreshStatus(state.ResourceStatus, state.OccurredAtUtc, now),
            ["budget"] = GetFreshStatus(state.BudgetStatus, state.OccurredAtUtc, now),
            ["termination"] = GetFreshStatus(state.TerminationStatus, state.OccurredAtUtc, now),
            ["health"] = GetFreshStatus(state.HealthStatus, state.OccurredAtUtc, now)
        };

        var stateRevision = stateIsFresh ? state.ContextRevision : $"stale:{state.ContextRevision}";
        return new CommandAdmissionContext(
            statuses,
            $"safety:{safetyState.ContextRevision}|admission:{stateRevision}");
    }

    private async Task<SafetyState> GetSafetyStateAsync(DateTimeOffset now)
    {
        try
        {
            // This is an in-memory interlock evaluation. SafetyEngine deliberately
            // performs no SQL work while its synchronous E-stop gate is held.
            await _safetyEngine.EvaluateInterlocksAsync().ConfigureAwait(false);
            return _safetyEngine.LastSafetyState;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return SafetyState.Unknown($"Safety state could not be evaluated: {exception.Message}", "unavailable", now);
        }
    }

    private async Task<AdmissionStateSnapshot> GetStateAsync(CommandIntent intent, CancellationToken cancellationToken)
    {
        try
        {
            return await _stateSource.GetAsync(intent, cancellationToken).ConfigureAwait(false)
                ?? AdmissionStateSnapshot.Unavailable(_timeProvider.GetUtcNow());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return AdmissionStateSnapshot.Unavailable(_timeProvider.GetUtcNow());
        }
    }

    private CommandCheckStatus GetCapabilityStatus(
        CommandIntent intent,
        AdmissionStateSnapshot state,
        bool stateIsFresh)
    {
        if (!HasCanonicalCapabilityAndParameters(intent))
        {
            return CommandCheckStatus.Failed;
        }

        if (!stateIsFresh || state.PolicyStatus == CommandCheckStatus.Unknown ||
            string.IsNullOrWhiteSpace(state.ApprovedPolicyRevision))
        {
            return CommandCheckStatus.Unknown;
        }

        return state.PolicyStatus == CommandCheckStatus.Passed &&
               string.Equals(intent.PolicyRevision, state.ApprovedPolicyRevision, StringComparison.Ordinal)
            ? CommandCheckStatus.Passed
            : CommandCheckStatus.Failed;
    }

    private CommandCheckStatus GetFreshStatus(
        CommandCheckStatus status,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset now) =>
        IsFresh(occurredAtUtc, now) ? status : CommandCheckStatus.Unknown;

    private bool IsFresh(DateTimeOffset occurredAtUtc, DateTimeOffset now) =>
        occurredAtUtc != default && occurredAtUtc <= now && now - occurredAtUtc <= _freshness;

    private static bool HasCanonicalCapabilityAndParameters(CommandIntent intent)
    {
        if (!string.Equals(intent.DeviceId, "simulator", StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(intent.CanonicalParameters);
            var parameters = document.RootElement;
            if (parameters.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            return intent.Capability switch
            {
                "SetFlow" => HasOnly(parameters, "flowRate") &&
                             TryGetDecimal(parameters, "flowRate", out var flowRate) &&
                             IsStep(flowRate, 0m, 10m, 0.1m),
                "SetTemperature" => HasOnly(parameters, "temperature") &&
                                    TryGetDecimal(parameters, "temperature", out var temperature) &&
                                    IsStep(temperature, 20m, 80m, 1m),
                "SetMode" => HasOnly(parameters, "mode") &&
                             parameters.GetProperty("mode").ValueKind == JsonValueKind.String &&
                             parameters.GetProperty("mode").GetString() is "Isocratic" or "Gradient" or "Purge",
                "GetFlow" or "GetTemperature" or "GetMode" => !parameters.EnumerateObject().Any(),
                _ => false
            };
        }
        catch (JsonException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static bool HasOnly(JsonElement parameters, string expectedName) =>
        parameters.EnumerateObject().Count() == 1 && parameters.TryGetProperty(expectedName, out _);

    private static bool TryGetDecimal(JsonElement parameters, string propertyName, out decimal value)
    {
        value = default;
        return parameters.TryGetProperty(propertyName, out var property) &&
               property.ValueKind == JsonValueKind.Number &&
               decimal.TryParse(
                   property.GetRawText(),
                   NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                   CultureInfo.InvariantCulture,
                   out value);
    }

    private static bool IsStep(decimal value, decimal minimum, decimal maximum, decimal step) =>
        value >= minimum && value <= maximum && (value - minimum) % step == 0m;
}

/// <summary>Fresh, authoritative non-safety evidence needed for command admission.</summary>
public sealed record AdmissionStateSnapshot(
    CommandCheckStatus PolicyStatus,
    string? ApprovedPolicyRevision,
    CommandCheckStatus LeaseStatus,
    CommandCheckStatus ResourceStatus,
    CommandCheckStatus BudgetStatus,
    CommandCheckStatus TerminationStatus,
    CommandCheckStatus HealthStatus,
    DateTimeOffset OccurredAtUtc,
    string ContextRevision)
{
    public static AdmissionStateSnapshot Unavailable(DateTimeOffset occurredAtUtc) =>
        new(
            CommandCheckStatus.Unknown,
            null,
            CommandCheckStatus.Unknown,
            CommandCheckStatus.Unknown,
            CommandCheckStatus.Unknown,
            CommandCheckStatus.Unknown,
            CommandCheckStatus.Unknown,
            occurredAtUtc,
            "unavailable");
}

/// <summary>
/// Agent composition supplies durable approved-policy, lease, capacity, budget,
/// termination, and device-health evidence through this read-only port.
/// </summary>
public interface IAdmissionStateSource
{
    Task<AdmissionStateSnapshot?> GetAsync(CommandIntent intent, CancellationToken cancellationToken = default);
}

internal sealed class UnavailableAdmissionStateSource : IAdmissionStateSource
{
    public static UnavailableAdmissionStateSource Instance { get; } = new();

    public Task<AdmissionStateSnapshot?> GetAsync(CommandIntent intent, CancellationToken cancellationToken = default) =>
        Task.FromResult<AdmissionStateSnapshot?>(AdmissionStateSnapshot.Unavailable(DateTimeOffset.UtcNow));
}
