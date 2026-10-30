# Spike Manifest

## Ideas

### ivoryos-scientific-workspace

Explore a scientific workspace informed by IvoryOS while preserving Helix's Agent authority and scientific integrity. Three comparisons were approved in risk order: workspace orientation first, protocol representation second, detail-panel placement third. Only 001 is built in this session. The original [TODO](../todos/spike-canvas-first-scientific-workspace.md) remains pending; research or automated tests cannot validate usability.

**Requirements:**
- Preferred direction recorded 2026-10-30: graph as the primary representation with an equally preserved card toggle; contextual inspector favored, with permanent panels retained as a comparison/advanced possibility. Canvas-first received positive visual feedback. User003 said "workflow graph and contextual inspector are quite nice". These are design preferences, not conclusive usability winners, approved production implementation, or tested safety/accessibility claims; all verdicts remain PENDING.
- User preference recorded 2026-10-30: workflow graph preferred, with easy switching to sequence cards. Both are first-class views of one canonical protocol; stable IDs, selection and saved/unsaved edits survive switching, and view changes never mutate control-flow semantics. This preference is not a validated usability winner or permanent exclusion of cards.
- Build 001a ribbon/dock and 001b canvas in the same interactive demo with identical data, capabilities and preserved state on switching. No predeclared winner.
- Then evaluate 002 sequence cards versus graph; then 003 contextual inspector versus permanent panels. Do not implement these comparisons inside 001.
- Isolate all code in `.planning/spikes`; vanilla HTML/CSS/JS modules, Node built-ins, no dependencies or product changes.
- All readings, progress and actions are simulation fixtures; never call an Agent, API or device. The real Agent remains sole execution authority.
- Preserve draft/run isolation, illustrative deterministic review, requested versus observed outcomes, uncertainty and reconciliation. Never imply silent replay is safe.
- Persistent execution/safety strip survives modes, variants and responsive layouts; blocked/uncertain states carry text as well as semantic symbols.
- Capture ISO/category/variant/scenario events, task durations and observer notes for export. Human review is required before any winner or production recommendation.
- 002 must share canonical stable step identities and dependencies across cards/SVG graph; explicitly label conditional branches and parallel lanes instead of flattening them into a single execution sequence.
- Numeric/unit edits, rename, add and linear reorder are draft-only and available in both representations. Confirm dirty fixture replacement; never mutate reviewed fixtures or the frozen run.

## Spikes

| # | Idea | Name | Type | Validates | Verdict | Tags |
|---|---|---|---|---|---|---|
| 001a / 001b | ivoryos-scientific-workspace | [Canvas workspace comparison](001-canvas-workspace-comparison/README.md) | comparison | Given shared apparatus and run fixtures, when the scientist completes identical tasks in ribbon/dock and canvas, then compare orientation, discoverability and truthful safety-state understanding | PENDING — built, awaiting human review | ui, simulation, safety, high-risk |
| 002a / 002b | ivoryos-scientific-workspace | [Protocol representation comparison](002-protocol-representation-comparison/README.md) | comparison | Given shared linear/branching/parallel proposals, when authored/reviewed in cards and graph, then compare clarity without hiding dependencies or execution uncertainty | PENDING — built, awaiting human review | ui, protocol, medium-risk |
| 003a / 003b | ivoryos-scientific-workspace | [Inspector layout comparison](003-inspector-layout-comparison/README.md) | comparison | Given linked device/step/sample/result records, when switching selection/layout with pending fields, then compare provenance clarity and retained edit/review context | PENDING — built, awaiting human review | ui, inspector, medium-risk |

## Review gate

001 received the user comment **"this looks really good"** on 2026-10-30: positive visual feedback, not a validated comparison. Limited parent-reported DOM smoke is recorded in its README. 002 received a graph preference with interchangeable cards, recorded verbatim in its README; this is not a validated winner. The introductory idea paragraph preserves the original capture. All three comparisons are now built, with human comparative review still pending. This session built only 003; the original TODO remains pending.

Launch with `node .planning/spikes/server.mjs`. Record head-to-head task observations and export logs before a verdict. Build completion is not experiential validation. Wrap-up was read but is deferred: no findings skill or production blueprint should be created from pending usability evidence.

The canonical TODO now records the three prototype links and feedback; construction is complete, but detailed comparative evaluation, human task timings and a separately approved production plan remain pending. Parent-reported independent 003 regression/DOM smoke is attributed in its README, not treated as comprehensive usability evidence.
