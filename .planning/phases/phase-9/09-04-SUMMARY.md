---
phase: "9"
plan: "04"
subsystem: docs
tags: [docusaurus, operator-guide, workflow-authoring, device-capabilities, experiment-setup, deployment]
dependency_graph:
  requires: [09-02, 09-03]
  provides:
    - docs-site/docs/operator/workflow-authoring.md
    - docs-site/docs/operator/device-capabilities.md
    - docs-site/docs/operator/experiment-setup.md
    - docs-site/docs/operator/deployment-setup.md
  affects: [docs-site/sidebars.ts]
tech_stack:
  added: []
  patterns: [mermaid-diagrams, mdx-html-entity-escape, docusaurus-admonitions]
key_files:
  created:
    - docs-site/docs/operator/workflow-authoring.md
    - docs-site/docs/operator/device-capabilities.md
    - docs-site/docs/operator/experiment-setup.md
    - docs-site/docs/operator/deployment-setup.md
  modified:
    - docs-site/sidebars.ts
decisions:
  - "Path parameter {id} in REST examples written as &#123;id&#125; in prose (MDX3 entity escape) but plain {id} inside mermaid/http code fences (safe)"
  - "GetTemperatures return value JSON written as escaped \\{ ... \\} in table cell to avoid MDX3 JSX parse error"
  - "dev credentials in deployment-setup.md explicitly marked dev-only per threat model T-09-05 (Information Disclosure: accept)"
metrics:
  duration_minutes: 4
  tasks_completed: 2
  files_modified: 5
  completed_date: "2026-06-02"
requirements:
  - F3
---

# Phase 9 Plan 04: Operator Documentation Summary

**One-liner:** Four operator docs (workflow authoring, device capabilities, experiment lifecycle, deployment) written from source with Mermaid diagrams and accurate CapabilityDescriptor schema.

## Tasks Completed

| Task | Status | Commit | Notes |
|------|--------|--------|-------|
| 1. workflow-authoring.md + device-capabilities.md | Done | e974b1d | 7-node-type table, Mermaid compile-execute flow, CapabilityDescriptor + ParameterSchema field tables accurate against Core source |
| 2. experiment-setup.md + deployment-setup.md + sidebars.ts | Done | 8c1dcc1 | Campaign→Runspace lifecycle Mermaid, docker-compose startup, sidebars.ts Operator Guide 5 items; npm run build exits 0 |

## npm run build

**Result:** Exit 0

Build succeeded after applying MDX 3 escaping patterns learned in Wave 1:

1. Path parameter `{id}` in prose written as `&#123;id&#125;` (HTML entity) — safe in table cells and inline prose
2. JSON return values in table cells written as `\{ "A": double, ... \}` (backslash-escape)
3. `{id}` inside mermaid and http code fences left as-is (code fences are not MDX-parsed)

## Accuracy Notes

### CapabilityDescriptor fields sourced from `Helix.Orchestration.Core/Capabilities/CapabilityDescriptor.cs`

All fields, types, and descriptions are taken directly from the record definition:
- `VerbName`, `DisplayName`, `Description`, `Category`, `Parameters`, `IsDestructive`, `RequiresActiveRunspace`, `EstimatedDurationMs`, `Returns`
- `ParameterType` enum values: Numeric, Boolean, Enum, String, Duration
- `CapabilityCategory` enum values: Control, Measurement, Status, Utility

### VapourtecR4 capability quick reference

Capability names and parameters cross-referenced against the adapter authoring guide (09-02) and the Phase 2 VapourtecR4 driver implementation.

### Campaign lifecycle REST endpoints

Endpoints sourced from `09-CONTEXT.md` SiteServer REST endpoints table:
- `POST /api/campaigns`, `POST /api/campaigns/{id}/start`, `DELETE /api/campaigns/{id}`
- `DELETE /api/experiments/{id}` for abort

### Deployment ports

- Agent: HTTP 5000, gRPC 5050 (from `09-02-SUMMARY.md` and Agent source)
- SiteServer: HTTP 7020 (default dev port from `09-CONTEXT.md`)
- Infrastructure: PostgreSQL 5432, MQTT 1883, MinIO 9000/9001

## Sidebars Update

Operator Guide items expanded from 1 to 5:
```
['operator/intro', 'operator/workflow-authoring', 'operator/device-capabilities', 'operator/experiment-setup', 'operator/deployment-setup']
```

Developer Guide items (6 items from Wave 1 plans 09-02 and 09-03) preserved unchanged.

## Deviations from Plan

None - plan executed exactly as written.

MDX 3 escaping patterns applied proactively based on Wave 1 learnings — not a deviation, but a preventive application of known requirements.

## Known Stubs

None. All four pages contain complete, sourced content.

## Threat Flags

None. Static documentation files only.

Threat model T-09-05 (Information Disclosure via dev credentials in deployment-setup.md) acknowledged and mitigated: credentials are explicitly labelled as development-only with a warning admonition.

## Self-Check: PASSED

- docs-site/docs/operator/workflow-authoring.md: exists, contains "mermaid"
- docs-site/docs/operator/device-capabilities.md: exists, contains "CapabilityDescriptor" (2x), "VerbName"
- docs-site/docs/operator/experiment-setup.md: exists, contains "mermaid", "DurableRunspace" (3x)
- docs-site/docs/operator/deployment-setup.md: exists, contains "docker-compose" (2x)
- docs-site/sidebars.ts: contains "deployment-setup", "workflow-authoring"
- Commit e974b1d: Task 1 (workflow-authoring + device-capabilities)
- Commit 8c1dcc1: Task 2 (experiment-setup + deployment-setup + sidebars.ts)
- npm run build: exits 0
