---
id: T017
title: Correct the SiteServer REST API port reference
wave: 3
deps: []
status: pending
agent: null
base: null
worktree: null
task_branch: null
files:
  - docs-site/package.json
  - docs-site/docs/developer/rest-api.md
---
# T017 — Correct the SiteServer REST API port reference
## Context
DOCS-AUDIT queue #21 reports that `docs-site/docs/developer/rest-api.md:9` says the SiteServer REST API runs on port 7020, while `docker-compose.yml:105` configures 5090 and the dashboard development proxy is separately configured. The user selected every accepted documentation finding, so this is a `fix-doc` task: current runtime/deployment configuration remains authoritative and no runtime port change is in scope. The documentation site remains the executable presentation check for this repair.
## Approach
- Replace the stale public/development port statement with the checked-in Compose SiteServer port and distinguish it from the dashboard development proxy where relevant.
- Re-run the audit claim check against the corrected page and build the Docusaurus site in a freshly provisioned dependency tree.
- Do not modify product runtime, deployment, or dashboard functionality.
## Interface contract
- None
## Intent coverage
- None
## Acceptance criteria
1. Queue #21 is corrected: the REST API reference identifies the Compose SiteServer port as 5090 and does not present 7020 as the supported endpoint.
2. The Docusaurus site builds from a freshly provisioned dependency tree after this documentation repair.
## Verify
```bash
grep -F "5090" docs-site/docs/developer/rest-api.md && npm install --prefix docs-site && npm run build --prefix docs-site # docs-site/package.json
```
Heavy: no
## Log
- 2026-10-28 — created by planner
