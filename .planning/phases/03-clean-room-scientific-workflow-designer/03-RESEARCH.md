# Phase 3: Clean-room Scientific Workflow Designer - Research

**Researched:** 2026-11-02
**Domain:** Proposal-only scientific workflow authoring workspace
**Confidence:** MEDIUM

<user_constraints>
## User Constraints (from CONTEXT.md)

### Provenance and locked direction
This phase implements the direction locked by Phase 02.1, including
`02.1-PHASE-3-RECOMMENDATION.md`, `02.1-CONTEXT.md` D-01 through D-11, and
the Phase 02.1 clean-room and UAT records. Spike 004 remains background
research only; its former cards-first and on-demand graph direction is
superseded.

### Locked Decisions
- **D-01/D-04:** Build one independently authored workflow board as the
  primary scientific-workspace representation. Do not restore cards-first
  navigation, a graph mode, or a graph toggle as an alternate primary
  workspace.
- **D-02/D-03:** Maintain an MIT-only clean room. Do not copy or adapt
  IvoryOS source, UI, styles, text, layout, assets, API shapes, names, logos,
  or branding. Preserve independent provenance for terminology, interactions,
  visuals/layout, APIs/data contracts, assets, and branding.

### Locked Safety and Record Boundary
The Designer is proposal-only. The local Helix Agent remains the only
authority that can address drivers or execute physical commands after
deterministic capability, safety, lease, resource, budget, and termination
validation. The board must not dispatch commands, grant approval, or confirm
execution.

Retain exact immutable published/approved identities, revision or frozen
references, independent procedure and plan revisions, sample custody and
lineage, append-only history, and transformation facts. Never infer a sample,
analytical result, or physical execution from a planned step.

### Locked Workflow-Board Behavior
- Provide an Agent-declared logical capability palette and typed inspector for
  bindings, units, defaults, provenance, quality/review state, and resources.
- Model explicit If/Else true/false ports and evidence placeholders; model
  Fork/Join with Agent resource and lease diagnostics.
- Resolve draft changes before revision, route, or procedure changes: require
  Save, Discard, or Keep editing. Keep editing retains the original draft
  context; never silently autosave, retarget, approve, or apply a draft to a
  reviewed run.
- Show a persistent, truthful reconciliation strip. Held, reconciling,
  faulted, uncertain, missing, or contradictory Agent evidence is read-only
  across pointer, keyboard, shortcut, inspector, and route affordances, and
  offers recovery/evidence navigation rather than an execution,
  acknowledgement, or confirmation control.
- Make the lifecycle legible as Draft -> Agent preflight -> scientist approval
  -> observed execution, without representing a UI proposal as authority.

### Locked Acceptance and Promotion Gate
Keep the legacy workspace available as the comparator until this gate is
complete. Before any primary-workspace change or promotion, Phase 3 must
perform a task-based installed-Tauri comparison for setup, experiment, plan,
run, results, and samples/analysis; capture keyboard-only and screen-reader
evidence, outcomes, and evidence locations for every UAT row; and obtain a
future explicit scientist approval. A prototype, automated check, or design
preference cannot satisfy this gate.

### the agent's Discretion
No `## the agent's Discretion` section exists in `03-CONTEXT.md`; therefore no discretionary user choice is copied. [VERIFIED: .planning/phases/03-clean-room-scientific-workflow-designer/03-CONTEXT.md:1-59]

### Deferred Ideas (OUT OF SCOPE)
No `## Deferred Ideas` section exists in `03-CONTEXT.md`; Phase 3 treats only the canonical locked scope above as in scope. [VERIFIED: .planning/phases/03-clean-room-scientific-workflow-designer/03-CONTEXT.md:1-59]
</user_constraints>

## Summary

Phase 3 is a desktop-renderer/UI integration phase, not a scientific-domain or execution phase. Implement one original workflow board at the existing `HelixShell` → `WorkspaceLayout` composition seam, backed by Helix-owned Agent/native read models and the six Phase 02.1 evidence projections. Preserve the legacy dock workspace as the UAT comparator until the recorded installed-Tauri gate and future scientist decision are complete. [VERIFIED: .planning/phases/02.1-scientific-workspace-ui-rework/02.1-WORKSPACE-IMPLEMENTATION-DECISION.md:12-22]

