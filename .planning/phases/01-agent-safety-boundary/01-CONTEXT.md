# Phase 1: Agent Safety Boundary - Context

**Gathered:** 2026-10-29
**Status:** Ready for planning

<domain>
## Phase Boundary

Make the local Helix Agent the sole, safety-gated authority for simulated-device commands. This phase establishes authenticated desktop-only control access, repeatable command admission, recoverable handling of uncertain outcomes, and an attributable command timeline. It does not add apparatus modeling, samples, analytical results, autonomous selection, or real-device execution.

</domain>

<decisions>
## Implementation Decisions

### Local control access
- **D-01:** The simulated-device MVP uses a desktop-only loopback path to the local Agent; do not accept lab-network clients or require Site Server proxying. — **Reversibility:** costly — widening the listener and deployment boundary later requires an explicit network security model and client contract.
- **D-02:** The desktop supplies a validated desktop-session identity for the scientist on every Agent request; record that identity in the audit record. — **Reversibility:** costly — identity becomes part of persisted command/evidence records and API middleware.
- **D-03:** Every Agent API, including reads, requires the validated desktop-session identity. Requests without it fail closed and the attempt is audited. — **Reversibility:** costly — changing this later alters the security contract across all Agent routes.

### Admission outcomes
- **D-04:** Ordinary pre-dispatch validation failures (for example missing capability or out-of-bound parameter) reject the command immediately with an operator-facing explanation plus structured failed-check details.
- **D-05:** Unknown or unsafe safety state, lease ownership, or device health places the campaign on hold and requires scientist review before continuation.
- **D-06:** Run policy and safety admission checks at proposal/compile time for feedback and revalidate immediately before every Agent dispatch. — **Reversibility:** costly — safety semantics must be consistent across compilation and runtime execution.

### Restart uncertainty
- **D-07:** When intent is durable but the outcome is unknown after Agent restart, enter `Held pending reconciliation`; never blindly replay the command.
- **D-08:** For simulated devices, automatically query simulator state during reconciliation. If observed state matches the expected outcome, resume automatically and append a prominent recovery audit event.
- **D-09:** If simulator state cannot be read or does not match the expected outcome, retain the hold and require scientist review; do not retry automatically.

### Command evidence
- **D-10:** Present command evidence as a chronological campaign timeline covering proposals, admission decisions, dispatches, observations, recovery, holds, and aborts.
- **D-11:** Show outcome-first event summaries by default: action, device/capability, timestamp, actor, and outcome. Provide expandable parameters, validation checks, identifiers, and observations.
- **D-12:** Original command facts are immutable. Operator context is an attributable append-only annotation or later reconciliation/decision event. — **Reversibility:** costly — persisted evidence/audit semantics rely on append-only history.
- **D-13:** Visually emphasize rejected, held, aborted, safety-related, and reconciliation events; ordinary successful commands remain readable but quiet.

### the agent's Discretion
- Choose concrete desktop-session authentication/transport mechanics that satisfy the loopback-only, fail-closed, all-routes requirement and fit existing Helix patterns.
- Choose exact timeline UI composition and structured diagnostic schema, while preserving the outcome-first and append-only decisions above.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Product scope and requirements
- `.planning/PROJECT.md` — Agent-only execution authority, local-first and simulated-device constraints.
- `.planning/REQUIREMENTS.md` — Phase 1 requirements `SAFE-01` through `SAFE-04`.
- `.planning/ROADMAP.md` §Phase 1 — fixed phase goal, requirements, and observable success criteria.
- `.planning/STATE.md` — active-phase context and known Phase 1 concerns.

### Research and codebase evidence
- `.planning/research/SUMMARY.md` §§Executive Summary, Architecture Approach, Critical Pitfalls — safety authority, reconciliation, audit, and command/effect-ledger guidance.
- `.planning/codebase/ARCHITECTURE.md` — Agent, `CapabilityRegistry`, runspace, proxy, safety, and leasing integration boundaries.
- `.planning/codebase/CONCERNS.md` §§Security Considerations, Fragile Areas, Scaling Limits — unauthenticated Agent routes, safety observability, in-memory coordination, and recorder limitations.
- `.planning/codebase/TESTING.md` — C# unit/integration testing, substitutes/fakes, and existing test-project conventions.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Helix.Orchestration.Core/Capabilities/CapabilityRegistry.cs`: existing logical capability dispatch seam; extend rather than allowing direct transport calls.
- `Helix.Orchestration.Agent/Runspaces/DurableRunspace.cs`: existing runspace state-machine basis for pause, resume, abort, and observable execution state.
- `Helix.Orchestration.Simulation/SimulationServiceExtensions.cs`: simulated-device registration and initial MVP execution target.
- `tests/Helix.Orchestration.Core.Tests/Safety/SafetyEngineTests.cs`: existing safety-engine test coverage to extend with audit/recovery behavior.
- `tests/Helix.Orchestration.SiteServer.Tests/HelixWebApplicationFactory.cs`: established host-level test seam using substituted dependencies.

### Established Patterns
- Cross-tier control uses `IAgentProxy` and protobuf contracts; Site Server code must not invoke Agent implementation types directly.
- Core workflows use logical capabilities through `ICapabilityInvoker`/`CapabilityRegistry`, not serial/TCP/OPC APIs.
- C# tests use xUnit, NSubstitute/Moq, asynchronous tests, and focused fake adapters for behavior that must be observed.
- Current Agent route mapping lacks authentication/authorization; Phase 1 must close this existing bypass rather than treating CORS as access control.

### Integration Points
- `Helix.Orchestration.Agent/Program.cs` and Agent API endpoint modules are the access-control composition and routing boundary.
- `Helix.Orchestration.Core/Safety/SafetyEngine.cs` is the existing safety-state seam; its emergency-stop observability currently needs structured/auditable treatment.
- Existing lease/state abstractions and `WorkflowExecutor` form the reconciliation and pre-dispatch admission integration points.

</code_context>

<specifics>
## Specific Ideas

- The command record should retain intent, admission result, dispatch, observed outcome, correlation, actor, and any recovery/reconciliation disposition.
- Automatic reconciliation is limited to simulator state matching; nonmatching or unreadable state remains held for the scientist.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 1-Agent Safety Boundary*
*Context gathered: 2026-10-29*
