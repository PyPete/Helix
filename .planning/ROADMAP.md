# Roadmap

**Project:** Helix Orchestration Platform
**Updated:** 2026-06-03

---

## Milestones

| Milestone | Description | Status | Archived |
|-----------|-------------|--------|---------|
| v0.1 | Hardware-ready platform: 48 drivers, Tauri workflow builder, site dashboard, docs, Docker deployment | SHIPPED 2026-06-03 | [v0.1-ROADMAP.md](milestones/v0.1-ROADMAP.md) |
| v0.2 | Production hardening: CORS, SciChart, cloud multi-site, SiLA 2 adapters, ELN/LIMS connector | PLANNING | — |

---

## v0.2 Backlog

> Phases to be defined via `/gsd:new-milestone`

Candidates from v0.1 deferred items:
- CORS policy for SiteServer (production deployment requirement)
- SciChart integration replacing recharts placeholder
- DeviceStatusEvent adapter-prefix fix (emits full key instead of adapter prefix)
- WorkflowBuilder.tsx async useState cleanup
- Human verification pass: Tauri runtime UI tests, live docker-compose run, systemd on Debian 12
- Cloud-level orchestration across multiple sites
- SiLA 2 protocol adapters
- ELN/LIMS connector (WSL authoring seam exists)
- AI/ML optimisation loop integration (Campaign Manager seam exists)
- Mobile app

