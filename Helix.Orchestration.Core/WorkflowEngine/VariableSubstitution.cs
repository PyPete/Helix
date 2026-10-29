using System.Text.RegularExpressions;
using Helix.Orchestration.Core.Capabilities;
using Helix.Orchestration.Core.Execution;
using Helix.Orchestration.Core.State;

namespace Helix.Orchestration.Core.WorkflowEngine;

/// <summary>One explicit failure or unknown encountered while resolving a variable.</summary>
public sealed record VariableResolutionDiagnostic(
    string Placeholder,
    ObservationReadStatus Status,
    string Code,
    string Reason);

/// <summary>Structured variable substitution result; unresolved values are never replaced by defaults.</summary>
public sealed record VariableSubstitutionResult(
    string? Value,
    IReadOnlyList<VariableResolutionDiagnostic> Diagnostics)
{
    public bool IsResolved => Diagnostics.Count == 0;
}

/// <summary>Raised by compatibility methods when a caller has not opted into structured diagnostics.</summary>
public sealed class VariableResolutionException(IReadOnlyList<VariableResolutionDiagnostic> diagnostics)
    : InvalidOperationException("One or more workflow variables could not be resolved.")
{
    public IReadOnlyList<VariableResolutionDiagnostic> Diagnostics { get; } = diagnostics;
}

/// <summary>Resolves WSL variables while keeping simulator reads observation-only and fail closed.</summary>
public static class VariableSubstitution
{
    private static readonly Regex VariablePattern = new(@"\$\{([a-zA-Z0-9_]+):([a-zA-Z0-9_\.\:]+)\}", RegexOptions.Compiled);

    /// <summary>
    /// Resolves all variables and preserves explicit Unknown/Invalid diagnostics.
    /// Device variables are exactly <c>${device:GetFlow}</c>,
    /// <c>${device:GetTemperature}</c>, or <c>${device:GetMode}</c>.
    /// </summary>
    public static async Task<VariableSubstitutionResult> TrySubstituteAsync(
        string input,
        string workflowId,
        IWorkflowStateStore workflowState,
        IGlobalStateStore globalState,
        ISimulatorObservation observations,
        string? campaignId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowId);
        ArgumentNullException.ThrowIfNull(workflowState);
        ArgumentNullException.ThrowIfNull(globalState);
        ArgumentNullException.ThrowIfNull(observations);

        var matches = VariablePattern.Matches(input);
        if (matches.Count == 0)
            return new VariableSubstitutionResult(input, []);

        var result = input;
        var diagnostics = new List<VariableResolutionDiagnostic>();
        foreach (Match match in matches)
        {
            var scope = match.Groups[1].Value.ToLowerInvariant();
            var key = match.Groups[2].Value;
            var resolution = await ResolveAsync(
                scope,
                key,
                match.Value,
                workflowId,
                workflowState,
                globalState,
                observations,
                campaignId,
                cancellationToken).ConfigureAwait(false);

            if (resolution.Diagnostic is not null)
            {
                diagnostics.Add(resolution.Diagnostic);
                continue;
            }

            result = result.Replace(match.Value, resolution.Value!, StringComparison.Ordinal);
        }

