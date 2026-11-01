# Deferred Items

- **02-17 Acceptance:** Repository-wide `dotnet format Helix.Orchestration.slnx --verify-no-changes --no-restore` reports pre-existing whitespace violations across Agent, Core, Infrastructure, SiteServer, and test projects. Repair the formatting baseline, then rerun `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verification/phase02-e2e.ps1 -Mode Acceptance`; its desktop Vitest and production-build steps remain unrun.
