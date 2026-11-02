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

Phase 3 is a desktop-renderer/UI integration phase, not a scientific-domain or execution phase. The live composition seam is `HelixShell` → `WorkspaceLayout`, but its current prop contract carries streams and workflow state only; it does **not** carry health, lifecycle, a retained operation, a reconciled evidence result, or a shared scientific-board record. A Phase 3 board may consume only the documented native reads below; every missing or non-comparable input renders a truthful unavailable/read-only state. [VERIFIED: helix-desktop/src/shell/HelixShell.tsx:153-163; helix-desktop/src/shell/WorkspaceLayout.tsx:30-42]

The only current Agent-backed eligibility observation is the native-broker health read represented by `AgentHealthState.healthy: boolean | null`. The hook converts only the exact response expression `status.trim().toLowerCase() === "healthy"` to `true`; pending is `null` and any non-exact response or read failure is `false`. It is not a reconciliation result, and the live app supplies no Agent-authoritative `reconciling`, `contradictory`, resource/lease, or all-six-view aggregate read model. Those unsupported inputs must produce persistent unavailable/read-only UI rather than locally invented states. [VERIFIED: helix-desktop/src/hooks/useAgentHealth.ts:4-8,15-19,23-40; helix-desktop/src/shell/HelixShell.tsx:153-163]

**Primary recommendation:** Mount an independently designed React board beside—not in place of—the legacy dock layout. Thread the existing health observation explicitly from `HelixShell` only if the new board needs it; retain the current `WorkspaceLayout` props and use source-backed native reads for a known immutable ID only. Do not invent a shared reconciliation contract or backend/domain input: unsupported lifecycle comparison, retained-operation correlation, diagnostics, and scientific records remain unavailable and fail closed. Keep UAT/promotion as the defined later human checkpoint. [VERIFIED: helix-desktop/src/shell/HelixShell.tsx:22-23,153-163; helix-desktop/src/shell/WorkspaceLayout.tsx:30-42; .planning/phases/02.1-scientific-workspace-ui-rework/02.1-PROMOTION-UAT.md:9-28]

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|---|---|---|---|
| Board composition, focus, keyboard interactions, and visual hierarchy | Browser / Client | Frontend Server — | The desktop React renderer owns presentation; it cannot create execution authority. [VERIFIED: AGENTS.md:182-186] |
| Scientific record identity, immutable revisions, custody, lineage, and evidence reads | API / Backend | Database / Storage | Phase 2 Agent-backed records remain the source of truth; renderer state cannot substitute identities. [VERIFIED: .planning/phases/02.1-scientific-workspace-ui-rework/02.1-WORKSPACE-BOARD-CONTRACT.md:9-16] |
| Reconciliation disposition | API / Backend | Browser / Client | The board adapts Agent/native observations and must fail closed rather than infer state locally. [VERIFIED: .planning/phases/02.1-scientific-workspace-ui-rework/02.1-RECONCILIATION-COMPARATOR-DISCOVERY.md:31-43] |
| Capability, safety, lease, resource, budget, and termination admission | API / Backend | — | The local Helix Agent alone validates and executes physical commands; this UI only proposes or observes. [VERIFIED: 03-CONTEXT.md:20-26] |

## Live Desktop Wiring Contract (Plan-Review Resolution)

### Actual `HelixShell` → `WorkspaceLayout` seam

`WorkspaceLayoutProps` currently declares exactly: `baseUrl`, `onSaveLayout`, `onLoadLayout`, `runspaceEvents`, `trendValues`, `safetyEvents`, `experimentEvents`, `activeExperimentId`, `currentWorkflow`, and `onWorkflowExport`. `HelixShell` supplies the required seven data props as `baseUrl`, `runspaceEvents`, `trendValues`, `safetyEvents`, `experimentEvents`, `activeExperimentId={timelineExperimentId}`, `currentWorkflow`, and `onWorkflowExport`. The following required Phase 3 inputs are **not current props**: `health`, `durableLifecycle`, `selectedExecutionId`, `AgentHealthState.lastChecked`, a `ScientificOperationResult`, a `CommandEvidenceHistoryState`, and any six-view board record. [VERIFIED: helix-desktop/src/shell/WorkspaceLayout.tsx:30-42; helix-desktop/src/shell/HelixShell.tsx:153-163]

