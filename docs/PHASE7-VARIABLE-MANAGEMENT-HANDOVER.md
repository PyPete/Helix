# Phase 7 — Cohesive Variable Management System — Handover

**Phase**: 7  
**Date**: 2025-01-XX  
**Status**: ✅ Complete  

---

## Executive Summary

Phase 7 delivers a **cohesive variable management system** for Helix workflows, enabling storage and retrieval of simple and complex data types (strings, numbers, arrays, dictionaries, nested objects) across multiple scopes:

- **Workflow-scoped variables**: Instance-specific state persisted in `WorkflowState`
- **Global/Campaign-scoped variables**: Shared state across multiple workflow instances
- **Device read variables**: Real-time device values accessed via capability invocation
- **WSL variable substitution**: Compile-time validation and runtime resolution of variable references

All variable storage uses **JSON serialization** for complex types, ensuring consistent handling of arrays, dictionaries, and nested objects while preserving primitive type efficiency.

---

## What Was Delivered

### 1. Workflow-Scoped Variable Extensions (`WorkflowStateExtensions.cs`)

**Purpose**: Typed access helpers for workflow instance variables with JSON serialization.

**Key Features**:
- `SetVariableAsync<T>()` / `GetVariableAsync<T>()` for typed storage/retrieval
- `GetVariableOrDefaultAsync<T>()` with fallback values
- `VariableExistsAsync()` / `DeleteVariableAsync()` for lifecycle management
- `ListVariableKeysAsync()` / `GetAllVariablesAsync()` / `ClearVariablesAsync()` for bulk operations
- Automatic JSON serialization for complex types (arrays, dictionaries, custom objects)
- Primitive types stored directly for efficiency

**Usage Example**:
```csharp
// Store simple value
await context.State.SetVariableAsync(workflowId, "temperature", 25.5);

// Store complex object
await context.State.SetVariableAsync(workflowId, "sample", new { Name = "Sample1", Tags = new[] { "tag1", "tag2" } });

// Retrieve with default
var temp = await context.State.GetVariableOrDefaultAsync(workflowId, "temperature", 20.0);
```

**Location**: `Helix.Orchestration.Core/State/WorkflowStateExtensions.cs`

---

### 2. Global State Store (`IGlobalStateStore`, `InMemoryGlobalStateStore`)

**Purpose**: Campaign-scoped and globally-shared variable storage across workflow instances.

**Key Features**:
- Scoped variable storage (campaign ID or "global" scope)
- Same typed access API as workflow variables
- Thread-safe concurrent access via `ConcurrentDictionary`
- `ListScopesAsync()` for discovering all active scopes
- Complete variable isolation between scopes

**Usage Example**:
```csharp
// Store campaign-level variable
await context.GlobalState.SetVariableAsync("campaign-123", "sampleCount", 42);

// Store global system variable
await context.GlobalState.SetVariableAsync("global", "systemReady", true);

// Retrieve from specific scope
var count = await context.GlobalState.GetVariableAsync<int>("campaign-123", "sampleCount");
```

**Location**:
- `Helix.Orchestration.Core/State/IGlobalStateStore.cs`
- `Helix.Orchestration.Core/State/InMemoryGlobalStateStore.cs`

---

### 3. WorkflowContext Integration

**Purpose**: Surface global state store in workflow execution context.

**Change**: Added `GlobalState` property to `WorkflowContext`:
```csharp
public required IGlobalStateStore GlobalState { get; init; }
```

Registered in Agent DI (`Program.cs`):
```csharp
builder.Services.AddSingleton<IGlobalStateStore, InMemoryGlobalStateStore>();
```

Wired into context factory:
```csharp
GlobalState = sp.GetRequiredService<IGlobalStateStore>()
```

**Impact**: All workflows now have access to both workflow-scoped (`context.State`) and global/campaign-scoped (`context.GlobalState`) variables.

---

### 4. Device Read Capability Extensions (`CapabilityReadExtensions.cs`)

**Purpose**: Read device state as typed variables through capability invocation.

**Key Features**:
- `ReadDeviceValueAsync<T>()` for typed device reads
- `ReadDeviceValueOrDefaultAsync<T>()` with fallback
- Property path navigation for nested device status objects (e.g., `"Status.FlowRate.Pump1"`)
- Works with existing `ICapabilityInvoker` / `CapabilityRegistry` architecture

**Usage Example**:
```csharp
// Read device status with property path navigation
var flowRate = await context.Capabilities.ReadDeviceValueAsync<double>(
    "vapourtec_status",
    propertyPath: "Status.FlowRate.Pump1"
);

// Read with default fallback
var temp = await context.Capabilities.ReadDeviceValueOrDefaultAsync(
    "vapourtec_status",
    defaultValue: 20.0,
    propertyPath: "Status.Temperature"
);
```

