using System.Globalization;
using Helix.Orchestration.Core.Execution;

namespace Helix.Orchestration.Core.WorkflowEngine;

/// <summary>Outcome of a read-only canonical simulator observation.</summary>
public enum ObservationReadStatus
{
    Known,
    Unknown,
    Invalid
}

/// <summary>
/// A structured observation result. Invalid means the requested vocabulary was
/// rejected; Unknown means a permitted observation could not be trusted.
/// </summary>
public sealed record ObservationReadResult<T>(
    ObservationReadStatus Status,
    T? Value,
    string Code,
    string Reason)
{
    public static ObservationReadResult<T> Known(T value) =>
        new(ObservationReadStatus.Known, value, "observation.known", "Observation is readable and current.");

    public static ObservationReadResult<T> Unknown(string code, string reason) =>
        new(ObservationReadStatus.Unknown, default, code, reason);

    public static ObservationReadResult<T> Invalid(string code, string reason) =>
        new(ObservationReadStatus.Invalid, default, code, reason);
}

/// <summary>
/// Resolves the deliberately small read-only simulator vocabulary used by WSL
/// variables. This type never invokes <c>ICapabilityInvoker</c>: a variable
/// read must not be able to call a setter or an arbitrary registered capability.
/// </summary>
public static class CapabilityReadExtensions
{
    private static readonly TimeSpan ObservationFreshness = TimeSpan.FromSeconds(2);

    /// <summary>Returns whether <paramref name="capability"/> is one of the three canonical getters.</summary>
    public static bool IsCanonicalObservation(string capability) =>
        capability is "GetFlow" or "GetTemperature" or "GetMode";

    /// <summary>
    /// Maps a canonical getter to its persistent simulator state key and unit.
    /// Namespaces are intentionally not accepted here; device bridges own any
    /// transport-specific namespace normalization.
    /// </summary>
    public static bool TryGetObservationMetadata(
        string capability,
        out string stateKey,
        out string unit)
    {
        switch (capability)
        {
            case "GetFlow":
                stateKey = "flowRate";
                unit = "mL/min";
                return true;
            case "GetTemperature":
                stateKey = "temperature";
                unit = "°C";
                return true;
            case "GetMode":
                stateKey = "mode";
                unit = "enum";
                return true;
            default:
                stateKey = string.Empty;
                unit = string.Empty;
                return false;
        }
    }

    /// <summary>
    /// Reads a canonical getter through the read-only observation port. Missing,
    /// stale, malformed, or unreadable data is Unknown, never a safe-looking
    /// numeric or empty-string default.
    /// </summary>
    public static async Task<ObservationReadResult<object>> ReadObservationAsync(
        this ISimulatorObservation observations,
        string capability,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(observations);

        if (!TryGetObservationMetadata(capability, out var stateKey, out var unit))
        {
            return ObservationReadResult<object>.Invalid(
                "observation.unsupported-capability",
                $"'{capability}' is not a canonical read-only observation. Reauthor with GetFlow, GetTemperature, or GetMode.");
        }

        SimulatorObservation? observation;
        try
        {
            observation = await observations.GetAsync("simulator", stateKey, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return ObservationReadResult<object>.Unknown(
                "observation.unreadable",
                $"'{capability}' observation could not be read: {exception.GetType().Name}.");
        }

        if (observation is null)
        {
            return ObservationReadResult<object>.Unknown(
                "observation.missing",
                $"'{capability}' has no independent simulator observation.");
        }

        var now = DateTimeOffset.UtcNow;
        if (observation.ObservedAtUtc == default || observation.ObservedAtUtc > now ||
            now - observation.ObservedAtUtc > ObservationFreshness)
        {
            return ObservationReadResult<object>.Unknown(
                "observation.stale",
                $"'{capability}' observation is stale or has an invalid timestamp.");
        }

        if (!string.Equals(observation.DeviceId, "simulator", StringComparison.Ordinal) ||
            !string.Equals(observation.StateKey, stateKey, StringComparison.Ordinal) ||
            !string.Equals(observation.Unit, unit, StringComparison.Ordinal))
        {
            return ObservationReadResult<object>.Unknown(
                "observation.mismatch",
                $"'{capability}' observation did not match its canonical simulator state key or unit.");
        }

        if (capability == "GetMode")
        {
            if (observation.CanonicalValue is not ("Isocratic" or "Gradient" or "Purge"))
            {
                return ObservationReadResult<object>.Unknown(
                    "observation.invalid-value",
                    "GetMode observation is not a canonical mode enum value.");
            }

            return ObservationReadResult<object>.Known(observation.CanonicalValue);
        }

        if (!decimal.TryParse(
                observation.CanonicalValue,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var numericValue))
        {
            return ObservationReadResult<object>.Unknown(
                "observation.invalid-value",
                $"'{capability}' observation is not a finite canonical decimal.");
        }

        return ObservationReadResult<object>.Known(numericValue);
    }

    /// <summary>Formats an already validated canonical observation for substitution.</summary>
    public static string FormatCanonicalValue(object value) => value switch
    {
        decimal decimalValue => decimalValue.ToString("0.#############################", CultureInfo.InvariantCulture),
        string enumValue when enumValue is "Isocratic" or "Gradient" or "Purge" => enumValue,
        _ => throw new ArgumentException("Observation value was not canonical.", nameof(value))
    };
}
