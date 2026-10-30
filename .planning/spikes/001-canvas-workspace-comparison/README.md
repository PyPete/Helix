---
spike: "001"
idea: ivoryos-scientific-workspace
name: canvas-workspace-comparison
type: comparison
variants: [001a-ribbon-dock, 001b-canvas-first]
validates: "Given identical simulated apparatus and run state, when a scientist completes shared tasks in ribbon/dock and canvas-first workspaces, then compare orientation, discoverability and truthful understanding of execution/safety state without selecting a winner beforehand."
verdict: PENDING
status: built-awaiting-human-review
related: ["002", "003"]
tags: [ui, simulation, safety, apparatus, comparison]
created: 2026-10-30
---

# Spike 001: One experiment, two perspectives

## What This Validates

**Given** a shared synthetic fumehood, temperature draft, admitted run revision and lifecycle scenario, **when** the scientist locates apparatus, proposes an edit and interprets a blocked/pending action in each workspace, **then** collect evidence about discoverability, orientation, task completion and safety-state comprehension. Human observations decide whether either approach deserves further exploration. Automated tests cannot answer this usability question.

001a is a functional **ribbon/dock approximation**, not a reproduction of the current product shell. 001b is an **apparatus-centered canvas** with labeled selectable device cards. Both use the same model and fixed selection inspector, task capabilities, modes and execution strip. No sequence-versus-graph editor or inspector-placement comparison is implemented: those are approved later spikes 002 and 003.

## Research

### Prior check and re-grounding

Re-read project AGENTS and the pending [project TODO](../../todos/spike-canvas-first-scientific-workspace.md). No spikes directory, local spike manifest, conventions, nested planning AGENTS or findings skill was present at the pre-build check. Created MANIFEST/CONVENTIONS, then re-read both before implementation. The user explicitly selected vanilla modules and Node, overriding the product React stack for this disposable experiment. The three comparisons were already approved in order; no additional alignment decision was needed.

Local shell grounding: [`HelixShell.tsx`](../../../helix-desktop/src/shell/HelixShell.tsx) imports Ribbon, tracks an active ribbon tab/panel, and activates workspace tabs. Its working tree was already modified by other work; it was read only and not copied into this spike.

### Source ledger

Inspected **2026-10-30** through GitHub commit/tree APIs and pinned raw source over HTTPS. No repositories or dependencies were installed. Commit dates are upstream metadata, not dates of experiential validation. These are source-code observations, not a live IvoryOS usability review.