| Input / exact live fields | Source and type | Agent-authoritative? | Missing/unhealthy behavior required for Phase 3 | Exact caller / adapter seam | Existing test evidence |
|---|---|---|---|---|---|
| Health: `healthy: boolean \| null`, `error: string \| null`, `lastChecked: Date \| null` | `useAgentHealth()` returns `AgentHealthState`; it calls `getAgentStatus(): Promise<string>`, which invokes Tauri `agent_status`; the Rust command requires the main window and session, then performs `GET /health`. [VERIFIED: helix-desktop/src/hooks/useAgentHealth.ts:4-8,14-18,23-40; helix-desktop/src/lib/agentClient.ts:178-181; helix-desktop/src-tauri/src/commands/agent.rs:344-348] | The raw status read is Agent-backed through the authenticated native broker. The `boolean` is renderer-derived from the exact string check, so it is a conservative eligibility projection, not a full reconciliation decision. [VERIFIED: helix-desktop/src/hooks/useAgentHealth.ts:10-13,25-31] | `null`, `false`, a non-null `error`, or absent board input => read-only with health-evidence/retry navigation. Only `healthy === true` is source-supported as a positive health observation; it cannot make an unsupported lifecycle/evidence contract available. [VERIFIED: helix-desktop/src/hooks/useAgentHealth.ts:15-19,27-39; .planning/phases/02.1-scientific-workspace-ui-rework/02.1-RECONCILIATION-COMPARATOR-DISCOVERY.md:40-43] | `HelixShell` already calls the hook at line 22 but passes none of its fields to `WorkspaceLayout`; Phase 3 must explicitly pass the existing observation to its new board factory or leave the board read-only. It must not re-fetch through browser networking. [VERIFIED: helix-desktop/src/shell/HelixShell.tsx:20-23,153-163; helix-desktop/src/hooks/useAgentHealth.ts:10-14] | `useAgentHealth.test.ts` proves pending → `null`, exact `"Healthy"` → `true`, and rejection → `false` plus error. `HelixShell.test.tsx` currently mocks only `healthy`/`error`; it does not assert propagation. [VERIFIED: helix-desktop/src/hooks/useAgentHealth.test.ts:13-35; helix-desktop/src/shell/HelixShell.test.tsx:59-62,81-132] |
| Last selected execution: `selectedExecutionId: string \| null` | Local `HelixShell` state. It updates only from `activeExperimentId`; `timelineExperimentId` is `activeExperimentId ?? selectedExecutionId`. [VERIFIED: helix-desktop/src/shell/HelixShell.tsx:29-30,43-46,65] | No. It is retained renderer selection, not an Agent receipt or a proven execution state. | `null` means no selected execution evidence; never manufacture an ID or select a lifecycle. Board must show unavailable/read-only evidence. [VERIFIED: helix-desktop/src/shell/HelixShell.tsx:43-46,65] | `WorkspaceLayout` receives only the derived `activeExperimentId={timelineExperimentId}` for `ExperimentTimeline`; it does not receive `selectedExecutionId`. [VERIFIED: helix-desktop/src/shell/HelixShell.tsx:153-163; helix-desktop/src/shell/WorkspaceLayout.tsx:234-246] | No test asserts selection retention, propagation, or an Agent receipt. `HelixShell.test.tsx` uses a mocked websocket with `activeExperimentId: null`. [VERIFIED: helix-desktop/src/shell/HelixShell.test.tsx:46-57,81-132] |
| Lifecycle: `durableLifecycle: string \| null` | Local `HelixShell` state set from a local map: `'runspace-started': 'Running', 'runspace-paused': 'Paused', 'runspace-completed': 'Completed', 'runspace-aborted': 'Aborted', 'runspace-faulted': 'Held'`. [VERIFIED: helix-desktop/src/shell/HelixShell.tsx:29-30,48-60] | No. The map is presentation state derived from received events; `runspace-faulted` is deliberately collapsed to `Held`. [VERIFIED: helix-desktop/src/shell/HelixShell.tsx:51-59] | `null`, any unmapped event, or any lifecycle use without an independently supplied Agent receipt => unavailable/read-only. Do not label a local `Held` as Agent reconciliation or expose a “reconciling” completion action. [VERIFIED: helix-desktop/src/shell/HelixShell.tsx:48-60; .planning/phases/02.1-scientific-workspace-ui-rework/02.1-RECONCILIATION-COMPARATOR-DISCOVERY.md:23-27,40-43] | It is not passed to `WorkspaceLayout`. Do not add it as if it were an authoritative contract; if displayed, label it as local event-derived and deny editing. [VERIFIED: helix-desktop/src/shell/HelixShell.tsx:153-163; helix-desktop/src/shell/WorkspaceLayout.tsx:30-42] | No lifecycle mapping test exists in `HelixShell.test.tsx`; the test only renders the shell with an empty mocked event list. [VERIFIED: helix-desktop/src/shell/HelixShell.test.tsx:46-57,81-132] |
| Stream state: `runspaceEvents: RunspaceEvent[]`, `connected: boolean`, `loading: boolean`, `error: string \| null`, `partial: boolean` | `useAgentWebSocket()` polls `pollCommittedEvidence`, maps returned `CommandEvidenceRecord` to `RunspaceEvent`, and retains at most 100 entries. Its actual current return fixes `activeExperimentId: null` and `experimentEvents: []`. [VERIFIED: helix-desktop/src/hooks/useAgentWebSocket.ts:5-16,24-34,40-45,63-90,105-116] | Committed evidence records are Agent-backed; connection/partial/loading and the `RunspaceEvent` adapter are renderer state. | `connected === false`, `error !== null`, `partial === true`, no selected execution, or no supplied receipt must not be transformed into reconciliation success. Board remains read-only and identifies evidence as unavailable/partial. [VERIFIED: helix-desktop/src/hooks/useAgentWebSocket.ts:85-90,105-116; .planning/phases/02.1-scientific-workspace-ui-rework/02.1-RECONCILIATION-COMPARATOR-DISCOVERY.md:40-43] | The array is already forwarded to `WorkspaceLayout` and `ExperimentTimeline`; `connected`, `loading`, `error`, and `partial` are discarded by `HelixShell`, so they are unsupported at the board seam unless Phase 3 explicitly threads those existing fields. [VERIFIED: helix-desktop/src/shell/HelixShell.tsx:23,153-163; helix-desktop/src/shell/WorkspaceLayout.tsx:35-39,234-242] | There is no `useAgentWebSocket` unit test in the current source tree; `HelixShell.test.tsx` mocks the returned fields. [VERIFIED: helix-desktop/src/shell/HelixShell.test.tsx:46-57] |
| Retained scientific operation: `state: "idle" \| "pending" \| "uncertain" \| "committed"`, optional `context`, optional `receipt`, and `reconcile()` | `useScientificOperation(userId)` stores `ScientificOperationContext` in session storage; a restored context begins `"uncertain"`. `reconcile()` reads `readScientificOperation(kind, operationId)` and returns a validated `ScientificReceipt`. [VERIFIED: helix-desktop/src/hooks/useScientificOperation.ts:5-18,20-31,40-55; helix-desktop/src/lib/scientificClient.ts:6-17,51-53] | The retained context and state are caller-local; only the returned receipt is Agent-backed. [VERIFIED: helix-desktop/src/hooks/useScientificOperation.ts:20-21,22-38,46-52] | No context, malformed/restoration failure, `pending`, or `uncertain` => no scientific outcome and read-only. Reconcile only the original `operationId`; do not retry or invent a receipt. [VERIFIED: helix-desktop/src/hooks/useScientificOperation.ts:24-31,45-52; helix-desktop/src/lib/scientificClient.ts:6-17] | This hook has **no caller** in `HelixShell` or `WorkspaceLayout`. It cannot supply their current board props. A board may use it only for a future board-originated proposal operation, not as a global “last operation” read model. [VERIFIED: helix-desktop/src/shell/HelixShell.tsx:1-170; helix-desktop/src/shell/WorkspaceLayout.tsx:1-420; helix-desktop/src/hooks/useScientificOperation.ts:20-55] | `useScientificOperation.test.ts` proves an uncertain original operation is retained and not committed; no test proves shell/layout integration or reconciliation success. [VERIFIED: helix-desktop/src/hooks/useScientificOperation.test.ts:7-17] |
| Command reconciliation evidence: `CommandEvidenceHistoryState` fields `records`, `loading`, `unavailable`, `error`, `refresh` | `useCommandEvidence(executionId)` reads cursor pages through the native `agent_evidence_cursor` route, merges stable `evidenceId` rows by sequence, and records unavailable history without deleting partial rows. [VERIFIED: helix-desktop/src/hooks/useCommandEvidence.ts:3-19,29-73; helix-desktop/src-tauri/src/commands/agent.rs:617-629] | `records` are Agent-backed committed facts; state flags are renderer fetch state. [VERIFIED: helix-desktop/src/hooks/useCommandEvidence.ts:29-34,53-63] | No known `executionId`, `unavailable === true`, `loading === true`, or an empty record set is unavailable evidence, not a safe lifecycle or an approval. Board remains read-only and may navigate to the timeline. [VERIFIED: helix-desktop/src/hooks/useCommandEvidence.ts:37-63; helix-desktop/src/shell/panels/ExperimentTimeline.tsx:99-104] | `ExperimentTimeline` owns this hook. `WorkspaceLayout` supplies `activeExperimentId`; no `CommandEvidenceHistoryState` crosses into board composition. [VERIFIED: helix-desktop/src/shell/WorkspaceLayout.tsx:234-246; helix-desktop/src/shell/panels/ExperimentTimeline.tsx:80-104] | `useCommandEvidence.test.ts` proves ordered identity merge and preservation of partial records with `unavailable: true`; `ExperimentTimeline.test.tsx` proves held evidence display. [VERIFIED: helix-desktop/src/hooks/useCommandEvidence.test.ts:14-35; helix-desktop/src/shell/panels/ExperimentTimeline.test.tsx:16-38] |

