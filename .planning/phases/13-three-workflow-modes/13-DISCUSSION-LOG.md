# Phase 13: Three Workflow Modes - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-09-05
**Phase:** 13-three-workflow-modes
**Areas discussed:** viewMode persistence, Kanban data model, Start/Stop mode scope, Mode-switch conflict UX

---

## viewMode persistence

| Option | Description | Selected |
|--------|-------------|----------|
| WslDocument.viewMode field | Mode is part of the document — saved/loaded with it. Additive schema change. | ✓ |
| Tauri Store per document name | UI preference stored separately; risk of sync issues on rename. | |
| HelixShell in-memory only | Ephemeral; resets to Node-Flow on reload. | |

**User's choice:** WslDocument.viewMode field
**Notes:** —

---

### viewMode default

| Option | Description | Selected |
|--------|-------------|----------|
| node-flow | Existing documents open in Node-Flow — no visual surprise. | ✓ |
| start-stop | Existing documents open in operator mode. | |

**User's choice:** node-flow

---

### Mode switch persistence timing

| Option | Description | Selected |
|--------|-------------|----------|
| Immediately on mode click | viewMode updated in-memory on every switch; written on next Save. | ✓ |
| Only on explicit Save | Mode reverts if user closes without saving. | |

**User's choice:** Immediately on mode click

---

## Kanban data model

### Lane metadata location

| Option | Description | Selected |
|--------|-------------|----------|
| WslNode.parameters["lane"] | Additive; compiler ignores it; round-trippable. | ✓ |
| Top-level WslDocument.metadata dict | Keeps nodes clean; adds top-level schema field. | |
| Ephemeral Kanban state only | Simplest; can't round-trip back to Kanban correctly. | |

**User's choice:** WslNode.parameters["lane"]

---

### Block add interaction

| Option | Description | Selected |
|--------|-------------|----------|
| Drag from Task Library dock only | Controlled authoring; matches J5. | |
| Drag OR click-to-add in lane | Also supports + button in lane with inline capability picker. | ✓ |
| You decide | — | |

**User's choice:** Drag OR click-to-add in lane

---

### Block visual

| Option | Description | Selected |
|--------|-------------|----------|
| Compact card: icon + label + collapsed params | Dense, scannable; chevron expands params inline. | ✓ |
| Full card: always-expanded params | Readable but visually overwhelming. | |
| You decide | — | |

**User's choice:** Compact card: icon + label + collapsed params

---

### Kanban execution overlay

| Option | Description | Selected |
|--------|-------------|----------|
| No execution overlay — run via Start/Stop mode | Authoring-only in Phase 13. | ✓ |
| Kanban shows execution state inline | Running/done/error on each card; doubles execution surface. | |

**User's choice:** No execution overlay in Kanban — run via Start/Stop mode

---

## Start/Stop mode scope

### Primary purpose

| Option | Description | Selected |
|--------|-------------|----------|
| Operator execution view only | No editing; workflow name, Start/Stop/Pause, step log, event log. | ✓ |
| Simplified authoring + execution | Also lets users select a workflow and configure top-level params. | |

**User's choice:** Operator execution view only

---

### Step progress log format

| Option | Description | Selected |
|--------|-------------|----------|
| Node label + status + timestamp | [timestamp] Step "label" — Running / Done / Error. Clean tailing list. | ✓ |
| Full event log | Raw RunspaceEvent stream. | |
| You decide | — | |

**User's choice:** Node label + status + timestamp

---

### Execution delegation

| Option | Description | Selected |
|--------|-------------|----------|
| Delegates to ExecutionPanel logic (useExperiment hook) | Consistent with Phase 12 ExperimentId model; no new plumbing. | ✓ |
| Direct runspace submission | Bypasses experiment scope; breaks Phase 12 model. | |
| You decide | — | |

**User's choice:** Delegates to useExperiment hook + POST /experiments

---

## Mode-switch conflict UX

### Node-Flow → Kanban complex structures

**Initial question:** What happens when switching Node-Flow → Kanban with complex structures?

**User's clarification:** "Is there any nested structure in kanban? Or can a loop be linear styled? So it still repeats but shown linear?"

This prompted a clarifying sub-question about loop representation.

---

### Loop representation in Kanban

| Option | Description | Selected |
|--------|-------------|----------|
| Loop card — collapsible block with steps inside | Repeat-count field + expandable body of sequential child cards. | ✓ |
| Loop card — flat, no nesting | Steps flattened into main lane; loses grouping. | |
| Loop goes to Staging (treat same as Branch) | Overly aggressive; users expect simple loops to work. | |

**User's choice:** Loop card — collapsible block with steps inside (repeat-count + expandable body)

---

### Staging area for incompatible nodes (Branch, Parallel)

| Option | Description | Selected |
|--------|-------------|----------|
| 4th read-only swimlane labeled "Unrepresentable" | Nodes listed with warning icon + tooltip. User can delete or switch back to Node-Flow. | ✓ |
| Toast/banner warning only — nodes hidden | Simpler; user can't see which nodes are affected without switching. | |

**User's choice:** 4th read-only swimlane labeled "Unrepresentable"

---

### Kanban → Node-Flow edge generation

| Option | Description | Selected |
|--------|-------------|----------|
| Sequential edges within lanes, lanes chained pre→main→post | Block order = edge order. Matches J6. | ✓ |
| You decide | — | |

**User's choice:** Sequential edges between blocks within each lane, lanes connected pre→main→post

---

## Claude's Discretion

- Panel IDs, exact CSS class names, Loop card expand/collapse interaction details
- Task Library dock layout
- `+` button inline picker design
- Kanban lane header styling
- "Unrepresentable" lane icon choices
- Mode label format in document tab header

## Deferred Ideas

- Kanban execution overlay (running/done/error on cards) — consider Phase 14 polish
- ReactFlow canvas migration — Phase 14
- SciChart integration — Phase 15
- Kanban condition builder (J5) — planner to assess Phase 13 scope
- Graceful device loss in Kanban (card invalid state) — planner to assess scope