| Reference | Exact inspected source | Revision / commit date | Observation |
|---|---|---|---|
| ivoryos-next TopNav | [TopNav.tsx](https://github.com/ivoryos-ai/ivoryos-next/blob/d451defe20452ca5c11c49f38bc6ad291f4d52de/packages/shared-ui/src/TopNav.tsx) | `d451defe20452ca5c11c49f38bc6ad291f4d52de` / 2026-10-23T22:24:04Z | Shared compact page pills; active state, label/icon combinations, horizontal page-list overflow. Some narrow labels become tooltips. |
| ivoryos-next queue bar | [GlobalQueueBar.tsx](https://github.com/ivoryos-ai/ivoryos-next/blob/d451defe20452ca5c11c49f38bc6ad291f4d52de/frontend/src/components/GlobalQueueBar.tsx) | Same revision/date | Cross-page queue/run status, idle chip, pending count, pause/resume/cancel and failure decisions. Suppressed on run pages to avoid duplicate controls. Source uses optimistic state updates in `handleRunControl`. |
| ivoryos-next shared editor | [WorkflowEditor.tsx](https://github.com/ivoryos-ai/ivoryos-next/blob/d451defe20452ca5c11c49f38bc6ad291f4d52de/packages/shared-ui/src/WorkflowEditor.tsx) | Same revision/date | Inspected declarations, props and opening phase/editor logic: `SequenceBlock`, Prep/Main/Cleanup tabs, module toolbox and editable/reorderable blocks. Its “canvas” is workflow authoring, not an apparatus diagram. Whole large editor was not audited. |
| Original IvoryOS | [README](https://github.com/ivoryos-ai/IvoryOS/blob/1d0f0444f3525882432f86281dc80fa69689395b/README.md) and [base.html](https://github.com/ivoryos-ai/IvoryOS/blob/1d0f0444f3525882432f86281dc80fa69689395b/ivoryos/templates/base.html) | `1d0f0444f3525882432f86281dc80fa69689395b` / 2026-10-28T23:31:21Z | Python automation controls and drag/drop workflows; Workflow → Execution → Data navigation, instruments separated from pipeline; fixed global status icon in template. |
| IvoryOS-client — **API-only reference** | [README](https://github.com/ivoryos-ai/IvoryOS-client/blob/3082225c0c728837a0044985a3435f228827f7cb/README.md) | `3082225c0c728837a0044985a3435f228827f7cb` / 2026-09-23T07:32:27Z | Python HTTP client mirrors server controls; documents task-start versus task-result APIs. No UI pattern borrowed. No client calls made. |

Repository links: [ivoryos-next](https://github.com/ivoryos-ai/ivoryos-next), [original IvoryOS](https://github.com/ivoryos-ai/IvoryOS), [IvoryOS-client](https://github.com/ivoryos-ai/IvoryOS-client).

### Competing approaches and decision

| Approach | Tool | Potential benefit | Risk | Status |
|---|---|---|---|---|
| Ribbon / dock | Vanilla HTML/CSS; fixed device explorer and shared inspector | Stable tool/panel positions and explicit commands | Attention split among panels; more chrome | 001a built, not validated |
| Canvas-first | Same modules/model; labeled apparatus schematic with device cards | Relationships, readings and samples in spatial context | Tool discovery, scaling to complex apparatus, visual density | 001b built, not validated |

**Chosen approach:** build both in one demo. All run/draft/scenario/selection/task state survives switching; differences are presentation rather than capabilities. No winner preselected.

**Borrowed concepts:** compact scientific task navigation and always-available run/status awareness. **Proposed Helix-specific concepts:** apparatus canvas; explicit simulation provenance; draft/run revision isolation; immutable request candidate; requested-versus-observed status; reconciliation latch; review-aware evidence and task logging. No upstream code, artwork or dependencies were copied. Do **not** borrow optimistic “running” updates as physical confirmation, the original direct-control authority model, or an idle fallback for disconnected data. Keep navigation labels visible when narrow instead of relying on icon-only tooltips.

## How to Run

From the repository root, Node 20+; no installation:

```powershell
node .planning/spikes/server.mjs
```

- Launcher: <http://127.0.0.1:4317/>
- Comparison: <http://127.0.0.1:4317/001-canvas-workspace-comparison/>
- Alternate port: `node .planning/spikes/server.mjs 4318` (or `PORT` environment variable).
- Stop with Ctrl+C. The launcher enables future 002/003 links only if their proposed index paths exist; adjust those paths when their actual folders are chosen.

Static server binds **127.0.0.1 only**. Raw decoded traversal, backslashes/drive syntax, unexpected hosts, unsupported file types, outside-root symlinks and non-GET/HEAD requests are denied. It exposes only spike files, never repository files. CSP restricts scripts/styles/connectivity to this origin. The comparison makes no network calls; launcher only HEAD-checks local upcoming files. Opening the demo does not connect to an Agent.

Tests:

```powershell
node --test .planning/spikes/001-canvas-workspace-comparison/model.test.mjs
node --test .planning/spikes/server.test.mjs
node --check .planning/spikes/001-canvas-workspace-comparison/app.mjs
```

## What to Expect

- A restrained scientific UI with Apparatus / Design / Run / Evidence navigation; ribbon/dock baseline and canvas toggle; synthetic feed pump, reactor, recirculator and sample collector with units and selectable details.
- Shared heat editor, explicit illustrative review and a synthetic **untrusted AI proposal**. Saving 55 °C is valid under the demo's 20–80 °C rule; 95 °C, blank/nonfinite values or K/°F are invalid. Review is revision-bound. Neither edit nor review mutates the frozen run.
- Initial Running fixture with 40% static progress. Progress/readings are **not a live stream**, measurement or prediction. Scenario controls load snapshots, not observed device transitions.
- Ready, Running, Pausing, Paused, Held, Disconnected and Reconciliation-required fixtures. Disconnection marks all readings stale and latches uncertainty. Loading Running after Disconnected does **not** reconcile state: start/resume/retry remain blocked.
- A persistent execution/safety strip across modes/variants, including narrow layouts. Request pause/abort/start/resume never changes observed status until a distinct **SIMULATED observation**. Abort needs confirmation before even queueing the request. This is not an emergency stop.
- Start only from the NEW Ready fixture after valid illustrative review. Retry deliberately unavailable; Held is not equivalent to Paused. The NEW Ready control explicitly starts a new fixture, **not recovery or replay**.
- S-014 provenance is synthetic, and analytical evidence is quarantined/unreviewed. UI/AI cannot promote it to accepted evidence.
- Native keyboard controls, visible focus, text plus symbols, reduced-motion CSS, and a responsive layout. Saved model drafts and unsaved editor buffers survive presentation toggles; unsaved text is not reviewed or exported as an admitted draft. No localStorage: reload starts a new session; export before closing.

## Observability

Every model interaction records ISO time, category, variant, selected scenario, observed status, mode and event-specific data. Recent 20 events display on screen; export includes the entire log, notes, current snapshot, pending candidate, active task and completed attempts.

Export session JSON includes unique session ID, schema/version, idea/spike identity, simulation flag, start/export times, `PENDING` verdict, event/task/note/blocked counts, distinct scenario+observed-state count, total duration, split variant durations, task durations/outcomes and variant/scenario provenance for notes. Counts/timings measure the observer session, not equipment. Mid-task switching is recorded with start/finish variants; do not compare such attempts as clean single-variant timings. Blocked attempts remain evidence, not automatically failed usability tests. Observer text is rendered via `textContent`, never injected HTML. Downloads are local only; no telemetry.

## Human Review Protocol

Use the in-demo task guidance, same fixtures and same task capabilities in both variants. Counterbalance which variant is first; explicitly load a NEW Ready fixture when needed, not as recovery. Record elapsed time, misclicks/searches, confidence and explanation accuracy; do not choose solely on speed. Observe keyboard operation, small-screen safety visibility and whether draft/run/pending distinctions are understood. Capture screenshots and exported notes/logs. No threshold or winner is asserted before evidence.

| Given | When | Then / evidence to collect |
|---|---|---|
| Shared apparatus fixture | Locate R-201, trace feed/cooling and select S-401 in each variant | Record completion, errors, discovery paths and whether relationships are understood; screenshots + notes |
| Frozen run r7 and editable draft | Propose/review 55 °C then try 95 °C or K | Explain valid draft versus approved run; invalid policy visible, run unchanged; event export |
| Running fixture | Request pause, then confirm simulated observation | Observer identifies pending request versus observed Running, then observed Paused; screenshot both states |
| Active fixture | Open abort confirmation, cancel, then confirm request | Cancel preserves state; confirm queues only; observation separately marks Aborted; event export |
| Disconnected fixture | Switch variants/modes then load Running | Stale readings never imply readiness; uncertainty remains until an explicitly new fixture, no automatic resume/retry; screenshots |
| Sample S-014 | Inspect evidence in both variants | Source identity and quarantine are understandable; no accepted-result implication; observer explanation |

Record recommendation, rejected alternatives, trade-offs and unanswered questions **after review**. Do not mark the original TODO complete just because this artifact exists.

## Investigation Trail

1. Prior check found no spike infrastructure. Read approved TODO and scoped three-comparison requirements; created/re-grounded manifest and user-selected conventions.
2. Inspected pinned upstream source. Found that upstream workflow “canvas” denotes sequence authoring, so apparatus canvas is documented as a proposal, not a borrowed verified feature. Found optimistic queue updates; deliberately replaced them with request/observation separation.
3. Built pure shared simulation model first, then both views over it. Scenario changes invalidate pending candidates; disconnection latches reconciliation. Separate NEW Ready fixture is clearly not recovery.
4. Probed invalid units/limits, revision changes, edits after start request, Pausing/Held, abort confirmation, reconnect-without-reconciliation and structured log export. Model freezes a reviewed start candidate at request, preventing a later draft edit from changing the admitted observation.
5. Added static-root host/method/traversal checks and Node server tests. Preserved device keyboard focus on rerender and unsaved editor buffers across comparison switches.
6. Automated checks: first run passed 10 model tests plus 1 server-boundary test (11/11); final targeted rerun passed 10/10 model and 1/1 server tests with no failures. App/server/launcher syntax checks passed. Actual localhost GET smoke returned HTTP 200 for the comparison and launcher. A desktop-browser attempt returned `browser.disconnected`; no connected browser was available, so visual interaction/responsiveness/download verification is **not claimed**. Human review remains pending.

## Results

**PENDING — built, awaiting human review.** Automated model and server checks establish simulation semantics and static boundary behavior, not discoverability, accessibility conformance, UI rendering or superiority of either variant. No scientist observations, head-to-head timing dataset or screenshot evidence collected yet. Browser smoke was unavailable; test the page in a browser before drawing findings.

Constraints: small four-device fixture; no layout dragging/persistence, complex apparatus scaling, real protocol compiler, CSV import, durable execution, real safety admission, backend, real AI or hardware. Only a heat policy rule is implemented; real Agent checks remain mandatory. No actual reconciliation implementation: uncertainty is never cleared by reconnect. Confirmation buttons intentionally stand in for externally observed outcomes, labeled simulation throughout.

**Next:** human checkpoint on 001, then parent-orchestrated 002 (sequence cards vs graph), then 003 (inspector vs permanent panels). Preserve shared scientific/safety semantics. Wrap-up was read but deferred until experiential evidence exists; do not manufacture a validated findings skill or production blueprint from pending work.
