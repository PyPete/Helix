---
id: T021
title: Fix wave 2 cycle 1 review findings in T003
wave: 7
deps: [T003, T022]
status: done
agent: build_T021
base: 12071c45850c50f59f1191ae84ca01af3ce33d53
worktree: null
task_branch: null
files:
  - helix-desktop/src/App.test.tsx
  - helix-desktop/src/App.tsx
  - helix-desktop/src/campaign/CampaignWorkspace.test.tsx
  - helix-desktop/src/campaign/CampaignWorkspace.tsx
  - helix-desktop/src/campaign/agentCampaignClient.ts
---

# T021 — Fix wave 2 cycle 1 review findings in T003

## Context

Repair task created from the wave 2 cycle 1 review of T003.
The failed criteria and every reviewer observation are recorded verbatim under Review findings.
The source task contracts are unchanged; read them and .project/intent/INTENT.md before editing.

## Approach

- Fix only what the Review findings name, inside the listed files; do not change the source
  tasks' acceptance criteria or interface contracts.
- App owns the selected `AgentConnection`, passes it to HelixShell, derives CampaignWorkspace
  `baseUrl` from that same value, and keys/remounts CampaignWorkspace by URL.
- Re-run the source tasks' Verify commands after the fix and record the result in the Log.

## Interface contract

`HelixShell` accepts optional controlled props `connection?: AgentConnection` and `onConnectionChange?: (connection: AgentConnection) => void`. With both props supplied, it uses the supplied connection and reports saves through the callback; App always supplies both after T021. For zero-prop compatibility while T003's existing App remains unchanged, it defaults to `DEFAULT_CONNECTION` and a no-op save callback. It does not retain shell-owned mutable connection-selection state.

## Intent coverage

- None

## Acceptance criteria

1. In the local `helix-desktop` campaign workspace, a scientist starts the bounded simulated activity and observes its durable campaign/activity identity, pending state, and Agent audit history.
2. A scientist can start the one bounded simulated activity from `helix-desktop` and see its durable campaign/activity identity, pending external-evidence lifecycle, and Agent audit history (SC1).

## Verify

```bash
set -e
(
npm install --prefix helix-desktop && npm exec --prefix helix-desktop -- vitest run --root helix-desktop --config vitest.config.ts helix-desktop/src/campaign/CampaignWorkspace.test.tsx -t "^does_not_create_an_activity_for_an_unknown_identity$"
npm exec --prefix helix-desktop -- vitest run --root helix-desktop --config vitest.config.ts helix-desktop/src/campaign/CampaignWorkspace.test.tsx -t "^does_not_regress_pending_projection_from_an_older_revision$"
npm exec --prefix helix-desktop -- vitest run --root helix-desktop --config vitest.config.ts helix-desktop/src/App.test.tsx
)
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

- 2026-10-29 — created by dispatch_driver.py fix-tasks from wave 2 cycle 1
- 2026-10-29 — App now owns the selected AgentConnection, supplies it to HelixShell, derives the campaign URL from it, and keys CampaignWorkspace by that URL. Added an App regression that selects a non-default Agent, creates the bounded activity, switches Agents to remount, and restores only through the selected Agent URL.
- 2026-10-29 — Verify passed: `npm install --prefix helix-desktop`; CampaignWorkspace unknown-identity focused test (1 passed, 3 skipped); CampaignWorkspace stale-revision focused test (1 passed, 3 skipped); App.test.tsx (6 passed).
- 2026-10-29 — orchestrator Verify (sidecar gsd-path-verify/task-t021-verify): pass, exit 0; output tail:
  ```
     Duration  1.75s (transform 72ms, setup 146ms, import 162ms, tests 104ms, environment 1.11s)
  
  
   RUN  v4.1.7 C:/Users/u643664/.gsd-path/projects/3b869b23b4749acc/73bee6b8d21f842f/verify/task-t021-verify/helix-desktop
  
  
   Test Files  1 passed (1)
        Tests  1 passed | 3 skipped (4)
     Start at  08:52:52
     Duration  1.75s (transform 67ms, setup 128ms, import 151ms, tests 131ms, environment 1.14s)
  
  
   RUN  v4.1.7 C:/Users/u643664/.gsd-path/projects/3b869b23b4749acc/73bee6b8d21f842f/verify/task-t021-verify/helix-desktop
  
  
   Test Files  1 passed (1)
        Tests  6 passed (6)
     Start at  08:52:56
     Duration  1.78s (transform 96ms, setup 144ms, import 204ms, tests 152ms, environment 1.07s)
  ```
- 2026-10-29 — orchestrator regression Verify of T003 (sidecar gsd-path-verify/task-t021-verify): pass, exit 0; output tail:
  ```
     Duration  1.69s (transform 70ms, setup 128ms, import 153ms, tests 124ms, environment 1.07s)
  
  
   RUN  v4.1.7 C:/Users/u643664/.gsd-path/projects/3b869b23b4749acc/73bee6b8d21f842f/verify/task-t021-verify/helix-desktop
  
  
   Test Files  1 passed (1)
        Tests  1 passed | 3 skipped (4)
     Start at  08:53:06
     Duration  1.73s (transform 74ms, setup 131ms, import 164ms, tests 148ms, environment 1.09s)
  
  
   RUN  v4.1.7 C:/Users/u643664/.gsd-path/projects/3b869b23b4749acc/73bee6b8d21f842f/verify/task-t021-verify/helix-desktop
  
  
   Test Files  1 passed (1)
        Tests  6 passed (6)
     Start at  08:53:10
     Duration  1.79s (transform 92ms, setup 139ms, import 187ms, tests 174ms, environment 1.09s)
  ```