The board must derive a single conservative interaction policy: editable only when the native health observation is exactly `healthy`; held, reconciling, faulted, uncertain, missing, contradictory, and unknown evidence are read-only. It must expose evidence/recovery navigation rather than physical-command, acknowledgement, or approval controls. [VERIFIED: .planning/phases/02.1-scientific-workspace-ui-rework/02.1-RECONCILIATION-COMPARATOR-DISCOVERY.md:20-27,40-51]

**Primary recommendation:** Build an independently designed React board projection beside—not in place of—the legacy dock layout, centralize fail-closed reconciliation and draft-context resolution, reuse Phase 2’s exact Agent/native records read-only, and make UAT/promotion a terminal gated workstream rather than an implementation side effect. [VERIFIED: .planning/phases/02.1-scientific-workspace-ui-rework/02.1-PHASE-3-RECOMMENDATION.md:38-54]

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|---|---|---|---|
| Board composition, focus, keyboard interactions, and visual hierarchy | Browser / Client | Frontend Server — | The desktop React renderer owns presentation; it cannot create execution authority. [VERIFIED: AGENTS.md:182-186] |
| Scientific record identity, immutable revisions, custody, lineage, and evidence reads | API / Backend | Database / Storage | Phase 2 Agent-backed records remain the source of truth; renderer state cannot substitute identities. [VERIFIED: .planning/phases/02.1-scientific-workspace-ui-rework/02.1-WORKSPACE-BOARD-CONTRACT.md:9-16] |
| Reconciliation disposition | API / Backend | Browser / Client | The board adapts Agent/native observations and must fail closed rather than infer state locally. [VERIFIED: .planning/phases/02.1-scientific-workspace-ui-rework/02.1-RECONCILIATION-COMPARATOR-DISCOVERY.md:31-43] |
| Capability, safety, lease, resource, budget, and termination admission | API / Backend | — | The local Helix Agent alone validates and executes physical commands; this UI only proposes or observes. [VERIFIED: 03-CONTEXT.md:20-26] |

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|---|---|---:|---|---|
| Node.js | Desktop tests/build and validator | ✓ | `v24.12.0` | — [VERIFIED: environment probe, 2026-11-02] |
| npm | Desktop scripts | ✓ | `11.6.2` | — [VERIFIED: environment probe, 2026-11-02] |
| Cargo | Installed-Tauri build/UAT | ✓ | `1.95.0` | — [VERIFIED: environment probe, 2026-11-02] |
| .NET SDK | Existing Agent/backend integration | ✓ | `11.0.100-rc.3.26522.148` | — [VERIFIED: environment probe, 2026-11-02] |
| Global `tauri` CLI | Direct shell command | ✗ | — | Use repository `npm run tauri`, which invokes the installed project CLI. [VERIFIED: environment probe, 2026-11-02; helix-desktop/package.json:6-11,25-36] |
| Local Helix Agent with representative records | Final installed-Tauri comparative UAT | Not established by this research | — | Blocking human/environment checkpoint; no synthetic record or prototype can close the gate. [VERIFIED: .planning/phases/02.1-scientific-workspace-ui-rework/02.1-PROMOTION-UAT.md:9-20] |

**Missing dependencies with no fallback:** Actual Agent-backed representative records plus human keyboard/screen-reader/scientist-approval evidence for the final UAT gate. [VERIFIED: .planning/phases/02.1-scientific-workspace-ui-rework/02.1-PHASE-3-RECOMMENDATION.md:38-54]

**Missing dependencies with fallback:** No global Tauri CLI; use the local package script. [VERIFIED: helix-desktop/package.json:6-11,25-36]

## Validation Architecture

### Test Framework
| Property | Value |
|---|---|
| Framework | Vitest `^4.1.7` with React Testing Library `^16.3.2` [VERIFIED: helix-desktop/package.json:26-28,32-36] |
| Config file | `helix-desktop/vitest.config.ts` using jsdom and `src/test/setup.ts` [VERIFIED: helix-desktop/vitest.config.ts:1-11; helix-desktop/src/test/setup.ts:1] |
| Quick run command | `npm test -- --run` [VERIFIED: helix-desktop/package.json:6-11] |
| Full suite command | `npm test -- --run` [VERIFIED: helix-desktop/package.json:6-11] |