### Unsupported inputs — fixed fail-closed decision

The following inputs are not exposed by the current `HelixShell`/`WorkspaceLayout` seam or a current Agent/native read model available to the board: an Agent-authoritative reconciled lifecycle; an Agent-issued `reconciling` disposition; a comparator that can determine health-versus-lifecycle contradiction; a shell-wide retained/last operation; a shared six-view board record; Agent-provided resource/lease diagnostics; capability binding/default/provenance/quality/review metadata beyond the existing `CapabilityDescriptor`; and a reviewed analytical-result record. The board must render each as `Unavailable` with no mutation affordance and evidence/recovery navigation only. It must not introduce a Phase 2 backend/domain change, create a synthetic ID/status, or infer a scientific fact. [VERIFIED: helix-desktop/src/shell/WorkspaceLayout.tsx:30-42,247-261; helix-desktop/src/types/agent.ts:83-108; helix-desktop/src/types/scientific.ts:1-199; helix-desktop/src/shell/HelixShell.tsx:20-65]

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
- [ ] `helix-desktop/src/components/ScientificWorkflowBoard/ScientificWorkflowBoard.a11y.test.tsx` — keyboard command blocking, focus return, dialog names, and status announcements. The path and coverage are the accepted Phase 3 plan partition; source-backed policy tests must also cover every unavailable input listed in the Live Desktop Wiring Contract. [VERIFIED: .planning/phases/03-clean-room-scientific-workflow-designer/03-03-PLAN.md:7-14,59-68]
- [ ] Human UAT records under the Phase 3 directory for all six rows, populated only from actual installed-Tauri sessions. [VERIFIED: .planning/phases/02.1-scientific-workspace-ui-rework/02.1-PROMOTION-UAT.md:13-28]