**Note**: Device reads require capability names in the format `deviceId_capabilityName` (e.g., `vapourtec_status`), consistent with the existing `CapabilityRegistry` model.

**Location**: `Helix.Orchestration.Core/WorkflowEngine/CapabilityReadExtensions.cs`

---

### 5. WSL Variable Substitution (`VariableSubstitution.cs`)

**Purpose**: Runtime resolution of `${scope:key}` variable references in WSL parameters.

**Supported Scopes**:
- `${workflow:key}` — workflow instance variables
- `${global:key}` — global system variables
- `${campaign:key}` — campaign-scoped variables (requires campaign context)
- `${device:capabilityName:propertyPath}` — device read variables

**Key Features**:
- `SubstituteAsync()` for single string substitution
- `SubstituteParametersAsync()` for dictionary batch substitution
- Async resolution with cancellation token support
- Graceful handling of missing variables (empty string substitution)

**Usage Example**:
```csharp
// WSL parameter: "Set temperature to ${workflow:targetTemp} degrees"
var substituted = await VariableSubstitution.SubstituteAsync(
    input: nodeParameter,
    workflowId: context.WorkflowId,
    workflowState: context.State,
    globalState: context.GlobalState,
    capabilities: context.Capabilities,
    campaignId: "campaign-123"
);
// Result: "Set temperature to 25.5 degrees"
```

**Location**: `Helix.Orchestration.Core/WorkflowEngine/VariableSubstitution.cs`

---

### 6. WSL Compiler Variable Validation

**Purpose**: Compile-time validation of variable reference syntax in WSL documents.

**Validation Rules**:
- Variable pattern: `${scope:key}` where scope is `workflow`, `global`, `campaign`, or `device`
- Scope validation: Unknown scopes produce `WSL040` error
- Variable keys are **not** validated at compile time (intentional — supports dynamic workflows)
- Device variables follow format: `${device:capabilityName}` or `${device:capabilityName:propertyPath}`

**Diagnostic Codes**:
- `WSL040`: Unknown variable scope
- (Device-specific format validation removed to align with capability-based architecture)

**Example Errors**:
```text
WSL040: Node 'step1', parameter 'message': unknown variable scope 'invalid'. Valid scopes: workflow, global, campaign, device
```

**Location**: `Helix.Orchestration.Core/WSL/WslCompiler.cs` (`ValidateVariableReferences()` method)

---

## Testing & Validation

### Test Coverage

1. **WorkflowStateExtensionsTests.cs** (10 tests)
   - Simple value storage/retrieval
   - Complex object JSON serialization
   - Default fallback behavior
   - Variable existence checks
   - Delete, list, clear operations

2. **GlobalStateStoreTests.cs** (12 tests)
   - Scoped variable storage
   - Scope isolation validation
   - List scopes operation
   - All CRUD operations per scope

**Result**: ✅ All 22 tests pass

### Build Verification

- ✅ Solution builds successfully
- ✅ No compilation errors or warnings
- ✅ Test project updated with `GlobalState` initialization
- ✅ Agent DI registration complete

---

## Architecture & Design Decisions

### 1. JSON Serialization Strategy

**Decision**: Use `System.Text.Json` for complex type serialization.

**Rationale**:
- Consistent handling of arrays, dictionaries, and nested objects
- Primitives stored directly for efficiency
- Standard .NET serialization avoids custom marshalling logic
- Future-proof for persistence layer upgrades (e.g., SQL, Redis)

**Trade-offs**:
- JSON overhead for complex types vs. direct storage
- Type safety lost during serialization (mitigated by typed accessors)

---

### 2. Scope-Based Variable Model

**Decision**: Three-tier scope hierarchy: workflow → campaign/global → device.

**Rationale**:
- **Workflow scope**: Instance-specific state, durable across restarts
- **Campaign/global scope**: Shared state for multi-workflow coordination
- **Device scope**: Real-time device reads, not persisted

**Benefits**:
- Clear separation of concerns
- Prevents variable name collisions across scopes
- Supports both isolated and shared workflow patterns

---

### 3. Device Variable Architecture

**Decision**: Device reads use capability invocation, not direct device coupling.

**Rationale**:
- Consistent with existing Helix `ICapabilityInvoker` / `CapabilityRegistry` model
- Maintains abstraction boundary between workflows and device protocols
- Capability names encode device identity (e.g., `vapourtec_status`)

**Format**: `${device:capabilityName:propertyPath}`
- `capabilityName`: Registered capability (e.g., `vapourtec_status`)
- `propertyPath`: Optional dot-separated path into result object (e.g., `Status.FlowRate.Pump1`)