**Current baseline:** `npm test -- --run` completed with 37 test files and 155 tests passing on 2026-11-02. [VERIFIED: environment test execution, 2026-11-02]

### Phase Behaviors → Test Map
| Behavior | Test Type | Automated Command | File Exists? |
|---|---|---|---|
| Board renders one original primary board representation without a cards/graph mode toggle | component/unit | `npm test -- --run` | ❌ Wave 0 |
| Capability palette and typed inspector show logical Agent declarations but do not expose driver/command APIs | component/unit with mocked Agent read model | `npm test -- --run` | ❌ Wave 0 |
| All non-exact-healthy, missing, conflicting, and unknown reconciliation inputs block pointer, keyboard, shortcut, inspector, and route mutations | unit + component | `npm test -- --run` | ❌ Wave 0 |
| Dirty revision/route/procedure change requires Save, Discard, or Keep editing; retained context is asserted | component | `npm test -- --run` | ❌ Wave 0 |
| Six projections retain exact Agent-backed identity/evidence and do not infer result, sample, or execution | component | `npm test -- --run` | ❌ Wave 0 |
| Legacy comparator remains reachable before gate completion | shell/layout component | `npm test -- --run` | ❌ Wave 0 |
| Actual installed-Tauri six-view comparison, keyboard-only and screen-reader evidence | manual installed application UAT | `npm run tauri` then complete six UAT rows | ❌ mandatory human gate |

### Sampling Rate
- **Per task commit:** `npm test -- --run`
- **Per wave merge:** `npm test -- --run`
- **Phase gate:** Desktop build plus full installed-Tauri six-row comparative/accessibility UAT, recorded evidence locations/outcomes, and explicit scientist approval before promotion. [VERIFIED: .planning/phases/02.1-scientific-workspace-ui-rework/02.1-PROMOTION-UAT.md:9-28]

### Wave 0 Gaps
- [ ] `helix-desktop/src/components/ScientificWorkflowBoard/ScientificWorkflowBoard.test.tsx` — one board representation, capability/inspector projection, identity evidence.
- [ ] `helix-desktop/src/hooks/useBoardInteractionPolicy.test.ts` — complete fail-closed state matrix and every interaction entry point.
- [ ] `helix-desktop/src/hooks/useDraftContextResolver.test.ts` — Save/Discard/Keep editing state and retained context.
- [ ] `helix-desktop/src/components/ScientificWorkflowBoard/ReconciliationStrip.test.tsx` — persistent accessible status/evidence/recovery only; no execution/approval acknowledgement affordance.
- [ ] `helix-desktop/src/components/ScientificWorkflowBoard/ScientificWorkflowBoard.a11y.test.tsx` — keyboard command blocking, focus return, dialog names, and status announcements. [ASSUMED: Suggested test-file partitioning; exact component names are a Phase 3 implementation decision.]
- [ ] Human UAT records under the Phase 3 directory for all six rows, populated only from actual installed-Tauri sessions. [VERIFIED: .planning/phases/02.1-scientific-workspace-ui-rework/02.1-PROMOTION-UAT.md:13-28]

## Security Domain

Security enforcement is enabled because `.planning/config.json` has no explicit `security_enforcement: false` setting. [VERIFIED: .planning/config.json:1-44]

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---|---|---|
| V2 Authentication | yes | Use existing authenticated native broker/Agent read and mutation seams; do not create a renderer-direct driver path. [VERIFIED: helix-desktop/src/hooks/useAgentHealth.ts:10-14; AGENTS.md:15-16] |
| V3 Session Management | yes | Do not treat local layout/draft/session storage as Agent authority or scientific evidence. [VERIFIED: helix-desktop/src/hooks/useScientificOperation.ts:20-38] |
| V4 Access Control | yes | The board remains proposal/read-only; physical execution stays Agent-only. [VERIFIED: 03-CONTEXT.md:20-26] |
| V5 Input Validation | yes | Validate typed UI values before proposal serialization; preserve Agent-side validation as the authority. [ASSUMED: Phase 3 must add typed client validation without changing the locked backend/domain contract.] |
| V6 Cryptography | no new Phase 3 cryptographic function | Do not add custom cryptography. [ASSUMED] |

