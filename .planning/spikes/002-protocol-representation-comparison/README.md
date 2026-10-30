---
spike: "002"
idea: ivoryos-scientific-workspace
name: protocol-representation-comparison
type: comparison
variants: [002a-sequence-cards, 002b-workflow-graph]
validates: "Given shared linear, branching and parallel protocol proposals, when a scientist authors/reviews them in cards and graph, then compare clarity and dependency, draft/run and uncertainty comprehension without preselecting a winner."
verdict: PENDING
status: built-awaiting-human-review
related: ["001", "003"]
tags: [ui, protocol, simulation, safety, comparison]
created: 2026-10-30
---

# Spike 002: The same intent. Two ways to read it.

## What This Validates

**Given** identical canonical linear/branching/parallel proposals, **when** a scientist selects, renames, edits, adds and reviews steps in cards and graph, **then** collect evidence about discovery, authoring clarity and truthful interpretation of dependencies and draft versus frozen run. Automated checks cannot decide usability or a winner.

Both views share stable identities, selection, buffers, diagnostics, evidence, tasks and a permanent editor. Cards explicitly group exclusive branches and independent lanes; visual order is not a fabricated total execution order. The original SVG graph is a dependency projection, not an apparatus canvas. Inspector placement is held constant; 003 is not built.

## Research

Continues the approved [TODO](../../todos/spike-canvas-first-scientific-workspace.md), [manifest](../MANIFEST.md), [conventions](../CONVENTIONS.md) and [001](../001-canvas-workspace-comparison/README.md). Project AGENTS, prior model/styles, launcher and spike workflow were consulted before construction. Vanilla modules/Node remain the user-selected stack; no dependencies or reference repositories installed. All three builds were approved in order, but this dispatch stops at 002's human checkpoint.

001's exact user comment **"this looks really good"**, dated **2026-10-30**, is recorded in its README as positive visual feedback only, alongside separately attributed parent DOM smoke. Neither establishes a validated winner or completes head-to-head safety/usability trials.

### Pinned source ledger

Inspected **2026-10-30** via GitHub commit/tree metadata and raw HTTPS source. Revision **`d451defe20452ca5c11c49f38bc6ad291f4d52de`**, upstream commit date **2026-10-23T22:24:04Z**. These are source observations, not a live IvoryOS evaluation or comprehensive audit.

