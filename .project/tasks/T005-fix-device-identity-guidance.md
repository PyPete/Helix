---
id: T005
title: Label device identity guidance as future intent
wave: 3
deps: []
status: pending
agent: null
base: null
worktree: null
task_branch: null
files:
  - .github/copilot-instructions.md
---
# T005 — Label device identity guidance as future intent
## Context
"Devices should be identified by COM port plus USB VID:PID" | aspirational | .github/copilot-instructions.md states this guidance; current serial adapter options use port names and no VID/PID identity implementation was found (Helix.Orchestration.Devices/Transports/Serial/SerialTransportOptions.cs).
The accepted ruling is fix-doc, not implementation work. The current runtime is authoritative.
## Approach
- Retain useful guidance only when clearly labelled as a desired future guideline, not shipped behavior; do not add VID/PID code.
## Interface contract
- None
## Intent coverage
- None
## Acceptance criteria
1. The instruction does not claim shipped COM-plus-VID/PID identity support.
## Verify
```bash
powershell -NoProfile -Command "$p='.github/copilot-instructions.md'; if ((Get-Content -Raw $p) -match 'Devices should be identified by COM port plus USB VID:PID(?!.*future)') { throw 'unsupported shipped claim remains' }" # .github/copilot-instructions.md
```
Heavy: no
## Log
- 2026-10-28 — created by planner