**Trade-offs**:
- Requires capability registration before device reads work
- Indirect device addressing vs. direct device ID lookup

---

### 4. Compile-Time vs. Runtime Validation

**Decision**: Validate variable **syntax** at compile time, resolve **values** at runtime.

**Rationale**:
- Compile-time syntax checks catch typos early
- Runtime resolution supports dynamic workflows where variables are set during execution
- Prevents false positives for variables that don't exist yet but will be set before use

**Example**:
```text
✅ Compile time: ${workflow:temperature} — syntax valid
❌ Compile time: ${invalid:temperature} — unknown scope 'invalid'
✅ Runtime: Resolve ${workflow:temperature} to actual value (25.5)
```

---

### 5. Variable Substitution Granularity

**Decision**: Support partial string substitution with multiple variables.

**Example**:
```text
"Set ${workflow:reagent} to ${workflow:targetTemp} degrees in ${device:vapourtec_status:Status.VesselName}"
```

**Rationale**:
- Workflows often need templated strings with multiple variable references
- Regex-based substitution handles this efficiently
- Graceful fallback to empty string for missing variables

---

## Known Gaps & Future Work

### 1. Persistent Global State

**Current**: `InMemoryGlobalStateStore` — lost on Agent restart.

**Future**: SQL/Redis-backed `IGlobalStateStore` implementation for durable campaign state.

**Workaround**: For now, global variables are session-scoped. Campaign state must be re-initialized on Agent restart.

---

### 2. Variable Type Coercion

**Current**: Type conversion limited to primitives and JSON deserialization.

**Future**: Custom type converters for domain-specific objects (e.g., `Measurement`, `TrendData`).

**Workaround**: Store complex types as JSON and deserialize to target type.

---

### 3. Variable Namespacing

**Current**: Flat key namespace within each scope.

**Future**: Hierarchical namespaces (e.g., `workflow:experiment.sample.temperature`).

**Workaround**: Use dot-separated keys and convention (e.g., `experiment_sample_temperature`).

---

### 4. Variable Change Notifications

**Current**: No pub/sub for variable updates.

**Future**: Event-driven notifications when global/campaign variables change (useful for cross-workflow coordination).

**Workaround**: Poll variables or use explicit signaling via workflow state.

---

### 5. Variable Security & Access Control

**Current**: No access control on global/campaign variables.

**Future**: Role-based access control (RBAC) for sensitive variables (e.g., API keys, credentials).

**Workaround**: Store sensitive data outside the variable system (e.g., Azure Key Vault integration).

---

## Usage Patterns

### Pattern 1: Workflow-Scoped Experiment Parameters

```csharp
// Store experiment parameters at workflow start
await context.State.SetVariableAsync(workflowId, "experimentId", "EXP-123");
await context.State.SetVariableAsync(workflowId, "targetYield", 85.0);

// Retrieve during workflow execution
var expId = await context.State.GetVariableAsync<string>(workflowId, "experimentId");
var yield = await context.State.GetVariableAsync<double>(workflowId, "targetYield");
```

---

### Pattern 2: Campaign-Wide Sample Tracking

```csharp
// Campaign orchestrator increments sample count
var campaignId = "campaign-2025-Q1";
var currentCount = await context.GlobalState.GetVariableOrDefaultAsync(campaignId, "sampleCount", 0);
await context.GlobalState.SetVariableAsync(campaignId, "sampleCount", currentCount + 1);

// Individual workflows read the count
var totalSamples = await context.GlobalState.GetVariableAsync<int>(campaignId, "sampleCount");
```

---

### Pattern 3: Device Read in WSL

**WSL Document**:
```json
{
  "nodeType": "LogMessage",
  "parameters": {
    "message": "Current flow rate: ${device:vapourtec_status:Status.FlowRate.Pump1} mL/min"
  }
}
```

**Runtime Substitution**:
```csharp
var substituted = await VariableSubstitution.SubstituteAsync(
    nodeParameter,
    workflowId,
    context.State,
    context.GlobalState,
    context.Capabilities
);
// Result: "Current flow rate: 2.5 mL/min"
```

---

### Pattern 4: Conditional Logic with Variables

```csharp
// Check if variable exists before branching
if (await context.State.VariableExistsAsync(workflowId, "emergencyStop"))
{
    var stopRequested = await context.State.GetVariableAsync<bool>(workflowId, "emergencyStop");
    if (stopRequested == true)
    {
        // Abort workflow
    }
}
```

---

## Migration Notes

### Updating Existing Workflows