### Known Threat Patterns for the desktop board
| Pattern | STRIDE | Standard Mitigation |
|---|---|---|
| UI control is mistaken for physical authority | Spoofing / Elevation of Privilege | Persistent proposal-only language, no board execution/approval controls, Agent-only admission boundary. [VERIFIED: 03-CONTEXT.md:20-26] |
| Stale/missing/conflicting evidence enables a mutation | Tampering | Central fail-closed policy and recovery navigation; unknown evidence is read-only. [VERIFIED: .planning/phases/02.1-scientific-workspace-ui-rework/02.1-RECONCILIATION-COMPARATOR-DISCOVERY.md:40-51] |
| Planned state is represented as an observed sample/result/execution fact | Tampering / Repudiation | Exact Agent-backed identity/status fields and explicit unavailable evidence; preserve sample custody/lineage. [VERIFIED: .planning/phases/02.1-scientific-workspace-ui-rework/02.1-WORKSPACE-BOARD-CONTRACT.md:11-16] |
| Stored layout/workflow JSON masquerades as a scientific record | Tampering | Keep `workflowLibrary` local convenience data outside scientific revision/custody authority. [VERIFIED: helix-desktop/src/lib/workflowLibrary.ts:4-35] |
| Keyboard-only edits bypass disabled pointer controls | Elevation of Privilege | Policy-gate every mutation path and test keyboard/shortcut/route behavior. [VERIFIED: 03-CONTEXT.md:43-46] |

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|---|---|---|
| A1 | Existing `@xyflow/react` can serve as the final board renderer after adding an original, keyboard-accessible control layer. | Standard Stack / Don’t Hand-Roll | A replacement or accessibility adapter may be required. |
| A2 | Proposed `ScientificWorkflowBoard` / policy / resolver test file names are appropriate. | Validation Architecture | Planner must align file names with final component decomposition. |
| A3 | Phase 3 can add typed client-side proposal validation without a Phase 2 backend/domain change. | Security Domain | Must confirm no contract extension is needed; otherwise stop at a planned UI-only validation boundary. |
| A4 | No new cryptography is needed for this UI-only redesign. | Security Domain | Reassess if Phase 3 introduces persistence or credentials outside existing native/Agent paths. |

## Open Questions

1. **What exact Agent/native read-model contract supplies every Phase 3 board capability, typed inspector field, lease/resource diagnostic, and reconciliation state?**
   - What we know: Phase 02.1 identifies `useAgentHealth`, retained operation receipts, and six evidence projections, and requires no renderer-local state inference. [VERIFIED: .planning/phases/02.1-scientific-workspace-ui-rework/02.1-RECONCILIATION-COMPARATOR-DISCOVERY.md:29-51]
   - What's unclear: Existing desktop sources do not expose a complete reconciled board state or all requested designer diagnostics. [VERIFIED: .planning/phases/02.1-scientific-workspace-ui-rework/02.1-RECONCILIATION-COMPARATOR-DISCOVERY.md:11-16]
   - Recommendation: Begin with an interface/adapter inventory task; only consume existing Agent/native read models and fail closed for each unavailable input. Do not authorize a Phase 2 backend/domain change. [VERIFIED: 03-CONTEXT.md:43-47]

2. **When can primary-workspace promotion occur?**
   - What we know: Its controlling status is `PHASE_3_EXECUTION_REQUIRED`; Phase 02.1 explicitly decided `NO_FINAL_PROMOTION_IN_PHASE_02_1`. [VERIFIED: .planning/phases/02.1-scientific-workspace-ui-rework/02.1-PROMOTION-UAT.md:3-11; .planning/phases/02.1-scientific-workspace-ui-rework/02.1-PROMOTION-DECISION.md:1-21]
   - What's unclear: Actual six-session installed-Tauri results and future scientist decision are not yet recorded. [VERIFIED: .planning/phases/02.1-scientific-workspace-ui-rework/02.1-PHASE-3-RECOMMENDATION.md:38-44]
   - Recommendation: Make promotion a plan checkpoint after—not alongside—the mandatory UAT evidence work; preserve comparator until it passes. [VERIFIED: 03-CONTEXT.md:51-59]

## Sources

