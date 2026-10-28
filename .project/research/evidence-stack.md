# Evidence — stack

Dimension: stack
Questions assigned: Which existing Agent durable-workflow primitives can safely implement activity execution, external-result waits, idempotency, and restart recovery without creating a second orchestration engine?

## Finding: Cleipnir provides existing primitives for durable activities, message waits, and effect boundaries

- **Claim**: Cleipnir Resilient Functions documents PostgreSQL-backed registered actions, resumption of unfinished invocations after restart, message-based waits, and captured effects with an `AtMostOnce` resiliency setting; its invocation API accepts a caller-supplied flow-instance ID.
- **Source**: [Cleipnir.ResilientFunctions NuGet README](https://www.nuget.org/packages/Cleipnir.ResilientFunctions) and [Cleipnir.ResilientFunctions.PostgreSQL NuGet README](https://www.nuget.org/packages/Cleipnir.ResilientFunctions.PostgreSQL) (opened live; PostgreSQL store setup, `RegisterAction`, `workflow.Messages`, `Effect.Capture`, and restart behavior are shown).
- **Confidence**: high
- **Why it matters here**: These primitives fit the settled .NET Agent/PostgreSQL/Cleipnir baseline and can express activities, an external-result pause, retry boundaries, and restart recovery without adding a second orchestration substrate.

## Finding: Helix has Cleipnir-backed execution components, but runtime wiring and runspace recovery are not yet equivalent to end-to-end durability

- **Claim**: Agent startup initializes a PostgreSQL `PostgreSqlFunctionStore`, registers `FunctionsRegistry` and `WorkflowExecutor`, and exposes `WslExecutionAdapter`, which constructs a `DurableRunspace`; however, the separately registered `IRunspaceManager` factory constructs `InMemoryRunspace` and executes its BFS traversal, while the manager's tracking/status registry is in-process. The inspected sources do not establish that all production submissions use the adapter or that runspace metadata is reconstructed after restart.
- **Source**: `Helix.Orchestration.Agent/Program.cs:70-114,247-248`; `Helix.Orchestration.Agent/Execution/WslExecutionAdapter.cs:39-60`; `Helix.Orchestration.Agent/Runspaces/DurableRunspace.cs:11-16,41-63`; `Helix.Orchestration.Core/Runspace/RunspaceManager.cs:9-12,29-51`; `Helix.Orchestration.Core/Execution/WorkflowExecutor.cs:31-79` (opened and checked).
- **Confidence**: high
- **Why it matters here**: Brownfield work is an integration and recovery-validation task around existing Agent components, not a clean-sheet engine replacement; durable function records alone do not prove that a campaign or operator-visible runspace resumes correctly.

## Finding: Effect capture and stable invocation IDs help prevent duplicate effects but do not guarantee exactly-once physical execution

- **Claim**: Cleipnir's sample captures a transaction identifier and wraps work in `Effect.Capture` with `ResiliencyLevel.AtMostOnce`, while its documented invocation retries incomplete flows; neither the sample nor inspected Helix code establishes an atomic transaction between Cleipnir persistence and an instrument's physical action. The current `DurableRunspace` generates a new random runspace ID at construction, so callers must supply/persist a stable logical activity identity rather than assume that generated ID survives reconstruction.
- **Source**: [Cleipnir.ResilientFunctions NuGet README](https://www.nuget.org/packages/Cleipnir.ResilientFunctions) (opened live; sample and invocation behavior); `Helix.Orchestration.Agent/Runspaces/DurableRunspace.cs:21,41-44`; `Helix.Orchestration.Core/Execution/WorkflowExecutor.cs:35-58`; `Helix.Orchestration.Agent/Execution/WslExecutionAdapter.cs:56-60`; `.project/CHARTER.md:23,32-35` (opened).
- **Confidence**: high
- **Why it matters here**: Workflow retries must not bypass the Agent's capability, lease, validation, safety, and audit path; physical actions need stable deduplication keys plus a defined reconciliation strategy for uncertain outcomes, not an exactly-once assumption.

## Finding: Cleipnir message waits can pause for an external result, but Helix result correlation and ingress guarantees remain unresolved

- **Claim**: Cleipnir's public README demonstrates a registered action awaiting typed messages through `Workflow.Messages`, but the inspected sources do not establish Helix-specific durable message ingress, duplicate or out-of-order result handling, result-revision identity, or correlation across Agent restart.
- **Source**: [Cleipnir.ResilientFunctions NuGet README](https://www.nuget.org/packages/Cleipnir.ResilientFunctions) (opened live; typed `workflow.Messages` example); `.project/CHARTER.md:13-15,43-46`; `Helix.Orchestration.Agent/Program.cs:70-87` (opened).
- **Confidence**: medium
- **Why it matters here**: An external analytical result should first be persisted as provenance-bearing evidence and correlated to the requested activity/revision; a generic wait primitive alone does not establish safe resumption semantics.

## Finding: In-memory leases and auxiliary state remain independent safety and recovery gaps

- **Claim**: Agent startup registers `InMemoryDeviceLeasingService`, `InMemoryGlobalStateStore`, and `InMemoryTrendProvider` alongside the PostgreSQL function store; WSL execution acquires exclusive leases before starting its durable runspace, but the shown lease/state registrations do not provide durable lease reconstruction or reacquisition on restart.
- **Source**: `Helix.Orchestration.Agent/Program.cs:62-65,76-87`; `Helix.Orchestration.Agent/Execution/WslExecutionAdapter.cs:39-60`; `.project/research/evidence-codebase.md:23-28` (opened).
- **Confidence**: high
- **Why it matters here**: Recovery must not infer physical ownership from a workflow record: the Agent remains the sole execution, safety, capability, lease, and recovery authority, and must reacquire and revalidate before any later device command.

## Finding: Temporal .NET covers durable workflows and asynchronous external completion but carries a larger migration footprint

- **Claim**: Temporal's official .NET documentation provides workflow/activity workers, durable workflow message handling and wait conditions, and asynchronous activity completion by task token or workflow/activity identity; its .NET developer guide assumes a Temporal Service, which would add a service/history and worker/client deployment model beyond the existing Agent/PostgreSQL/Cleipnir integration.
- **Source**: [Temporal .NET developer guide](https://docs.temporal.io/develop/dotnet) (opened live; service setup and worker/workflow/activity model); [Workflow basics](https://docs.temporal.io/develop/dotnet/workflows/basics); [Workflow message passing](https://docs.temporal.io/develop/dotnet/workflows/message-passing); [Asynchronous Activity Completion](https://docs.temporal.io/develop/dotnet/activities/asynchronous-activity) (official pages opened live); `.project/CHARTER.md:15,32-35`.
- **Confidence**: high
- **Why it matters here**: Temporal is a technically capable alternative for long waits, but replacing/bridging the current .NET Agent durable path entails service operations, worker/client wiring, workflow porting, and new authority-boundary review; all device execution would still have to remain inside Helix Agent capability and safety controls.

## Finding: Retaining the BFS runner is lowest migration effort but does not supply evidenced restart recovery

- **Claim**: The registered Agent runspace factory performs a BFS walk in an `InMemoryRunspace`, while Helix separately contains `DurableRunspace` and `WorkflowExecutor` components backed by Cleipnir; the inspected BFS runner has no shown durable checkpoint/restart mechanism. A bespoke PostgreSQL workflow state machine would add a separate orchestration implementation rather than close the existing Cleipnir integration gap.
- **Source**: `Helix.Orchestration.Agent/Program.cs:89-145`; `Helix.Orchestration.Core/Execution/IWorkflowExecutor.cs:3-17`; `Helix.Orchestration.Core/Execution/WorkflowExecutor.cs:8-18`; `Helix.Orchestration.Agent/Runspaces/DurableRunspace.cs:8-16` (opened).
- **Confidence**: high
- **Why it matters here**: Keeping the BFS path is the smallest immediate change but does not demonstrate the charter's restart-with-pending-result scenario; custom orchestration would duplicate responsibilities the settled durable-execution baseline already assigns to Cleipnir and the Agent.

## Assigned questions — answers

- Which existing Agent durable-workflow primitives can safely implement activity execution, external-result waits, idempotency, and restart recovery without creating a second orchestration engine? → Cleipnir's existing `FunctionsRegistry`/`RegisterAction` with `PostgreSqlFunctionStore` is the closest-fit activity/restart primitive; a flow can wait using `Workflow.Messages`, and `Effect.Capture` with a stable caller-supplied flow-instance/activity identity can define retry/idempotency boundaries. Recovery still needs integration validation: current runspace manager tracking/status, leases, global state, and trends are in memory, and the separately wired BFS factory uses `InMemoryRunspace`; the adapter's `DurableRunspace` also receives a freshly generated runspace ID unless the logical identity is persisted separately. Physical commands must continue through Agent capabilities and revalidate/reacquire safety and leases. Cleipnir's generic message example does not prove Helix result-revision/correlation or physical exactly-once guarantees. Alternatives weighed: (1) extend and validate Cleipnir/Agent wiring—lowest migration cost against the settled .NET/PostgreSQL/Cleipnir baseline; (2) Temporal—capable wait/activity model, but adds a service/history and requires worker/client integration and workflow porting; (3) retain BFS or create bespoke PostgreSQL orchestration—lowest immediate adaptation or superficially familiar storage, but no evidenced recovery in the former and a second engine in the latter. No option is selected here. Sources: findings above.

## Dead ends

- `https://github.com/cleipnir/ResilientFunctions` and its `master/Documentation` path — returned 404 during the earlier check; the package README was subsequently reopened live at NuGet and the current package links its publisher repository at `https://github.com/stidsborg/Cleipnir.ResilientFunctions`.
- Web search integration for Cleipnir documentation — request failed with HTTP 403 during the earlier check; used live reads of the official NuGet package pages instead.
- Exact guarantees for durable external-message deduplication/correlation and atomicity between Cleipnir effect records and physical instrument actions — no reliable guarantee was found in the sources opened; preserve as unresolved rather than assume exactly-once physical execution.