## Security Domain

Security enforcement is enabled because `.planning/config.json` has no explicit `security_enforcement: false` setting. [VERIFIED: .planning/config.json:1-44]

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---|---|---|
| V2 Authentication | yes | Use existing authenticated native broker/Agent read and mutation seams; do not create a renderer-direct driver path. [VERIFIED: helix-desktop/src/hooks/useAgentHealth.ts:10-14; AGENTS.md:15-16] |
| V3 Session Management | yes | Do not treat local layout/draft/session storage as Agent authority or scientific evidence. [VERIFIED: helix-desktop/src/hooks/useScientificOperation.ts:20-38] |
| V4 Access Control | yes | The board remains proposal/read-only; physical execution stays Agent-only. [VERIFIED: 03-CONTEXT.md:20-26] |
| V5 Input Validation | yes | Use only existing typed client contracts where a source exists (for example, `CapabilityDescriptor`, `ProcedureDraft`, and `PlanDraft`); an unsupported field is unavailable/read-only, while the Agent remains the authority. [VERIFIED: helix-desktop/src/types/agent.ts:83-108; helix-desktop/src/types/scientific.ts:14-31] |
| V6 Cryptography | no Phase 3 credential or crypto seam is in the live board composition | Keep the current native broker boundary; Phase 3 must not add a credential, secret, or custom cryptographic path. [VERIFIED: helix-desktop/src/shell/HelixShell.tsx:18-23,153-163; helix-desktop/src-tauri/src/commands/agent.rs:344-348] |

