# Phase 13: Three Workflow Modes - Context

**Gathered:** 2026-09-05
**Status:** Ready for planning

<domain>
## Phase Boundary

Build three rendering modes for the Workflow Editor document tab in helix-desktop: **Start/Stop** (operator execution view), **Kanban** (SOP-writer swimlane editor), and **Node-Flow** (full ReactFlow graph). All three read/write the same `WslDocument` — WSL is canonical, mode is a view. Mode switching is non-destructive; the document is never transformed by switching, only re-rendered. Mode buttons live in Ribbon Home tab → View group. Mode label shown in document tab header.

No backend changes required. No SciChart or ReactFlow canvas migration in this phase (those are Phase 14/15).

</domain>

<decisions>
## Implementation Decisions

### viewMode Field
- **D-01:** Add `viewMode?: "node-flow" | "kanban" | "start-stop"` to `WslDocument` in `helix-desktop/src/types/agent.ts`. Additive — existing documents without the field are treated as `"node-flow"`. — **Reversibility:** costly — changes the canonical WSL serialisation contract; any saved `.wsl.json` file written after this change will carry the field; rolling back requires a migration or a schema version bump.
- **D-02:** Mode switch updates `viewMode` in the in-memory `WslDocument` immediately on click. The document is marked dirty and written to disk on the next explicit Save (same dirty-save pattern as other document edits).
- **D-03:** Default for missing/undefined `viewMode` is `"node-flow"`. All existing saved workflows open in Node-Flow.

### Kanban Data Model
- **D-04:** Kanban lane assignment stored in `WslNode.parameters["lane"]` with values `"pre"`, `"main"`, or `"post"`. `WslCompiler` is unaware of the `lane` key — it is filtered out before compilation. Node-Flow view may render a lane annotation label if it finds the key, but is not required to.
- **D-05:** Blocks can be added to a lane by **drag from Task Library dock** (primary) **or** by clicking a `+` button within a lane (secondary — opens a capability picker inline). Task Library dock lists device capabilities (from `DeviceRegistry`) plus flow control types (Loop, Delay, WaitFor).
- **D-06:** Kanban block visual: **compact card** — device icon + step label + collapsed parameter chevron. Parameters expand inline on click. Matches the Manual Control card pattern from sketch findings (dense, dark IDE aesthetic, `--color-surface` background, `--color-primary` accent).
- **D-07:** **No execution overlay in Kanban mode** in Phase 13. Kanban is authoring-only. To run a Kanban-authored workflow, user switches to Start/Stop mode.

### Start/Stop Mode
- **D-08:** Start/Stop is an **operator execution view** — no editing. Shows: workflow name + current phase (pre/main/post), large Start / Stop / Pause controls, live step progress log (tailing list), event log.
- **D-09:** Step progress log format: `[timestamp] Step "{label}" — Running / Done / Error`. Driven by `RunspaceEvent` stream from `useAgentWebSocket`. No raw event dump — filtered to step-relevant event types only.
- **D-10:** Start/Stop delegates to **`useExperiment` hook** for all experiment lifecycle operations (POST /experiments, Start/Stop/Pause). No new execution plumbing. Consistent with Phase 12 ExperimentId model.

### Mode-Switch Conflict Handling
- **D-11:** Loops in Node-Flow → Kanban: `Loop` nodes become **Loop cards** in Kanban — a collapsible block with a repeat-count field and an expandable body of sequential child cards. Single-body loops are representable in Kanban.
- **D-12:** Truly incompatible nodes (Branch, Condition, Parallel) → placed in a **4th read-only swimlane labeled "Unrepresentable"** below pre/main/post. Nodes listed with a warning icon + tooltip explaining why. User can switch back to Node-Flow to edit them, or delete from Kanban.
- **D-13:** Kanban → Node-Flow edge generation: sequential edges within each lane in block order, lanes chained `pre → main → post`. WslCompiler receives a fully wired WslDocument identical to one authored directly in Node-Flow.

### Claude's Discretion
- Panel IDs, exact CSS class names, Loop card expand/collapse interaction details, Task Library dock layout, `+` button inline picker design, Kanban lane header styling, Unrepresentable lane icon choices, mode label format in document tab header.