### Primary (HIGH confidence)
- `03-CONTEXT.md` — locked clean-room, authority, board, reconciliation, scientific-record, and promotion constraints.
- `.planning/phases/02.1-scientific-workspace-ui-rework/02.1-PHASE-3-RECOMMENDATION.md` — accepted Phase 3 handoff and non-promotion boundary.
- `.planning/phases/02.1-scientific-workspace-ui-rework/02.1-WORKSPACE-BOARD-CONTRACT.md` — six-view exact-record bindings.
- `.planning/phases/02.1-scientific-workspace-ui-rework/02.1-RECONCILIATION-COMPARATOR-DISCOVERY.md` — fail-closed evidence disposition and seams.
- `helix-desktop/src/shell/HelixShell.tsx` and `WorkspaceLayout.tsx` — current composition and legacy comparator integration seam.
- `helix-desktop/src/components/ScientificSetup/*`, `Samples/Samples.tsx`, and `shell/panels/ExperimentTimeline.tsx` — existing immutable/evidence UI patterns.

### Secondary (MEDIUM confidence)
- `.planning/spikes/004-canvas-workspace-evaluation/WRAP-UP.md` and `CLEAN-ROOM-DECISION.md` — background behavioral research only; superseded implementation direction is not adopted.

### Tertiary (LOW confidence)
- None beyond assumptions A1–A4.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — directly verified in the desktop manifest and live source.
- Architecture: HIGH — directly verified in current shell/layout/source and accepted Phase 02.1 handoff.
- Pitfalls: HIGH — driven by explicit fail-closed, authority, provenance, and UAT constraints plus current implementation seams.

**Research date:** 2026-11-02
**Valid until:** 2026-12-02 for codebase seam findings; revalidate before plan execution if desktop source or Phase 02.1 handoff changes.
## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---|---|---|---|
| Physical command dispatch, admission, approval, or execution confirmation | A board-side command client, policy approval surrogate, or device-control button | Existing Helix Agent/native authenticated boundary and Agent evidence projections | Only the Agent may execute after deterministic validation. [VERIFIED: 03-CONTEXT.md:20-26] |
| Scientific identity/revision/custody/lineage store | Browser state, local board JSON, or labels as canonical record | Existing Agent-backed scientific records and exact identifiers | Local renderer state cannot replace immutable record provenance. [VERIFIED: .planning/phases/02.1-scientific-workspace-ui-rework/02.1-RECONCILIATION-COMPARATOR-DISCOVERY.md:44-51] |
| Uncertain operation recovery | A retry button that sends a new mutation | `useScientificOperation.reconcile` / existing receipt read paths | The hook retains the original operation identity and reads a receipt before committing UI state. [VERIFIED: helix-desktop/src/hooks/useScientificOperation.ts:20-55] |
| Dock layout persistence | A second layout persistence mechanism | `useWorkspaceLayouts` / `WorkspaceLayout` only for legacy comparator retention | Existing dock content requires `updateTab` for live prop refresh and serializes tab IDs, not React content. [VERIFIED: helix-desktop/src/shell/WorkspaceLayout.tsx:277-292,324-375] |
| Full canvas renderer | A new graph runtime | Existing `@xyflow/react` seam, but with original board components and explicit keyboard alternatives | A new package does not solve the safety or UAT constraints and would increase unvalidated surface. [ASSUMED] |

**Key insight:** Phase 3’s value is a safety-truthful, independently authored projection over existing records; it must not recreate Agent policy, persistence, or execution semantics in the renderer. [VERIFIED: .planning/phases/02.1-scientific-workspace-ui-rework/02.1-PHASE-3-RECOMMENDATION.md:34-36]

## Common Pitfalls

### Pitfall 1: Treating local health/lifecycle as authoritative reconciliation
**What goes wrong:** The board re-enables editing from cached renderer state, an unmatched event, or a stale health check.

**Why it happens:** Current health and lifecycle arrive on separate inputs, and the tracer explicitly says there is no complete reconciled board state on that path. [VERIFIED: .planning/phases/02.1-scientific-workspace-ui-rework/02.1-RECONCILIATION-COMPARATOR-DISCOVERY.md:11-16]

