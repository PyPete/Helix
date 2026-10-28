---
id: T013
title: Correct campaign-create API guidance
wave: 3
deps: []
status: pending
agent: null
base: null
worktree: null
task_branch: null
files:
  - docs-site/docs/operator/experiment-setup.md
---
# T013 — Correct campaign-create API guidance
## Context
"Campaign create takes workflowDefinitionJson and controls iteration count" | stale | docs-site/docs/operator/experiment-setup.md:46,60; Helix.Orchestration.SiteServer/Endpoints/CampaignEndpoints.cs:13-27 accepts parameter sets instead.
The accepted ruling is fix-doc, not a campaign API change.
## Approach
- Describe the current create request’s name, description, and parameter-set contract; remove workflow-definition and iteration-control claims not supported by `CampaignEndpoints`.
## Interface contract
- None
## Intent coverage
- None
## Acceptance criteria
1. Experiment setup documents campaign creation through the current parameter-set API and does not claim `workflowDefinitionJson` controls iterations.
## Verify
```bash
powershell -NoProfile -Command "$p='docs-site/docs/operator/experiment-setup.md'; $c=Get-Content -Raw $p; if($c -match 'workflowDefinitionJson' -or $c -notmatch 'ParameterSets'){throw 'campaign-create claim not aligned'}" # docs-site/docs/operator/experiment-setup.md
```
Heavy: no
## Log
- 2026-10-28 — created by planner
