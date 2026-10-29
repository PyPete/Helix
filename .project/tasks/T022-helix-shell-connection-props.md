---
id: T022
title: Expose HelixShell connection as an App-owned composition contract
wave: 7
deps: [T003]
status: done
agent: build_T022
base: 477f85f03f11f1d22c2ade66296b7a65a78455df
worktree: null
task_branch: null
files:
  - helix-desktop/src/shell/HelixShell.tsx
---

# T022 — Expose HelixShell connection as an App-owned composition contract

## Context

Repair task created from the wave 2 cycle 1 review of T003 for the out-of-batch HelixShell fix site.
The failed criteria and every reviewer observation are recorded verbatim under Review findings.
The source task contracts are unchanged; read them and .project/intent/INTENT.md before editing.

## Approach

- Make HelixShell a controlled composition surface only; do not add device commands, lease behavior, or later-command behavior.
- Re-run the source task's Verify commands after the fix and record the result in the Log.

## Interface contract

`HelixShell` accepts optional controlled props `connection?: AgentConnection` and `onConnectionChange?: (connection: AgentConnection) => void`. With both props supplied, it uses the supplied connection and reports saves through the callback; App always supplies both after T021. For zero-prop compatibility while T003's existing App remains unchanged, it defaults to `DEFAULT_CONNECTION` and a no-op save callback. It does not retain shell-owned mutable connection-selection state.

## Intent coverage

- None

## Acceptance criteria

1. With both optional controlled props supplied, HelixShell renders from the supplied `AgentConnection` and reports ConnectionConfig saves through `onConnectionChange`.
2. With zero props, HelixShell remains render-compatible with T003's unchanged App by using `DEFAULT_CONNECTION` and a no-op save callback; it has no private mutable connection-selection state.
3. All existing shell URL-derived behaviors remain based on the effective connection (the supplied connection when controlled, otherwise the default).
4. The shell remains projection-only and adds no device commands, lease behavior, or later-command behavior; this shell-only task does not alone satisfy SC1.

## Verify

```bash
git diff --check -- helix-desktop/src/shell/HelixShell.tsx
npm install --prefix helix-desktop && npm exec --prefix helix-desktop -- vitest run --root helix-desktop --config vitest.config.ts helix-desktop/src/campaign/CampaignWorkspace.test.tsx -t "^does_not_create_an_activity_for_an_unknown_identity$"
npm exec --prefix helix-desktop -- vitest run --root helix-desktop --config vitest.config.ts helix-desktop/src/campaign/CampaignWorkspace.test.tsx -t "^does_not_regress_pending_projection_from_an_older_revision$"
npm exec --prefix helix-desktop -- vitest run --root helix-desktop --config vitest.config.ts helix-desktop/src/App.test.tsx
```

Heavy: no

## Review findings

### sc1
Criterion: In the local `helix-desktop` campaign workspace, a scientist starts the bounded simulated activity and observes its durable campaign/activity identity, pending state, and Agent audit history.
- The configured-Agent surface is not delivered end to end: `helix-desktop/src/App.tsx:3-6,14` binds campaign actions/restoration to the default URL independently of the active shell connection (`helix-desktop/src/shell/HelixShell.tsx:23-25,157-160`). A scientist configured to a non-default local Agent cannot start/observe that Agent's activity through this workspace; default-Agent presence can instead show/create the wrong Agent's activity. Rendered identities, pending state and Agent audit at `CampaignWorkspace.tsx:157-188` do not repair this routing failure. Fix by sharing the selected Agent connection and isolating workspace identity/projection state per URL, with the non-default-URL start/remount regression described under AC1.

### t003_ac1
Criterion: A scientist can start the one bounded simulated activity from `helix-desktop` and see its durable campaign/activity identity, pending external-evidence lifecycle, and Agent audit history (SC1).
- A scientist can start the one bounded simulated activity from `helix-desktop` and see its durable campaign/activity identity, pending external-evidence lifecycle, and Agent audit history (SC1). — The workspace is reachable and its start/display implementation exists, but it does not use the configured Agent. `helix-desktop/src/App.tsx:3-6,14` always supplies `DEFAULT_CONNECTION` (`http://localhost:5080`). The isolated base's `helix-desktop/src/shell/HelixShell.tsx:23-25,157-160` keeps the operator-selected connection privately, and `helix-desktop/src/components/ConnectionConfig.tsx:24-27` saves it only through that shell's callback. Selecting another local Agent therefore leaves campaign POST/GET requests pointed at localhost:5080: they fail when that default Agent is absent, or address a different Agent when it is present. The PLAN surface entry explicitly requires a configured Agent URL (`.project/plan/PLAN.md:49`). The recorded App Verify passes shell presence, not this connection behavior; its connection component is mocked. fix: Lift the active Agent connection into a shared composition owner in `App.tsx`, supply that same connection to the shell and campaign workspace, and key/remount the campaign workspace by Agent URL so identities and in-flight projections cannot cross Agents. Include the necessary `HelixShell.tsx` prop/callback change in the repair's declared scope. Add an App integration regression that saves a non-default local Agent URL, starts the activity, remounts, and asserts that both the create and identity GET use that URL and never the default endpoint.

## Log

- 2026-10-29 — created by Plan recovery to expose the out-of-batch HelixShell controlled-connection contract.
- 2026-10-29 — implemented controlled HelixShell connection props with zero-prop defaults and no shell-owned connection selection state.
- 2026-10-29 — Verify passed: `git diff --check -- helix-desktop/src/shell/HelixShell.tsx`; both CampaignWorkspace focused source commands (1 passed each); App source command (5 passed).
- 2026-10-29 — orchestrator Verify (sidecar gsd-path-verify/task-t022-verify): pass, exit 0; output tail:
  ```
     Duration  1.77s (transform 65ms, setup 131ms, import 169ms, tests 108ms, environment 1.13s)
  
  
   RUN  v4.1.7 C:/Users/u643664/.gsd-path/projects/3b869b23b4749acc/73bee6b8d21f842f/verify/task-t022-verify/helix-desktop
  
  
   Test Files  1 passed (1)
        Tests  1 passed | 3 skipped (4)
     Start at  08:46:20
     Duration  1.70s (transform 69ms, setup 133ms, import 151ms, tests 139ms, environment 1.08s)
  
  
   RUN  v4.1.7 C:/Users/u643664/.gsd-path/projects/3b869b23b4749acc/73bee6b8d21f842f/verify/task-t022-verify/helix-desktop
  
  
   Test Files  1 passed (1)
        Tests  5 passed (5)
     Start at  08:46:24
     Duration  2.59s (transform 312ms, setup 129ms, import 1.03s, tests 138ms, environment 1.10s)
  ```