**How to avoid:** Centralize the fail-closed adapter; only the supported healthy evidence can allow proposal editing. Quote from the source of truth: `status.trim().toLowerCase() === "healthy"`; initial health is `healthy: null`. [VERIFIED: helix-desktop/src/hooks/useAgentHealth.ts:14-19,23-40]

**Warning signs:** A board action is enabled while the strip reads missing, contradictory, uncertain, held, faulted, or reconciling evidence. [VERIFIED: 03-CONTEXT.md:43-46]

### Pitfall 2: Leaking edits through keyboard, shortcuts, or navigation
**What goes wrong:** Pointer controls appear disabled but drag/drop, Delete/Backspace, a keyboard shortcut, inspector input, or context change still mutates a proposal.

**Why it happens:** The legacy builder registers pointer drag listeners and `deleteKeyCode={['Backspace', 'Delete']}` independently from its visual UI. [VERIFIED: helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.tsx:204-235,305-321]

**How to avoid:** Attach the single interaction policy at every mutation entry point and test each input mode. The reconciliation strip must offer only evidence/recovery navigation, not acknowledgement or execution-looking controls. [VERIFIED: 03-CONTEXT.md:43-47]

### Pitfall 3: Replacing exact records with board-local display data
**What goes wrong:** Labels, node IDs, layout positions, or planned collection nodes are presented as revisions, samples, results, or execution facts.

**Why it happens:** Existing workflow persistence writes a local JSON document under app data, which is convenient but not a Phase 2 immutable scientific record. [VERIFIED: helix-desktop/src/lib/workflowLibrary.ts:4-35]

**How to avoid:** Pass through source record IDs/revisions and explicitly render unavailable evidence. Existing Samples behavior is the required precedent: a planned reference does not create a sample. [VERIFIED: helix-desktop/src/components/Samples/Samples.tsx:13-14,130-161; helix-desktop/src/components/Samples/Samples.test.tsx:16-28]

### Pitfall 4: Accidentally making the board a promotion or approval mechanism
**What goes wrong:** A "start," "approve," "confirm," or primary-default switch appears with the board before the UAT gate closes.

**How to avoid:** Keep legacy workspace reachable as comparator; record all six installed-Tauri UAT rows with actual record identities, keyboard, screen-reader, observation, and evidence locations, then wait for explicit scientist approval. [VERIFIED: .planning/phases/02.1-scientific-workspace-ui-rework/02.1-PROMOTION-UAT.md:9-28]

### Pitfall 5: Clean-room provenance drift
**What goes wrong:** An external visual/textual/API expression becomes a design reference during implementation, or provenance is recorded only at the start.

**How to avoid:** Maintain a Phase 3 provenance update per terminology, interactions, visual/layout, API/data contract, assets/branding, sources reviewed, and excluded expression. [VERIFIED: .planning/phases/02.1-scientific-workspace-ui-rework/02.1-CLEAN-ROOM-PROVENANCE.md:7-24]

## Code Examples

### Fail-closed policy consumed by all mutation paths
```tsx
// Source: Helix-established fail-closed adapter contract
// .planning/phases/02.1-scientific-workspace-ui-rework/
// 02.1-RECONCILIATION-COMPARATOR-DISCOVERY.md:31-51
const policy = deriveBoardInteractionPolicy(evidence);

function requestBoardMutation(action: () => void) {
  if (!policy.canEdit) {
    navigateTo(policy.recoveryRoute);
    return;
  }

  action();
}
```

### Preserve scientific identity instead of creating local authority
```tsx
// Source: existing exact-reference evidence presentation
// helix-desktop/src/components/ScientificSetup/FrozenAssembly.tsx:64-88
function ReferenceEvidence({ reference }: { reference: Readonly<Record<string, string>> }) {
  return <code title={reference.id}>{reference.id}</code>;
}
```

The second pattern is presentational only; `reference.id` must be passed from the Helix-owned Agent/native record and never generated by the board. [VERIFIED: .planning/phases/02.1-scientific-workspace-ui-rework/02.1-WORKSPACE-BOARD-CONTRACT.md:9-16]

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|---|---|---|---|
| Spike 004 cards-first navigation with on-demand graph | One original workflow board; no cards/graph alternate primary mode | Superseded by Phase 02.1 D-01/D-04 | Do not implement or test a graph toggle as an acceptable Phase 3 outcome. [VERIFIED: 03-CONTEXT.md:3-18] |
| Non-primary six-view prototype harness | Final Phase 3 original board plus UAT gate | Phase 02.1 handoff | Prototype validation is structural evidence, not implementation/promotion evidence. [VERIFIED: .planning/phases/02.1-scientific-workspace-ui-rework/02.1-PHASE-3-RECOMMENDATION.md:16-18,46-54] |

