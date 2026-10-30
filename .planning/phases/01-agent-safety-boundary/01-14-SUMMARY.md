# Plan 01-14 Summary

## Delivered

- Added a protected 64 MiB append-only denial spool with 8 KiB record bounds, 32-request reservation ceiling, redacted route-only records, synchronous disk flush, and fail-closed saturation/I/O behavior.
- Denied attempts retain stable IDs, UTC occurrence time, Agent generation, source ordinal, bounded reason, and verified identity only; bearer tokens, query values, and request bodies are excluded.
- Added startup and two-second periodic spool recovery. Recovery appends immutable rejected-command evidence using the original stable attempt ID, acknowledges only after ledger success, archives only acknowledged records, and blocks command admission until the canonical ledger is available.
- Tightened PostgreSQL ledger idempotency so a reused command ID must match immutable intent, admission state, and check evidence.
- Added spool redaction, capacity-saturation, and recreated-service recovery tests.

## Verification

`dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --filter "FullyQualifiedName~CommandEvidenceTests.Spool|FullyQualifiedName~CommandEvidenceTests.Saturation" --verbosity minimal`

Passed: 3 tests. The build completed as part of the targeted test run. Existing restore/package and platform warnings remain (NU1603, NU1902, MSB3277, CA1416).