### Folded Todos
- **Multi-Mode Workflow Authoring (UI Architecture)** (`.planning/todos/pending/2026-05-30-multi-mode-workflow-authoring.md`) — Core of Phase 13. Architecture described in the todo (shared WslDocument, mode-specific React components, CapabilityRegistry-driven authoring surfaces) is adopted directly.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` Phase J section — J1–J8 requirements (Three Workflow Modes)
- `.planning/ROADMAP.md` Phase 13 entry — Goal, success criteria, dependencies

### Design System
- `.claude/skills/sketch-findings-Helix/SKILL.md` — Design direction summary; CSS variable index
- `.claude/skills/sketch-findings-Helix/references/layout-and-chrome.md` — Ribbon patterns, document tab structure, chrome HTML skeleton
- `.claude/skills/sketch-findings-Helix/references/manual-control-ui.md` — Compact card pattern (reference for Kanban block visual design)
- `.claude/skills/sketch-findings-Helix/sources/themes/default.css` — All CSS custom properties

### Existing Integration Points
- `helix-desktop/src/types/agent.ts` — `WslDocument`, `WslNode`, `WslEdge` — extend with `viewMode` field here
- `helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.tsx` — Current Node-Flow editor; becomes one rendering branch under the mode router
- `helix-desktop/src/hooks/useExperiment.ts` — Start/Stop mode delegates all experiment lifecycle calls here
- `helix-desktop/src/hooks/useAgentWebSocket.ts` — Provides `runspaceEvents` for Start/Stop step progress log
- `helix-desktop/src/shell/panels/DeviceExplorer.tsx` — Task Library dock references same device list
- `helix-desktop/src/shell/Ribbon.tsx` — Mode buttons wired in Ribbon Home → View group

### Prior Phase Context
- `.planning/phases/11-ribbon-docking-workspace-shell/11-CONTEXT.md` — Established shell architecture (rc-dock, document tabs, CSS variable layer)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `useExperiment` hook — provides `startExperiment`, `stopExperiment`, `pauseExperiment` for Start/Stop mode; no new plumbing needed
- `useAgentWebSocket` — provides `runspaceEvents: RunspaceEvent[]`; Start/Stop filters these for step progress log
- `WorkflowBuilder.tsx` — becomes the Node-Flow rendering branch; its `onExport` prop and `WslDocument` state are the shared document interface
- `DeviceRegistry.tsx` / `DeviceAdapter` type — provides device + capability data for Task Library in Kanban mode
- Compact card pattern from `DeviceManualControl` — design reference for Kanban block cards

### Established Patterns
- `WslDocument` is the canonical shared state; all mode components read/write it via callbacks (not internal state)
- TypeScript strict mode; all new components and hooks must type-check
- Vitest co-located tests (`{Component}.test.tsx`) — each new mode component needs tests
- `vi.hoisted()` + explicit mock patterns required for Vitest
- CSS custom properties via `var(--color-*)` — no hardcoded hex values
- Named exports for sub-components and hooks

### Integration Points
- Workflow Editor document tab in `WorkspaceLayout.tsx` — hosts the mode router component
- `Ribbon.tsx` Home tab → View group — mode toggle buttons (Start/Stop / Kanban / Node-Flow)
- Document tab header in `WorkspaceLayout.tsx` — shows current mode label
- `saveWorkflow` / `loadWorkflow` in `helix-desktop/src/lib/workflowLibrary.ts` — WslDocument serialization already exists; `viewMode` field is additive and will serialize automatically

</code_context>

<specifics>
## Specific Ideas

- Loops in Kanban as collapsible blocks: scientist sees a "Loop (N times)" header with child step cards indented underneath. Chevron expands/collapses the body. This keeps Kanban scannable without sacrificing loop semantics.
- The "Unrepresentable" staging lane should make it easy to switch back: a "Switch to Node-Flow to edit" link or button in the lane header.
- Mode label in document tab: `[NodeFlow] My Workflow` / `[Kanban] My Workflow` / `[Start/Stop] My Workflow` — or use a small icon prefix.

</specifics>

<deferred>
## Deferred Ideas

- **Kanban execution overlay** (running/done/error state on cards) — Phase 13 authoring-only; consider for Phase 14 or as a polish item.
- **ReactFlow canvas migration** — Phase 14; WorkflowBuilder stays on the Phase 12 ReactFlow build in Phase 13.
- **SciChart in chart panels** — Phase 15.
- **Kanban condition builder** (J5 controlled condition builder on conditional blocks) — noted in requirements as Must Have; planner to assess scope within Phase 13 or flag if needs its own plan.
- **Graceful device loss in Kanban** (device offline → card invalid state) — described in the folded todo; planner to assess fit in Phase 13.

### Reviewed Todos (not folded)
- **ReactFlow Node Canvas Spike** (`.planning/todos/pending/2026-06-01-reactflow-node-canvas-spike.md`) — Phase 14 (ReactFlow migration). Phase 13 uses the existing ReactFlow-based WorkflowBuilder as-is.
- **Graceful Device Loss & Removal** (`.planning/todos/pending/2026-05-30-graceful-device-loss-removal.md`) — Partially relevant (Kanban card invalid state when device offline); deferred for planner to assess scope.

</deferred>

---

*Phase: 13-three-workflow-modes*
*Context gathered: 2026-09-05*