**Deprecated/outdated:** Spike 004’s cards-first/on-demand graph production recommendation is superseded; retain it only as background behavioral research and clean-room boundary input. [VERIFIED: 03-CONTEXT.md:3-9]

## Project Constraints (from AGENTS.md)

- Keep UI and LLM output untrusted proposals; only the local Helix Agent may address drivers or execute physical commands. [VERIFIED: AGENTS.md:13-21]
- Preserve deterministic capability, safety, lease, resource, budget, and termination validation; do not weaken campaign/recovery evidence. [VERIFIED: AGENTS.md:15-19]
- Preserve immutable revisions, stable identities, provenance, unit/quality validation, and idempotent processing for scientific records. [VERIFIED: AGENTS.md:19-21]
- Use TypeScript PascalCase components/types, camelCase hooks/helpers, local import/style conventions, and co-located `*.test.ts(x)` tests. [VERIFIED: AGENTS.md:90-99]
- Preserve local desktop conventions; the desktop entry uses the default-export component convention. [VERIFIED: AGENTS.md:142-146]
- Render failed remote state as visible UI; do not swallow errors. [VERIFIED: AGENTS.md:114-120]

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---|---:|---|---|
| React | `^18.3.0` | Desktop renderer component/state composition | Already the renderer framework. [VERIFIED: helix-desktop/package.json:20-21] |
| TypeScript | `^5.5.0` | Typed UI contracts and build checking | Already compiled by the desktop `build` script. [VERIFIED: helix-desktop/package.json:6-11,34-36] |
| `@xyflow/react` | `^12.11.0` | Existing flow/canvas rendering | Reuse only as a rendering implementation seam; do not inherit its old interaction model or local authority. [VERIFIED: helix-desktop/package.json:17-22; helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.tsx:1-19] |
| `rc-dock` | `3.3.2` | Legacy comparator workspace mount | The existing `WorkspaceLayout` owns the dock registry and restoration seam. [VERIFIED: helix-desktop/package.json:19-22; helix-desktop/src/shell/WorkspaceLayout.tsx:158-292] |

### Supporting
| Library | Version | Purpose | When to Use |
|---|---:|---|---|
| Vitest | `^4.1.7` | Unit/component regression tests | Test policy, resolver branches, identity projection, and accessibility semantics. [VERIFIED: helix-desktop/package.json:6-11,32-36] |
| Testing Library | `^16.3.2` | User-facing React assertions | Use roles/labels and keyboard event coverage instead of implementation-only selectors where practical. [VERIFIED: helix-desktop/package.json:26-28; helix-desktop/src/shell/HelixShell.test.tsx:1-4] |
| Tauri 2 | `^2.0.0` API and CLI | Installed desktop comparative UAT target | Use for the final human gate; jsdom tests cannot substitute for it. [VERIFIED: helix-desktop/package.json:6-16,25-36; helix-desktop/src-tauri/tauri.conf.json:6-35] |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|---|---|---|
| Existing ReactFlow renderer seam | A new graph/canvas package | Do not install one: the required change is an original board behavior and safety policy, not a renderer replacement. [ASSUMED] |
| Existing dock comparator | Replacing `WorkspaceLayout` immediately | Contradicts the required legacy comparator retention until installed-Tauri UAT and approval complete. [VERIFIED: 03-CONTEXT.md:51-59] |

**Installation:** No Phase 3 package installation is recommended. Reuse the lockfile-backed desktop dependencies above. [VERIFIED: helix-desktop/package.json:13-36]

## Architecture Patterns

### System Architecture Diagram

