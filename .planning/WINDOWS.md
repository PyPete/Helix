---
schema_version: 1
open_count: 1
waived_count: 0
fixed_count: 0
total_count: 1
last_updated: 2026-11-03T11:16:03.288Z
---

# Broken Windows Ledger

> Cross-phase defect register. With `workflow.windows_enforce` enabled, `/gsd-ship` blocks while `open_count > 0`.
> Waive with `gsd-tools windows waive <id> "<reason>"` (reason required).
> Mark fixed with `gsd-tools windows fixed <id>`.

| id | phase | kind | file | line | description | status | reason | recorded_at | resolved_at |
|----|-------|------|------|------|-------------|--------|--------|-------------|-------------|
| 1 | 03 | deviation | .planning/phases/03-clean-room-scientific-workflow-designer/03-04-SUMMARY.md |  | Task 3 accessibility changes did not have a separate committed RED test before implementation; task tests were completed green. | open |  | 2026-11-03T11:16:03.288Z |  |

````json
[
  {
    "id": 1,
    "kind": "deviation",
    "phase": "03",
    "file": ".planning/phases/03-clean-room-scientific-workflow-designer/03-04-SUMMARY.md",
    "line": null,
    "description": "Task 3 accessibility changes did not have a separate committed RED test before implementation; task tests were completed green.",
    "status": "open",
    "reason": "",
    "recorded_at": "2026-11-03T11:16:03.288Z",
    "resolved_at": null,
    "milestone": null
  }
]
````
