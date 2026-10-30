# Phase 2: Scientific Setup Foundation - Context

**Gathered:** 2026-10-30
**Status:** Ready for planning

<domain>
## Phase Boundary

Deliver the revisioned scientific setup that future campaigns can identify and freeze: apparatus topology and device bindings, independently versioned procedures and experiment plans, and confirmed sample facts with custody and lineage. A planned collection step is not itself proof that a sample was collected. This phase establishes the setup and sample records; campaign approval, durable campaign execution, analytical evidence, adaptive selection, and real-device execution remain in their roadmap phases or out of scope.

</domain>

<decisions>
## Implementation Decisions

### Revision lifecycle
- **D-01:** Each apparatus, procedure, and plan has one active editable draft based on its latest published revision. Edits remain draft changes until the scientist explicitly publishes a named, immutable revision.
- **D-02:** When a campaign is assembled, default its apparatus, procedure, and plan references to the latest published revisions and show those exact versions. Pin them at assembly; later publications must not silently change an assembled campaign. The approved campaign policy is also frozen for execution under the existing project requirements.

### Procedure and experiment plan
- **D-03:** A plan instantiates a specific published procedure revision while retaining its own independent revision. It records experiment-specific parameter values and units so a later campaign can identify the exact intended setup.
- **D-04:** Structural changes to a procedure used by a plan create a new derived procedure revision, not an in-place edit to the pinned revision. The derived revision is reusable by other plans and retains its relationship to the source revision.

### Sample facts and lineage
- **D-05:** A scientist separately confirms actual sample collection; completion of a planned procedure step alone does not create a confirmed sample.
- **D-06:** Helix assigns each sample an immutable unique identity. Scientists may add a human-readable label, but changing that label does not change identity.
- **D-07:** Record collection and custody/location changes as dated, attributable append-only events; derive the current custody state from that history.
- **D-08:** Represent splits, pools, and other sample processing as attributable transformation events linking one or more parent samples to child samples. Capture operation and quantities/units when relevant.

### Folded Todos
- **Spike canvas-first scientific workspace:** Use its preferred workspace direction as provisional design input for setup-facing work: canvas-first apparatus context, graph preferred with cards available as a semantics-preserving toggle, and a contextual inspector. Stable identities, selection, draft state, and canonical meaning must survive representation changes. The comparisons are still `PENDING` and awaiting human review; folding this TODO does not validate a winner or authorize a production redesign. See `.planning/todos/spike-canvas-first-scientific-workspace.md` for the remaining review/evidence requirements.

### the agent's Discretion
- Research and planning choose the concrete domain model, persistence, APIs, migration strategy, and test approach. Keep apparatus topology distinct from procedure graphs, and preserve Agent-only physical authority and the project’s scientific-integrity constraints.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Product scope, requirements, and prior decisions
- `.planning/PROJECT.md` — Local-first product goal, Agent authority, safety and scientific-integrity constraints.
- `.planning/REQUIREMENTS.md` — `APP-01`–`APP-04` and `SAMP-01`–`SAMP-02` requirements for this phase.
- `.planning/ROADMAP.md` §Phase 2 — Fixed phase goal, boundary, dependencies, and success criteria.
- `.planning/STATE.md` — Phase 1 caveats and current planning state. Note: it currently identifies Phase 02.1 as ready while the roadmap orders 02.1 after Phase 2; reconcile this before execution without overwriting unrelated state changes.
- `.planning/phases/01-agent-safety-boundary/01-CONTEXT.md` — Locked Agent-only access, safety admission, reconciliation, and immutable command-evidence decisions.

### Folded workspace design input
- `.planning/todos/spike-canvas-first-scientific-workspace.md` — User feedback, preferred direction, constraints, and pending human review; not production approval.
- `.planning/spikes/001-canvas-workspace-comparison/README.md` — Canvas-first vs ribbon/dock comparison and evidence status.
- `.planning/spikes/002-protocol-representation-comparison/README.md` — Graph/cards comparison; graph preference and semantics-preservation notes.
- `.planning/spikes/003-inspector-layout-comparison/README.md` — Contextual inspector vs permanent panels comparison and evidence status.

### Existing code and integration seams
- `Helix.Orchestration.Core/WSL/WslDocument.cs` and `Helix.Orchestration.Core/WSL/WslNode.cs` — Existing workflow/procedure graph representation; not an apparatus topology model.
- `Helix.Orchestration.Core/Devices/DeviceDescriptor.cs` and `Helix.Orchestration.Core/Devices/DeviceRegistry.cs` — Logical device identity, alias, type, capabilities, and metadata pattern.
- `helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.tsx` — Existing React Flow workflow authoring surface and Agent simulator discovery integration.
- `helix-desktop/src/lib/workflowLibrary.ts` — Current workflow JSON save/load in Tauri application data; it is not a domain revision store.
- `helix-desktop/src/shell/panels/DeviceExplorer.tsx` — Existing simulator-focused device discovery UI pattern.