```text
Scientist keyboard / pointer / screen reader
                 |
                 v
Original workflow board (renderer proposal + read-model projection)
   |                    |                     |
   |                    |                     +--> Legacy dock workspace (UAT comparator only)
   |                    +--> Draft resolver (Save / Discard / Keep editing)
   v
Reconciliation policy adapter <--- native health + retained operation receipt + Agent evidence
   |                 |                |
   | editable only with exact evidence | otherwise read-only strip -> evidence/recovery view
   v
Helix-owned client/native broker --> Local Helix Agent --> deterministic admission / scientific records
                                                   |
                                                   +--> physical driver commands (Agent only)
```

### Recommended Project Structure
```text
helix-desktop/src/
├── components/ScientificWorkflowBoard/  # original board, palette, inspector, reconciliation strip
├── hooks/                               # board interaction-policy and draft-resolution hooks
├── shell/                               # HelixShell/WorkspaceLayout composition and legacy comparator mount
├── components/ScientificSetup/          # existing Agent-backed procedure/plan/assembly evidence projections
└── components/Samples/                  # existing sample, custody, lineage projections
```

### Pattern 1: Explicit board policy adapter
**What:** Create one pure, exhaustively tested adapter that accepts Agent/native evidence and returns an interaction disposition plus cited evidence/recovery destination. It must be the sole source of `disabled`, command interception, inspector editability, and route-change gating.

**When to use:** Every board action and navigation path, including pointer actions, keyboard actions, shortcuts, palette addition, inspector edits, and selection/route changes.

**Evidence:** The existing Phase 02.1 tracer requires exact-supported healthy observation for editability and all other known/unknown conditions to fail closed. [VERIFIED: .planning/phases/02.1-scientific-workspace-ui-rework/02.1-RECONCILIATION-COMPARATOR-DISCOVERY.md:40-47]

**Verbatim state evidence:** `HelixShell` currently maps runspace events through:
`'runspace-started': 'Running', 'runspace-paused': 'Paused', 'runspace-completed': 'Completed', 'runspace-aborted': 'Aborted', 'runspace-faulted': 'Held'`. [VERIFIED: helix-desktop/src/shell/HelixShell.tsx:48-60]

### Pattern 2: Resolver-gated context changes
**What:** Treat a requested revision, route, or procedure change as pending intent. If the proposal is dirty, open an accessible modal with Save, Discard, and Keep editing. Save must complete/acknowledge before applying the requested target; Discard clears only current local draft; Keep editing cancels the requested change and retains the same draft context.

**When to use:** Any board navigation that changes scientific identity or revision context.

**Why:** The inherited procedure/plan UI has individual mutation state but no general dirty-context resolver; Phase 3 needs the centralized guard rather than a silent change. [VERIFIED: helix-desktop/src/components/ScientificSetup/ProcedurePlans.tsx:20-30,32-58; .planning/phases/02.1-scientific-workspace-ui-rework/02.1-WORKSPACE-BOARD-CONTRACT.md:20-23]

### Pattern 3: Projection, not replacement
**What:** Board cards/nodes render exact server/Agent identities, revisions, frozen references, custody/history, provenance, and evidence status supplied by existing records. Local selection and layout are presentational; they never become the scientific record or execution state.

**When to use:** Each of setup, experiment, plan, run, results, and samples-analysis projections.

**Example:** `FrozenAssembly` constructs a list from exact apparatus, plan, plan-pinned procedure, policy revision, and preview-token values and states that later publications do not update the assembly or start a campaign. [VERIFIED: helix-desktop/src/components/ScientificSetup/FrozenAssembly.tsx:36-45,64-88]

### Anti-Patterns to Avoid
- **Restoring Cards/Graph primary modes:** violates D-01/D-04; the board is the only primary representation. [VERIFIED: 03-CONTEXT.md:11-18]
- **Renderer-local reconciliation:** conflicts must remain explicit; local events/caches cannot establish safety. [VERIFIED: .planning/phases/02.1-scientific-workspace-ui-rework/02.1-RECONCILIATION-COMPARATOR-DISCOVERY.md:11-16,40-43]
- **Embedding execution controls in the board:** mounting `ExecutionPanel`, direct client controls, or driver capability calls violates proposal-only scope. [VERIFIED: 03-CONTEXT.md:20-26]
- **Using the old `WorkflowBuilder` unchanged:** it auto-exports canvas changes and writes local workflow storage, so it is an implementation reference, not the Phase 3 scientific-record model. [VERIFIED: helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.tsx:137-164]