### Known Threat Patterns for the desktop board
| Pattern | STRIDE | Standard Mitigation |
|---|---|---|
| UI control is mistaken for physical authority | Spoofing / Elevation of Privilege | Persistent proposal-only language, no board execution/approval controls, Agent-only admission boundary. [VERIFIED: 03-CONTEXT.md:20-26] |
| Stale/missing/conflicting evidence enables a mutation | Tampering | Central fail-closed policy and recovery navigation; unknown evidence is read-only. [VERIFIED: .planning/phases/02.1-scientific-workspace-ui-rework/02.1-RECONCILIATION-COMPARATOR-DISCOVERY.md:40-51] |
| Planned state is represented as an observed sample/result/execution fact | Tampering / Repudiation | Exact Agent-backed identity/status fields and explicit unavailable evidence; preserve sample custody/lineage. [VERIFIED: .planning/phases/02.1-scientific-workspace-ui-rework/02.1-WORKSPACE-BOARD-CONTRACT.md:11-16] |
| Stored layout/workflow JSON masquerades as a scientific record | Tampering | Keep `workflowLibrary` local convenience data outside scientific revision/custody authority. [VERIFIED: helix-desktop/src/lib/workflowLibrary.ts:4-35] |
| Keyboard-only edits bypass disabled pointer controls | Elevation of Privilege | Policy-gate every mutation path and test keyboard/shortcut/route behavior. [VERIFIED: 03-CONTEXT.md:43-46] |

## Assumptions Log

None. Former assumptions are now resolved as source-backed implementation limits: use React/TypeScript and already installed packages only; do not treat `@xyflow/react` as a scientific/read-model contract; use only the exact client types in the Live Desktop Wiring Contract; and create no crypto or backend/domain seam. [VERIFIED: helix-desktop/package.json:13-36; helix-desktop/src/types/agent.ts:83-108; helix-desktop/src/types/scientific.ts:1-199]

## Open Questions (RESOLVED)

1. **Exact Phase 3 inputs and reconciliation contract — resolved.**
   - **Decision:** The implementation input boundary is the live inventory above. Health may be explicitly threaded from `HelixShell`; known-ID scientific receipts/evidence may be read through their existing native routes. No current source supplies a reconciled lifecycle, a contradiction comparator, shell-wide retained operation, resource/lease diagnostics, capability quality/review metadata, a shared six-view record, or a reviewed result. Each is intentionally represented as unavailable/read-only, not deferred for a backend/domain change. [VERIFIED: helix-desktop/src/shell/HelixShell.tsx:20-65,153-163; helix-desktop/src/shell/WorkspaceLayout.tsx:30-42; helix-desktop/src/hooks/useScientificOperation.ts:20-55; helix-desktop/src/hooks/useCommandEvidence.ts:29-73]
   - **Implementation consequence:** A local event, session-storage context, selection, or board label cannot supply missing authority. The board shows the evidence/recovery destination only and blocks every mutation channel. [VERIFIED: 03-CONTEXT.md:43-47; .planning/phases/02.1-scientific-workspace-ui-rework/02.1-RECONCILIATION-COMPARATOR-DISCOVERY.md:40-51]