No Phase 2 SPEC.md or external ADR was found; the phase requirements are defined by `REQUIREMENTS.md` and `ROADMAP.md`.
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `WslDocument` / `WslNode` and the Core WSL compiler provide an existing executable workflow graph. Keep this distinct from the physical apparatus model; the model currently has no first-class apparatus, procedure, or sample domain types.
- `DeviceDescriptor` / `DeviceRegistry` represent logical device identity, aliases, device type, capabilities, and metadata, which are relevant to apparatus device bindings.
- `WorkflowBuilder` already uses React Flow and reads Agent-discovered simulator devices; it may inform setup-facing interaction, but currently authors executable workflows rather than apparatus topology.
- `DeviceExplorer` provides a small simulator discovery pattern for the desktop.

### Established Patterns
- Physical commands remain behind the local Agent’s capability and safety boundary; setup records and UI gestures are not execution authority.
- Workflow documents currently save as JSON under Tauri AppData through `workflowLibrary.ts`; this is a convenience library, not immutable revisioning or durable cross-service domain persistence.
- Core models are separated from host composition, while Site Server endpoints/repositories and desktop clients provide integration seams. No existing setup/sample persistence path was found.

### Integration Points
- New apparatus, procedure/plan revision, and sample records are net-new domain concepts. Planning should identify their Core contracts, durable persistence/API boundary, and desktop entry points without merging physical topology into the WSL workflow graph.
- Setup authoring can draw on the existing React Flow and device-discovery UI, subject to the pending human review and approval boundary described in the folded TODO.

</code_context>

<specifics>
## Specific Ideas

- The user preferred the canvas-first workspace visually, said they liked the workflow graph and wanted to alternate between graph and cards, and liked the contextual inspector. Treat these as positive but unvalidated preferences, not usability findings or production approval.
- Sample confirmation is a separate fact from planned execution; custody and transformation history should retain provenance, parent/child relationships, and quantities/units when relevant.
</specifics>

<deferred>
## Deferred Ideas

- Human comparative evaluation of the three workspace spikes and any production redesign remain pending under the canvas-first workspace TODO / Phase 02.1; this context does not complete that work or authorize redesign.
- None — other discussion stayed within Phase 2 scope.

</deferred>

---

*Phase: 2-Scientific Setup Foundation*
*Context gathered: 2026-10-30*

## Authorized Planning Addendum — Narrow Policy Reference Provider

**Authorization:** The user selected “Narrow provider (Recommended)” when planning paused at the APP-04 policy-provider checkpoint. This addendum does not replace D-01–D-08.

- **P-01:** Phase 2 may establish a narrowly scoped immutable policy-reference/content-resolution provider and an explicit approval-provenance contract. Content identity, exact revision resolution, and approval-provenance validation belong to this foundation; campaign approval and execution workflows do not.
- **P-02:** Missing content, absent approval provenance, invalid provenance, or unapproved provenance must fail closed. Registering immutable policy content never grants approval. The downstream campaign phase owns actual scientist approval and issuance of authoritative approval provenance.
- **P-03:** Phase 2 must prove frozen apparatus/procedure/plan/policy content resolution, manifest consistency, and execution-binding consumption against a trusted test approval authority, including rejection paths and reconstruction after later publication. Test approval provenance is evidence of the consumer contract, not production campaign approval. Report downstream approval-provider wiring and campaign execution acceptance separately; a policy-unavailable screen alone does not satisfy APP-04.

Campaign approval, durable campaign execution, analytical evidence, adaptive selection, and real-device execution remain outside this phase. The Phase 02.1 comparison/explicit-approval fence remains unchanged: no production canvas-first redesign, graph/cards toggle, or persistent contextual inspector is authorized.

### Corrected decomposition record — Restore tracer

**P-04 (corrected):** The user explicitly selected “Restore tracer (Recommended)” in Phase 2 planning revision iteration 1. `TRACER_MODE=true`, `MVP_MODE=true`, and `REVERSIBILITY_GATES=true`. Deliver one thin production-quality desktop → native session broker → Agent → PostgreSQL path, verify it end-to-end before behavior expansions, and keep scientific scope unchanged. Checkpoint/preflight work may precede the first production implementation; it is not a substitute for the tracer.

**Correction provenance:** The previous addendum asserted “Disable tracer for this phase” without verifiable user authorization. That planner-authored inference from the tracer/file-count tension was incorrectly represented as a user choice and propagated into plan objectives and the planning contract. It is withdrawn, not a historical approval. File-count guidance does not authorize abandoning tracer-first. Only P-01–P-03 authorize the narrow policy foundation; P-04 changes decomposition, never domain scope.

### Authorized tracer-only sizing exception

**P-05:** The user explicitly selected “Allow tracer exception (Recommended)” at the task-sizing checkpoint. ONLY the leading 02-18 Task 1 real desktop → native session → Agent → PostgreSQL tracer may exceed the five-file/task bound: its cohesive 25-file slice retains explicit read lists, acceptance and runnable verification. This is not a phase-wide sizing waiver. All expansion tasks must be split into bounded units of at most five modified files, without disabling tracer or losing scientific scope. The tracer's successful automated and actual desktop evidence gates every expansion. No commits or STATE finalization are authorized by this revision.