        return diagnostics.Count == 0
            ? new VariableSubstitutionResult(result, diagnostics)
            : new VariableSubstitutionResult(null, diagnostics);
    }

    /// <summary>
    /// Compatibility overload. It deliberately throws for Unknown/Invalid input
    /// rather than silently substituting an empty string.
    /// </summary>
    public static async Task<string> SubstituteAsync(
        string input,
        string workflowId,
        IWorkflowStateStore workflowState,
        IGlobalStateStore globalState,
        ISimulatorObservation observations,
        string? campaignId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await TrySubstituteAsync(
            input, workflowId, workflowState, globalState, observations, campaignId, cancellationToken).ConfigureAwait(false);
        if (!result.IsResolved)
            throw new VariableResolutionException(result.Diagnostics);

        return result.Value!;
    }

    /// <summary>
    /// Retained only so older callers fail closed while they migrate to the
    /// observation port. Arbitrary capability invocation is not a valid device
    /// variable read because an invoker can contain mutating capabilities.
    /// </summary>
    public static Task<string> SubstituteAsync(
        string input,
        string workflowId,
        IWorkflowStateStore workflowState,
        IGlobalStateStore globalState,
        ICapabilityInvoker capabilities,
        string? campaignId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        return SubstituteAsync(
            input,
            workflowId,
            workflowState,
            globalState,
            UnavailableObservation.Instance,
            campaignId,
            cancellationToken);
    }

    /// <summary>Structured counterpart for parameter-dictionary substitution.</summary>
    public static async Task<(Dictionary<string, object?> Parameters, IReadOnlyList<VariableResolutionDiagnostic> Diagnostics)>
        TrySubstituteParametersAsync(
            Dictionary<string, object?> parameters,
            string workflowId,
            IWorkflowStateStore workflowState,
            IGlobalStateStore globalState,
            ISimulatorObservation observations,
            string? campaignId = null,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        var result = new Dictionary<string, object?>(parameters.Count, StringComparer.Ordinal);
        var diagnostics = new List<VariableResolutionDiagnostic>();

        foreach (var parameter in parameters)
        {
            if (parameter.Value is not string value)
            {
                result[parameter.Key] = parameter.Value;
                continue;
            }

            var substitution = await TrySubstituteAsync(
                value, workflowId, workflowState, globalState, observations, campaignId, cancellationToken).ConfigureAwait(false);
            diagnostics.AddRange(substitution.Diagnostics);
            result[parameter.Key] = substitution.IsResolved ? substitution.Value : parameter.Value;
        }

        return (result, diagnostics);
    }

    /// <summary>Compatibility dictionary overload that does not hide unresolved variables.</summary>
    public static async Task<Dictionary<string, object?>> SubstituteParametersAsync(
        Dictionary<string, object?> parameters,
        string workflowId,
        IWorkflowStateStore workflowState,
        IGlobalStateStore globalState,
        ISimulatorObservation observations,
        string? campaignId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await TrySubstituteParametersAsync(
            parameters, workflowId, workflowState, globalState, observations, campaignId, cancellationToken).ConfigureAwait(false);
        if (result.Diagnostics.Count != 0)
            throw new VariableResolutionException(result.Diagnostics);

        return result.Parameters;
    }

    /// <summary>Fail-closed compatibility overload; see <see cref="SubstituteAsync(string,string,IWorkflowStateStore,IGlobalStateStore,ICapabilityInvoker,string?,CancellationToken)"/>.</summary>
    public static Task<Dictionary<string, object?>> SubstituteParametersAsync(
        Dictionary<string, object?> parameters,
        string workflowId,
        IWorkflowStateStore workflowState,
        IGlobalStateStore globalState,
        ICapabilityInvoker capabilities,
        string? campaignId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        return SubstituteParametersAsync(
            parameters,
            workflowId,
            workflowState,
            globalState,
            UnavailableObservation.Instance,
            campaignId,
            cancellationToken);
    }

    private static async Task<(string? Value, VariableResolutionDiagnostic? Diagnostic)> ResolveAsync(
        string scope,
        string key,
        string placeholder,
        string workflowId,
        IWorkflowStateStore workflowState,
        IGlobalStateStore globalState,
        ISimulatorObservation observations,
        string? campaignId,
        CancellationToken cancellationToken)
    {
        if (scope == "device")
        {
            var observation = await observations.ReadObservationAsync(key, cancellationToken).ConfigureAwait(false);
            return observation.Status == ObservationReadStatus.Known
                ? (CapabilityReadExtensions.FormatCanonicalValue(observation.Value!), null)
                : (null, new VariableResolutionDiagnostic(placeholder, observation.Status, observation.Code, observation.Reason));
        }

        object? value = scope switch
        {
            "workflow" => await workflowState.GetVariableAsync<object>(workflowId, key, cancellationToken).ConfigureAwait(false),
            "global" => await globalState.GetVariableAsync<object>("global", key, cancellationToken).ConfigureAwait(false),
            "campaign" when !string.IsNullOrWhiteSpace(campaignId) =>
                await globalState.GetVariableAsync<object>(campaignId, key, cancellationToken).ConfigureAwait(false),
            "campaign" => null,
            _ => null
        };

        if (scope is not ("workflow" or "global" or "campaign"))
        {
            return (null, new VariableResolutionDiagnostic(
                placeholder,
                ObservationReadStatus.Invalid,
                "variable.unsupported-scope",
                $"'{scope}' is not a supported variable scope."));
        }

        return value is null
            ? (null, new VariableResolutionDiagnostic(
                placeholder,
                ObservationReadStatus.Unknown,
                "variable.unknown",
                $"'{placeholder}' is not available in the current state."))
            : (Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture), null);
    }

    /// <summary>
    /// Prevents compatibility callers from falling back to a mutation-capable
    /// registry. Their non-device substitutions remain available, while device
    /// variables are reported as Unknown until the caller supplies observations.
    /// </summary>
    private sealed class UnavailableObservation : ISimulatorObservation
    {
        public static UnavailableObservation Instance { get; } = new();

        public Task<SimulatorObservation?> GetAsync(
            string deviceId,
            string stateKey,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<SimulatorObservation?>(null);
    }
}