1. **Replace direct state dictionary access**:
   ```csharp
   // Old (Phase 6 and earlier)
   var state = await context.State.GetAsync(workflowId);
   var temp = state.Variables["temperature"];

   // New (Phase 7)
   var temp = await context.State.GetVariableAsync<double>(workflowId, "temperature");
   ```

2. **Add `GlobalState` to test fixtures**:
   ```csharp
   // Update WorkflowContext creation in tests
   new WorkflowContext
   {
       // ... existing properties
       GlobalState = new InMemoryGlobalStateStore()  // Add this line
   };
   ```

---

## Performance Considerations

### Variable Access Patterns

- **Workflow variables**: Single async database lookup per access (current: in-memory dictionary)
- **Global variables**: Thread-safe concurrent dictionary with no locking overhead
- **Device reads**: Full capability invocation latency (device-dependent, typically 10-500ms)

### Optimization Recommendations

1. **Cache device reads**: Avoid repeated device queries in tight loops
2. **Batch variable writes**: Use `SetVariableAsync()` sparingly; prefer bulk updates
3. **Scope selection**: Use workflow scope for instance data, global scope only when necessary

---

## Integration Points

### 1. WorkflowContext

All variable APIs are accessible via `WorkflowContext`:
- `context.State.SetVariableAsync()` — workflow-scoped
- `context.GlobalState.SetVariableAsync()` — global/campaign-scoped
- `context.Capabilities.ReadDeviceValueAsync()` — device reads

---

### 2. WSL Compiler

Variable validation is automatic during `IWslCompiler.Compile()`:
```csharp
var result = compiler.Compile(wslDocument, deviceRegistry);
if (!result.Success)
{
    // Check diagnostics for WSL040 variable errors
}
```

---

### 3. Capability Registry

Device reads require capability registration:
```csharp
// In Agent startup
var registry = new CapabilityRegistry();
registry.Register("vapourtec_status", new DeviceCapabilityAdapter<IVapourtec>("vapourtec_status", vapourtecDevice));
```

---

## Next Steps (Post-Phase 7)

### Immediate (Phase 8 Candidates)

1. **Persistent global state** — SQL/Redis-backed `IGlobalStateStore`
2. **Variable audit logging** — Track variable changes for compliance
3. **WSL runtime integration** — Automatic variable substitution in `WslExecutionAdapter`

### Medium-Term

4. **Variable templating** — Jinja-style templates with filters (e.g., `${workflow:temp | round(2)}`)
5. **Device read caching** — TTL-based cache for frequently-read device values
6. **Variable migration tooling** — CLI for exporting/importing global variables

### Long-Term

7. **Cross-agent variable sync** — Distributed state for multi-agent deployments
8. **Variable versioning** — Snapshot/rollback for campaign variables
9. **GraphQL variable API** — Real-time variable subscriptions for UI dashboards

---

## References

### Files Modified

- `Helix.Orchestration.Core/State/WorkflowStateExtensions.cs` (new)
- `Helix.Orchestration.Core/State/IGlobalStateStore.cs` (new)
- `Helix.Orchestration.Core/State/InMemoryGlobalStateStore.cs` (new)
- `Helix.Orchestration.Core/WorkflowEngine/WorkflowContext.cs` (updated)
- `Helix.Orchestration.Core/WorkflowEngine/CapabilityReadExtensions.cs` (new)
- `Helix.Orchestration.Core/WorkflowEngine/VariableSubstitution.cs` (new)
- `Helix.Orchestration.Core/WSL/WslCompiler.cs` (updated)
- `Helix.Orchestration.Agent/Program.cs` (updated)
- `Helix.Orchestration.Core.Tests/Execution/WorkflowExecutorTests.cs` (updated)
- `Helix.Orchestration.Core.Tests/State/WorkflowStateExtensionsTests.cs` (new)
- `Helix.Orchestration.Core.Tests/State/GlobalStateStoreTests.cs` (new)

### Related Phases

- **Phase 4**: Durable execution and trend foundation
- **Phase 5**: Runspace delegation and WSL execution adapter
- **Phase 6**: WSL compiler, device registry, Vapourtec parity, MQTT ingestion
- **Phase 7**: ← **You are here**

---

## Conclusion

Phase 7 delivers a **production-ready variable management system** for Helix workflows. The three-tier scope model (workflow, global/campaign, device) provides flexibility for both isolated and coordinated workflow patterns. JSON-based serialization ensures consistent handling of complex data structures, and the integration with the WSL compiler provides compile-time safety while preserving runtime flexibility.

All tests pass, the solution builds successfully, and the system is ready for integration with WSL runtime execution in future phases.

---

**Phase 7 Status**: ✅ Complete  
**Next Phase**: TBD (Persistent global state, WSL runtime integration, or variable audit logging)