2. **Primary-workspace approval/promotion disposition — resolved as a future human checkpoint, not an implementation unknown.**
   - **Decision:** Keep the legacy workspace comparator and do not select a final approval, rejection, or primary-workspace disposition during implementation. The disposition becomes available only after the installed-Tauri comparison covers setup, experiment, plan, run, results, and samples/analysis; each row has keyboard-only, screen-reader, outcome, and evidence-location records; and a scientist explicitly decides. [VERIFIED: 03-CONTEXT.md:51-59]
   - **Implementation consequence:** Automated tests, prototype evidence, or a visual preference cannot promote the board. Until the checkpoint produces a recorded human decision, the only valid state is pending with legacy comparator access retained. [VERIFIED: .planning/phases/03-clean-room-scientific-workflow-designer/03-04-PLAN.md:50-71]

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
- None.

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
| Unsupported board/reconciliation data contract | A renderer-local aggregate, guessed lifecycle, or new backend/domain endpoint | Existing authenticated native reads for known IDs, otherwise visible unavailable/read-only state | The current seam has no aggregate contract; local data cannot become Agent evidence. [VERIFIED: helix-desktop/src/shell/HelixShell.tsx:20-65,153-163; helix-desktop/src/shell/WorkspaceLayout.tsx:30-42] |

**Key insight:** Phase 3’s value is a safety-truthful, independently authored projection over existing records; it must not recreate Agent policy, persistence, or execution semantics in the renderer. [VERIFIED: .planning/phases/02.1-scientific-workspace-ui-rework/02.1-PHASE-3-RECOMMENDATION.md:34-36]

## Common Pitfalls

### Pitfall 1: Treating local health/lifecycle as authoritative reconciliation
**What goes wrong:** The board re-enables editing from cached renderer state, an unmatched event, or a stale health check.

**Why it happens:** Current health and lifecycle arrive on separate inputs, and the tracer explicitly says there is no complete reconciled board state on that path. [VERIFIED: .planning/phases/02.1-scientific-workspace-ui-rework/02.1-RECONCILIATION-COMPARATOR-DISCOVERY.md:11-16]

**How to avoid:** Treat the source expression `status.trim().toLowerCase() === "healthy"` and initial `healthy: null` as the health projection only. It is necessary but not sufficient when the requested lifecycle/evidence input has no source; unsupported or missing evidence remains read-only. [VERIFIED: helix-desktop/src/hooks/useAgentHealth.ts:14-19,23-40; helix-desktop/src/shell/WorkspaceLayout.tsx:30-42]

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

No board-aggregation skeleton is supplied: a `deriveBoardInteractionPolicy(evidence)` API, a board `evidence` object, and a generic `reference.id` record are not live contracts and would invent the exact inputs this correction prohibits. Use the explicit native hook/client return types in the Live Desktop Wiring Contract. Any new board-only policy type must have source-backed variants only; an unavailable input maps directly to read-only UI. [VERIFIED: helix-desktop/src/hooks/useAgentHealth.ts:4-8; helix-desktop/src/hooks/useScientificOperation.ts:5-18; helix-desktop/src/hooks/useCommandEvidence.ts:29-34]

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
| `@xyflow/react` | `^12.11.0` | Legacy `WorkflowBuilder` dependency | It is installed and used by the legacy builder, but it is not a Phase 3 scientific/read-model contract and this research does not prescribe it for the new board. [VERIFIED: helix-desktop/package.json:17-22; helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.tsx:1-19] |
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
| Existing React/installed dependencies | Any new package | Do not install packages in Phase 3; current plans and this research require lockfile-backed dependencies only. [VERIFIED: .planning/phases/03-clean-room-scientific-workflow-designer/03-01-PLAN.md:104-106; .planning/phases/03-clean-room-scientific-workflow-designer/03-02-PLAN.md:88-90; helix-desktop/package.json:13-36] |
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
Fail-closed board policy <--- explicitly threaded native health observation
    |                         known-ID receipt/evidence reads when a source ID exists
    |                         unsupported lifecycle/diagnostic/view input -> Unavailable
    | editable only for source-backed policy inputs; otherwise read-only strip -> evidence/recovery view
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

