---
id: T004
title: Document the supported local campaign contract
wave: 3
deps: [T002]
status: in-progress
agent: build_T004
base: c2941e70e3f586cf461c6cb4bc6fef027ff680d4
worktree: C:\Users\u643664\source\repos\CSharpGitLab\Helix
task_branch: null
files:
  - docs-site/docs/operator/local-campaign.md
  - docs-site/sidebars.ts
---

# T004 — Document the supported local campaign contract

## Context

M001 must document the shipped local execution, ingress, recovery, and Agent-authority boundary. Current runtime/code is authoritative by default, and the Agent path is the only command authority. This document covers the bounded demonstrator only, not later sample, result, planning, or general campaign capabilities.

## Approach

- Document the local desktop start, durable pending wait, full ingress tuple/delivery identity behavior, duplicate dispositions, restart recovery, uncertain physical outcome, and renewed lease/capability/validation/safety requirements using the landed Agent contract.
- State plainly that accepted ingress and displayed records do not prove offline physical completion or grant device authority; no SiteServer, dashboard, internet, LIMS, or remote-driver requirement is introduced.
- Register the page in the existing Docusaurus sidebar.

## Interface contract

- Agent API: `POST /campaigns/bounded-activity` returns `{ campaignId, activityId, flowId, awaitingStepId, commandId, revision, lifecycle, physicalOutcome, audit }`; `GET /campaigns/{campaignId}/activities/{activityId}` returns that projection; `POST /external-evidence` accepts `{ campaignId, activityId, flowId, awaitingStepId, expectedRevision, source, eventId, payload }` and returns `{ disposition, projection }`.

## Intent coverage

- SC4

## Acceptance criteria

1. Operator documentation describes the shipped local execution, correlated ingress/deduplication, recovery, and Agent-only authority boundary and explicitly rejects offline physical-completion/control claims (SC4/N4).

## Verify

```bash
npm install --prefix docs-site && npm run build --prefix docs-site # docs-site/docs/operator/local-campaign.md
```

Heavy: no

## Log

- 2026-10-28 — created by planner
- 2026-10-29 — blocked: unable to inspect the checked-in Agent routes/persistence/recovery/desktop projection needed to document shipped behavior; the runtime search command (`rg`) was unavailable and the read-only context budget is exhausted. No product files changed.