| Reference | Direct inspected source | Source versus spike inference |
|---|---|---|
| Edge sequence | [WorkflowEditor.tsx](https://github.com/ivoryos-ai/ivoryos-next/blob/d451defe20452ca5c11c49f38bc6ad291f4d52de/packages/shared-ui/src/WorkflowEditor.tsx) | Stable `SequenceBlock.id`, Prep/Main/Cleanup lists and `@hello-pangea/dnd` are source-grounded. Our lane/dependency model is a proposed Helix design, not a translation. |
| Shared vocabulary | [Toolbox.tsx](https://github.com/ivoryos-ai/ivoryos-next/blob/d451defe20452ca5c11c49f38bc6ad291f4d52de/packages/shared-ui/src/Toolbox.tsx) | Shared logic chips use different edge-list/cloud-canvas drag bindings. Our native buttons/selects copy neither chips nor drag implementation. |
| Cloud graph | [CloudWorkflowEditor.tsx](https://github.com/ivoryos-ai/ivoryos-next/blob/d451defe20452ca5c11c49f38bc6ad291f4d52de/cloud_frontend/src/components/CloudWorkflowEditor.tsx) | React Flow nodes/edges, true/false handles and cycle/reachability checks inform explicit exits/diagnostics. Topological SVG/HTML-button layout and fork/join fixtures are independently authored. |
| Root license | [LICENSE](https://github.com/ivoryos-ai/ivoryos-next/blob/d451defe20452ca5c11c49f38bc6ad291f4d52de/LICENSE) | Apache-2.0 at root is not blanket evidence for every subtree. |
| Cloud license | [cloud_frontend/LICENSE.md](https://github.com/ivoryos-ai/ivoryos-next/blob/d451defe20452ca5c11c49f38bc6ad291f4d52de/cloud_frontend/LICENSE.md) | FSL-1.1-ALv2 has competing-use restrictions and a future Apache license after the second anniversary. Preserve the distinction for later reuse. No upstream code/artwork copied. |

Repository: [ivoryos-next](https://github.com/ivoryos-ai/ivoryos-next). Original IvoryOS/IvoryOS-client remain contextual references in 001, not connected APIs here.

| Approach | Tool | Potential benefit | Risk | Status |
|---|---|---|---|---|
| Explicit grouped cards | Native HTML | Readable details and local dependencies | Mistaking branch/lane rows for successive steps | 002a built, unvalidated |
| Dependency graph | Original SVG/HTML buttons | Visible paths and joins | Density, scrolling and scaled text | 002b built, unvalidated |

**Decision:** build both over one canonical model, not differing capability sets. Earlier/later reorder works for linear fixtures in both; branch/fork insertion needs an explicit outgoing path. Graph fit trades readable scale for width visibility. Borrow only stable identity, shared vocabulary, labeled exits and diagnostic concepts. Draft/run isolation, ANY versus ALL semantics, quarantine, reconciliation and task exports are Helix-specific proposals, not execution authority.

## How to Run

From repository root, Node 20+, no installation:

```powershell
node .planning/spikes/server.mjs
```

- Comparison: <http://127.0.0.1:4317/002-protocol-representation-comparison/>
- Launcher: <http://127.0.0.1:4317/>
- Alternate port: `node .planning/spikes/server.mjs 4318`; stop with Ctrl+C.

Unchanged shared server binds localhost and exposes only spike files with host/method/traversal checks and same-origin CSP. Launcher now targets this actual folder. No Agent/API/device/AI calls, telemetry, remote assets or durable storage. Reload starts a fresh session; export before closing or replacing an edited draft.

```powershell
node --test .planning/spikes/002-protocol-representation-comparison/model.test.mjs
node --test .planning/spikes/001-canvas-workspace-comparison/model.test.mjs .planning/spikes/server.test.mjs
node --check .planning/spikes/002-protocol-representation-comparison/app.mjs
```

## What to Expect

- Toggle views without losing selection, saved/unsaved proposals, review status, uncertainty or tasks. Both use the same stable IDs, edges, editor and dependency ledger.
- Linear thermal aliquot, hypothetical evidence gate and independent preparation lanes. YES/NO are exclusive alternatives with ANY-path merge; parallel has ALL-dependency join. No observed execution is inferred.
- Select/rename/edit a step, inspect upstream/downstream, insert on a selected path and reorder a linear chain. Graph nodes are native buttons in SVG `foreignObject`; fit/readable width is optional.
- Save 55 °C, then try 95 °C, blank/nonfinite input or K. Invalid saved proposals stay visible and block UI review; no implicit unit conversion. Edits invalidate revision-bound review; unsaved buffers prevent review too.
- Cycle/dangling-dependency probes validate malformed copies only, never contaminating the canonical draft. These checks are not the Helix compiler or scientific ordering validation.
- Fixture replacement confirms dirty/unsaved data loss. Cancel preserves fields and selection; confirm changes only the draft, retaining run/log.
- Frozen SIM-RUN-014 remains Running at approved-fixture-r7 / 50 °C. Start is always disabled. Persistent simulation/policy/uncertainty strip remains; reconnect latches reconciliation, not readiness or replay.
- S-014 → SIM-RUN-014 / approved-fixture-r7 → RESULT-014 has synthetic-results.csv source label and mg/L result unit. Mapping remains quarantined/unreviewed; no actual CSV imported, accepted evidence or authorized YES decision.
- Native labeled controls, focus styling, text/symbol warnings and linear reorder buttons. Full keyboard, assistive-technology and narrow-viewport readability still need human review.

## Observability

Begin timed tasks, mark complete/blocked, save notes and export local JSON. Complete is observer-marked, not validated success. Recent 20 events display; export includes the full ISO/category/variant/fixture/revision log, notes and active/completed tasks.

Report contains session/schema/idea/spike identity, simulation flag, start/export dates, PENDING verdict, event/task/note/blocked and distinct-fixture counts, total/split variant/fixture durations, and task start/finish context/duration. Detached semantic snapshot includes draft/review revisions, buffers, validation/projection, independent run, uncertainty, pending replacement and probe. Notes render as text, not HTML. Timings measure the observer session, not equipment. Mid-task toggles/fixture changes are logged and are not clean single-view timing trials. End-to-end browser download verification is not claimed.

## Human Review Protocol

Counterbalance the first view; use identical fixtures/tasks. Separate clean single-view attempts from toggle-preservation checks. Record searches/misclicks, confidence, explanations, errors and completion, not speed alone. Export sessions and capture screenshots as useful; no winner or threshold is predefined.

| Given | When | Then / collect |
|---|---|---|
| Linear draft | Rename heat, propose 55 °C and toggle/select | Explain stable identity, retained fields and frozen 50 °C run |
| Linear chain | Move earlier/later in each view | Explain regenerated dependencies versus unvalidated scientific order |
| Branch | Trace YES/NO/merge; add wait on chosen path | Identify exclusivity and ANY merge; quarantine does not support YES |
| Parallel | Trace lanes/join; add within one lane | Identify independence and ALL join without a false total order |
| Dirty/unsaved proposal | Request fixture replacement; cancel, then confirm | Verify preservation and intentional draft-only replacement |
| Invalid proposal/probe | Attempt review and inspect diagnostics | Distinguish blocked review, copy-only probes and execution authority |
| Disconnected fixture | Toggle/reconnect | Locate reconciliation and disabled start; explain uncertainty |
| Either view | Keyboard navigation, narrow layout, graph fit/scroll | Record focus, readability, safety visibility and accessibility gaps |

## Investigation Trail

1. Re-grounded prior comparison, feedback, manifest/conventions and pending TODO. Inspected source before build; cloud licensing differs from root.
2. Built pure shared model and frozen source fixtures first. Explicit labels and ANY/ALL-preserving insertion prevent branch/parallel flattening.
3. Added buffers, revision review, dirty confirmation and frozen run isolation. Invalid numeric/name proposals stay visible; malformed probes use copies.
4. Built cards/original SVG with shared editor, topological positioning, fit, ledger, persistent safety and observer controls. Reused 001 visual language, not product code.
5. Initial targeted checks passed 16 model tests and 11 combined 001/server regressions; app syntax passed. Coverage includes IDs/projections, reorder/insertion, bad units/limits/cycles/dependencies, cancellation/buffers, run/review isolation, reconciliation and export timing/provenance.
6. Earlier implementation-session DOM smoke at 1000×700 exercised edit/toggle preservation, reorder, dirty confirmation/cancel, YES-path insertion, parallel lanes, invalid numeric review blocking, frozen 50 °C run and reconciliation latch; no document horizontal overflow. Native click returned `UnknownVizError`; DOM `.click()`/events substituted. This is not native pointer, visual, accessibility or scientist task verification.
7. Continuation console check on the same 002 document returned no warnings/errors, no truncation or dropped messages. Final targeted rerun is reported at delivery; no checks establish a winning representation.

## Results

**PENDING — built, awaiting human head-to-head review.** Tests prove limited simulation semantics; DOM smoke proves limited wiring. No scientist comparison dataset, validated winner, full visual/a11y audit or end-to-end download evidence claimed.

Constraints: small synthetic fixtures, fixed graph positions, no arbitrary edge editing/dragging, durable state, actual CSV import, reviewed analytical evidence, AI/backend/compiler/device execution, scientific order validation or Agent admission. Parallel resource/lease feasibility and actual recovery are out of scope. Fit can shrink text; larger fixtures/native interaction need further evidence.

**Next:** review 002 and export observations before proceeding to 003. Keep 003 planned and original TODO pending; do not manufacture findings or a production blueprint from build completion.
