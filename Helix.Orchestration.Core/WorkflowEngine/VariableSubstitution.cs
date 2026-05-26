using System.Text.RegularExpressions;
using Helix.Orchestration.Core.Capabilities;
using Helix.Orchestration.Core.State;

namespace Helix.Orchestration.Core.WorkflowEngine;

/// <summary>
/// Provides runtime variable substitution for WSL parameter values.
/// Resolves ${scope:key} patterns at execution time with actual values from workflow state,
/// global state, or device reads.
/// </summary>
public static class VariableSubstitution
{
    private static readonly Regex VariablePattern = new(@"\$\{([a-zA-Z0-9_]+):([a-zA-Z0-9_\.\:]+)\}", RegexOptions.Compiled);

    /// <summary>
    /// Substitutes all variable references in the input string with runtime values.
    /// Supports workflow, global, campaign, and device scopes.
    /// </summary>
    /// <param name="input">The input string containing variable references.</param>
    /// <param name="workflowId">The current workflow ID for workflow-scoped variables.</param>
    /// <param name="workflowState">The workflow state store.</param>
    /// <param name="globalState">The global state store.</param>
    /// <param name="capabilities">The capability invoker for device reads.</param>
    /// <param name="campaignId">Optional campaign ID for campaign-scoped variables.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The input string with all variables substituted with runtime values.</returns>
    public static async Task<string> SubstituteAsync(
        string input,
        string workflowId,
        IWorkflowStateStore workflowState,
        IGlobalStateStore globalState,
        ICapabilityInvoker capabilities,
        string? campaignId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var matches = VariablePattern.Matches(input);
        if (matches.Count == 0)
            return input;

        var result = input;

        foreach (Match match in matches)
        {
            var scope = match.Groups[1].Value.ToLowerInvariant();
            var key = match.Groups[2].Value;
            var placeholder = match.Value;

            object? value = scope switch
            {
                "workflow" => await workflowState.GetVariableAsync<object>(workflowId, key, cancellationToken),
                "global" => await globalState.GetVariableAsync<object>("global", key, cancellationToken),
                "campaign" when !string.IsNullOrWhiteSpace(campaignId) =>
                    await globalState.GetVariableAsync<object>(campaignId, key, cancellationToken),
                "device" => await ResolveDeviceVariable(capabilities, key, cancellationToken),
                _ => null
            };

            // Replace placeholder with value (or empty string if null)
            result = result.Replace(placeholder, value?.ToString() ?? string.Empty);
        }

        return result;
    }

    /// <summary>
    /// Substitutes variables in a dictionary of parameters.
    /// Returns a new dictionary with substituted values.
    /// </summary>
    public static async Task<Dictionary<string, object?>> SubstituteParametersAsync(
        Dictionary<string, object?> parameters,
        string workflowId,
        IWorkflowStateStore workflowState,
        IGlobalStateStore globalState,
        ICapabilityInvoker capabilities,
        string? campaignId = null,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, object?>();

        foreach (var param in parameters)
        {
            if (param.Value is string stringValue)
            {
                result[param.Key] = await SubstituteAsync(
                    stringValue,
                    workflowId,
                    workflowState,
                    globalState,
                    capabilities,
                    campaignId,
                    cancellationToken);
            }
            else
            {
                result[param.Key] = param.Value;
            }
        }

        return result;
    }

    /// <summary>
    /// Resolves a device variable reference (format: capabilityName or capabilityName:propertyPath).
    /// Example: vapourtec_status:Status.FlowRate.Pump1
    /// Capability names should follow the convention: deviceId_capabilityName (e.g., vapourtec_status).
    /// </summary>
    private static async Task<object?> ResolveDeviceVariable(
        ICapabilityInvoker capabilities,
        string key,
        CancellationToken cancellationToken)
    {
        var parts = key.Split(':');
        var capabilityName = parts[0];
        var propertyPath = parts.Length > 1 ? string.Join('.', parts.Skip(1)) : null;

        return await capabilities.ReadDeviceValueAsync<object>(
            capabilityName,
            propertyPath,
            null,
            cancellationToken);
    }
}
