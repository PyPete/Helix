---
pipeline: gsd-path/v2
project: helix
milestone: durable-local-campaign-skeleton
phase: build
                    # inspect only for brownfield; greenfield starts at define
                    # roadmap only in program flow (CHARTER.md exists)
status: blocked
branch: gsd-path/M001
                    # the router rebinds before any next-milestone file change
                    # the bound branch is never main; ship integrates it there
archive: null       # persisted archive transaction path; never recomputed
integration_default: direct # direct | pull-request | external-landing; project setting
integration: direct # current milestone; may override the default before build
integration_source: default # default | milestone; preserves override provenance
default_branch: main # origin default branch; initialization records it once, nothing changes it
---

# Project State

One file, always current. The router reads this first; every phase updates
it on completion. If this file and the artifacts disagree, the artifacts win
— fix this file.

## Log

<!-- append one line per transition: date, phase, event -->
- 2026-10-26 — inspect — project initialized
- 2026-10-27 — inspect — router bound initial milestone
- 2026-10-27 — inspect — inspection artifacts passed
- 2026-10-27 — define — definition started
- 2026-10-27 — define — program charter approved
- 2026-10-27 — research — research started
- 2026-10-27 — research — Evidence gate failed after corrected redispatch: .project/research/evidence-domain.md has a malformed assigned-question answer; it uses -> instead of the required Unicode → separator.
- 2026-10-27 — research — evidence answer separator corrected; resuming research gate
- 2026-10-27 — research — research complete: manifest .project/research/RESEARCH.md; dispatched domain, stack, pitfalls, similar; skipped none; 4 questions
- 2026-10-27 — decide — decision synthesis started
- 2026-10-27 — decide — validated decision synthesis .project/SYNTHESIS.md
- 2026-10-28 — roadmap — roadmap started
- 2026-10-28 — roadmap — failed roadmap gate: milestone headings must use lowercase hyphenated slugs
- 2026-10-28 — roadmap — roadmap correction resumed
- 2026-10-28 — roadmap — program roadmap approved
- 2026-10-28 — define — definition started
- 2026-10-28 — define — milestone intent approved
- 2026-10-28 — research — research started
- 2026-10-28 — research — research complete: manifest .project/research/RESEARCH.md; dispatched stack,pitfalls; skipped domain,similar; 1 question
- 2026-10-28 — decide — decision synthesis started
- 2026-10-28 — decide — validated decision synthesis .project/research/SYNTHESIS.md
- 2026-10-28 — plan — planning started
- 2026-10-28 — plan — failed plan gates: Surface contract is missing the approved surface
- 2026-10-28 — plan — planning resumed
- 2026-10-28 — plan — failed plan gates: Surface contract missing local helix-desktop campaign workspace
- 2026-10-28 — plan — planning resumed
- 2026-10-28 — plan — plan approved
- 2026-10-28 — build — build started
- 2026-10-28 — build — User chose projection-only T003; transfer SC3 lease/later-command proof to a separately planned Agent task. T003 diff is preserved outside the repo and primary restored; dispatch attempt settlement awaits an approved command.