### Pattern 1: Source-bounded fail-closed board policy
**What:** Create one pure, exhaustively tested policy only over source-backed inputs from the Live Desktop Wiring Contract. It must return read-only/unavailable for absent, local-only, malformed, partial, or unsupported inputs; it is the sole source of `disabled`, command interception, inspector editability, and route-change gating.

**When to use:** Every board action and navigation path, including pointer actions, keyboard actions, shortcuts, palette addition, inspector edits, and selection/route changes.

**Evidence:** The existing Phase 02.1 tracer requires the exact-supported healthy observation for editability and all other known/unknown conditions to fail closed. The live desktop correction further establishes that health is not currently forwarded to `WorkspaceLayout`, while lifecycle, contradiction, and reconciliation inputs lack a shared authoritative source. [VERIFIED: .planning/phases/02.1-scientific-workspace-ui-rework/02.1-RECONCILIATION-COMPARATOR-DISCOVERY.md:40-47; helix-desktop/src/shell/HelixShell.tsx:20-65,153-163; helix-desktop/src/shell/WorkspaceLayout.tsx:30-42]

**Verbatim state evidence:** `HelixShell` currently maps runspace events through:
`'runspace-started': 'Running', 'runspace-paused': 'Paused', 'runspace-completed': 'Completed', 'runspace-aborted': 'Aborted', 'runspace-faulted': 'Held'`. [VERIFIED: helix-desktop/src/shell/HelixShell.tsx:48-60]

### Pattern 2: Resolver-gated context changes
**What:** Treat a requested revision, route, or procedure change as pending intent. If the proposal is dirty, open an accessible modal with Save, Discard, and Keep editing. Save must complete/acknowledge before applying the requested target; Discard clears only current local draft; Keep editing cancels the requested change and retains the same draft context.

**When to use:** Any board navigation that changes scientific identity or revision context.

**Why:** The inherited procedure/plan UI has individual mutation state but no general dirty-context resolver; Phase 3 needs the centralized guard rather than a silent change. [VERIFIED: helix-desktop/src/components/ScientificSetup/ProcedurePlans.tsx:20-30,32-58; .planning/phases/02.1-scientific-workspace-ui-rework/02.1-WORKSPACE-BOARD-CONTRACT.md:20-23]

### Pattern 3: Projection where a source exists; unavailable where it does not
**What:** Board cards/nodes render exact server/Agent identities, revisions, frozen references, custody/history, provenance, and evidence status only when a current native read returns them. Local selection and layout are presentational; they never become the scientific record or execution state. The current shared layout does not supply a six-view board record, so each unsupported view/field is explicitly unavailable rather than synthesized.

**When to use:** Each of setup, experiment, plan, run, results, and samples-analysis projections; source-backed values pass through unchanged, while no-source values stay unavailable/read-only.

**Example:** `FrozenAssembly` constructs a list from exact apparatus, plan, plan-pinned procedure, policy revision, and preview-token values and states that later publications do not update the assembly or start a campaign. [VERIFIED: helix-desktop/src/components/ScientificSetup/FrozenAssembly.tsx:36-45,64-88]

### Anti-Patterns to Avoid
- **Restoring Cards/Graph primary modes:** violates D-01/D-04; the board is the only primary representation. [VERIFIED: 03-CONTEXT.md:11-18]
- **Renderer-local reconciliation:** conflicts must remain explicit; local events/caches cannot establish safety. [VERIFIED: .planning/phases/02.1-scientific-workspace-ui-rework/02.1-RECONCILIATION-COMPARATOR-DISCOVERY.md:11-16,40-43]
- **Embedding execution controls in the board:** mounting `ExecutionPanel`, direct client controls, or driver capability calls violates proposal-only scope. [VERIFIED: 03-CONTEXT.md:20-26]
- **Using the old `WorkflowBuilder` unchanged:** it auto-exports canvas changes and writes local workflow storage, so it is an implementation reference, not the Phase 3 scientific-record model. [VERIFIED: helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.tsx:137-164]
