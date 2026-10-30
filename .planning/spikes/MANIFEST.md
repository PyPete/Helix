# Spike Manifest

## Ideas

### ivoryos-scientific-workspace

Explore a scientific workspace informed by IvoryOS while preserving Helix's Agent authority and scientific integrity. Three comparisons were approved in risk order: workspace orientation first, protocol representation second, detail-panel placement third. Only 001 is built in this session. The original [TODO](../todos/spike-canvas-first-scientific-workspace.md) remains pending; research or automated tests cannot validate usability.

**Requirements:**
- Build 001a ribbon/dock and 001b canvas in the same interactive demo with identical data, capabilities and preserved state on switching. No predeclared winner.
- Then evaluate 002 sequence cards versus graph; then 003 contextual inspector versus permanent panels. Do not implement these comparisons inside 001.
- Isolate all code in `.planning/spikes`; vanilla HTML/CSS/JS modules, Node built-ins, no dependencies or product changes.
- All readings, progress and actions are simulation fixtures; never call an Agent, API or device. The real Agent remains sole execution authority.
- Preserve draft/run isolation, illustrative deterministic review, requested versus observed outcomes, uncertainty and reconciliation. Never imply silent replay is safe.
- Persistent execution/safety strip survives modes, variants and responsive layouts; blocked/uncertain states carry text as well as semantic symbols.
- Capture ISO/category/variant/scenario events, task durations and observer notes for export. Human review is required before any winner or production recommendation.

## Spikes

| # | Idea | Name | Type | Validates | Verdict | Tags |
|---|---|---|---|---|---|---|
| 001a / 001b | ivoryos-scientific-workspace | [Canvas workspace comparison](001-canvas-workspace-comparison/README.md) | comparison | Given shared apparatus and run fixtures, when the scientist completes identical tasks in ribbon/dock and canvas, then compare orientation, discoverability and truthful safety-state understanding | PENDING — built, awaiting human review | ui, simulation, safety, high-risk |
| 002a / 002b | ivoryos-scientific-workspace | Sequence cards versus graph | comparison | Given equivalent linear and branching protocols, when authored/reviewed in both views, then compare clarity without hiding control flow | PENDING — planned, not built | ui, protocol, medium-risk |
| 003a / 003b | ivoryos-scientific-workspace | Contextual inspector versus permanent panels | comparison | Given device/step/sample selection, when inspecting and switching context, then compare discoverability and retained review context | PENDING — planned, not built | ui, inspector, medium-risk |

## Review gate

Launch with `node .planning/spikes/server.mjs`. Record head-to-head task observations and export logs before a verdict. Build completion is not experiential validation. Wrap-up was read but is deferred: no findings skill or production blueprint should be created from pending usability evidence.
