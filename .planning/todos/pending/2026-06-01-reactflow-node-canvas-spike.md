---
created: 2026-06-01T00:00:00Z
title: Spike — ReactFlow Migration + Blender/UE Node Canvas Theme
area: ui
priority: high
seed: SEED-005
files:
  - helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.tsx
  - helix-desktop/src/components/WorkflowBuilder/useReteEditor.ts
  - helix-desktop/src/components/WorkflowBuilder/nodes/HelixNodes.ts
  - helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.test.tsx
  - helix-desktop/package.json
---

## Goal

Validate the ReactFlow migration path before committing to a full phase plan.
Produce a working prototype of the new node canvas with:
- ReactFlow replacing Rete.js
- Dark Blender/UE-style theme matching the app palette
- At least 3 node types ported (Step/DeviceAction, Condition, Branch)
- Minimap + free pan/zoom working
- `exportToWsl()` producing identical WslDocument output
- Animated edge (amber travelling dot) on a manually-triggered "running" state

## Why This Spike First

Research (2026-06-01) confirmed migration cost is 2–4 days and WslDocument
schema needs zero changes. The spike validates the theme and animated edge
quality before the full phase is planned — these are the two highest visual-
risk elements that cannot be fully evaluated from docs alone.

## Spike Tasks

1. `npm install reactflow` — remove `rete`, `rete-area-plugin`, `rete-react-plugin`, `rete-connection-plugin`, `rete-context-menu-plugin`
2. Replace `useReteEditor.ts` with `useHelixFlow.ts` — `useState` nodes/edges, `useReactFlow()` hook
3. Port `StepNode`, `ConditionNode`, `BranchNode` to ReactFlow custom node components
4. Apply dark theme: `#141414` background, `#1e1e1e` node bodies, accent-colour handles
5. Add `<MiniMap />` component
6. Implement animated edge: SVG `<animateMotion>` with amber circle on `animated: true` edges
7. Rewrite `exportToWsl()` to read from `getNodes()/getEdges()`
8. Confirm existing `WorkflowBuilder.test.tsx` pattern still works (or rewrite for ReactFlow)

## Success Criteria

- [ ] `dotnet build` + Vite build pass with no errors
- [ ] Canvas renders with dark theme, minimap visible, pan/zoom working
- [ ] StepNode shows capability text field; ConditionNode shows true/false handles
- [ ] `exportToWsl()` output JSON is identical structure to current implementation
- [ ] Setting `animated: true` on an edge shows the amber travelling dot animation
- [ ] All existing WorkflowBuilder tests pass (rewritten if necessary)

## Notes

- The spike does NOT need to implement the device palette, hybrid ports, or live
  execution state — those are validated separately in the full phase plan.
- If the animated edge quality is unsatisfactory with `<animateMotion>`, evaluate
  the Web Animations API `offsetDistance` approach as an alternative.
- See SEED-005 for full design spec.
