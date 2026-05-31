---
id: SEED-002
status: dormant
planted: 2026-05-30
planted_during: Phase 6 / 6.1 — Tauri Desktop App (milestone v1.0)
trigger_when: when relevant
scope: unknown
---

# SEED-002: Rete.js Workflow Editor — Execution Scopes + Strict Authoring Environment

## Why This Matters

_To be filled in. Run `/gsd:capture --seed --enrich SEED-002` to add context._

## When to Surface

**Trigger:** when relevant

This seed will surface during `/gsd:new-milestone` when the milestone scope matches.

## Scope Estimate

**Unknown** — run `/gsd:capture --seed --enrich SEED-002` to estimate effort.

## Design Summary

Two complementary design decisions for the Rete.js workflow editor:

---

### 1. Execution Scopes for Loops

Each loop node (For, While, ForEach) creates a **scope** — an isolated execution
context with its own iteration variables and local state. Nested loops create nested
scopes. No variable name collisions between loop levels.

**WSL serialisation:** scope IDs are assigned by the Rete.js editor and preserved in
the WslDocument. The Agent reconstructs the scope tree at execution time.

**Agent runtime:**
- Creates and manages scope objects during execution
- Scope lifecycle: enter → iterate → exit (with clean event logging per scope)
- Supports `break` and `continue` semantics within the scope model
- Deterministic iteration: scope state is fully serialisable and inspectable

**Debugging:** a scope inspector panel shows active scopes, their variables, and
current iteration index during live execution.

**Nested loop support:** arbitrary depth. Parent scope variables are readable from
child scopes (lexical capture), but child scopes cannot write to parent scope
variables (isolation).

---

### 2. Strict Workflow Authoring Environment

Scientists cannot create arbitrary nodes — the editor enforces a controlled vocabulary:

**Node creation:**
- Nodes are created **only** by dragging predefined task items from dock windows
  (Device Explorer, a Tasks panel, etc.)
- No right-click "add any node" — the context menu shows only task-library items
- This prevents unsupported or malformed workflow steps

**Parameter validation:**
- Numeric inputs use custom Rete.js controls with min/max/step/unit from
  `CapabilityDescriptor.ParameterSchema`
- Units are displayed and enforced (e.g. mL/min, °C)
- No free-text parameter entry for typed fields

**Conditional logic:**
- Condition nodes use a controlled condition builder (field + operator + value)
  rather than free-form expression strings
- Supported operators derived from the capability schema type (Numeric → <, >, =,
  between; Enum → is, is not; Boolean → is true, is false)

**Branch and If-Else:**
- Branch nodes can dynamically add/remove unlimited branches at authoring time
- If-Else nodes support one-to-many Else-If chains (not just binary if/else)
- All branches are named and validated before export

**Validation hooks:**
- Rete.js `addPipe` hooks validate node connections at edge-draw time
- Invalid connections (type mismatch, missing required input) are rejected with
  inline error indicators
- Export is blocked if the graph has validation errors

**Integration with docked workspace (SEED-001):**
- The workflow editor is one dock panel within the VS-style IDE shell
- The task library is a separate dock panel (like a toolbox) feeding the canvas
- Validation errors surface in the Output dock panel

---

## Breadcrumbs

- `helix-desktop/src/components/WorkflowBuilder/nodes/HelixNodes.ts` — existing 7
  node types (Step, Condition, Branch, Loop, Parallel, Delay, WaitFor); scope model
  would extend Loop here
- `helix-desktop/src/components/WorkflowBuilder/useReteEditor.ts` — ContextMenuPlugin
  already wired; strict authoring would replace/restrict the context menu items
- `Helix.Orchestration.Core/Capabilities/CapabilityDescriptor.cs` — ParameterSchema
  provides the type/min/max/step data needed for validated numeric controls
- `Helix.Orchestration.Core/WSL/WslCompiler.cs` — WSL compiler where scope semantics
  would be implemented for loop execution
- `.planning/seeds/SEED-001-ribbon-docking-workspace-ui.md` — the docked workspace
  this editor embeds into
- `.planning/sketches/002-docking-panel-model/` — docked panel layout sketches
- `.planning/spikes/001-allotment-basic-layout/` — validated allotment for the
  IDE shell that would host this editor

## Notes

Captured from two design notes describing:
1. The loop scope model (execution semantics for the Agent)
2. The strict authoring model (UX/validation for the Rete.js editor)

These are closely related — the strict authoring model produces the scope-annotated
WSL that the Agent executes. Consider planning them together as one phase or as
closely linked phases (editor authoring → Agent execution).

A spike on Rete.js custom controls (numeric input with ParameterSchema validation)
and on scope serialisation in WslDocument would be valuable before planning.