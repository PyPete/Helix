# Plan 01-07 Summary: Command Evidence History

## Delivered

- Added authenticated command-history paging and append-only review-note endpoints. History is read from the ledger's committed facts; unavailable storage is surfaced as HTTP 503 rather than an empty completed history.
- Extended the durable ledger contract and PostgreSQL implementation with bounded, sequence-plus-stable-ID cursor ordering and idempotent, attributed review notes. Migration `005_command_review_notes.sql` creates the append-only note store.
- Added native broker support for selected-execution cursor reads and note appends, with renderer DTOs and `useCommandEvidence`. The hook cancels stale selections, merges stable evidence IDs in committed sequence order, retains partial rows, and exposes unavailable history.
- Mounted native committed history in `ExperimentTimeline`; history failures are visible and missing legacy fields render as `Not recorded`.

## Verification

- `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --filter FullyQualifiedName~CommandEvidenceTests --verbosity minimal` — passed (4 tests).
- `npm --prefix helix-desktop test -- --run src/hooks/useCommandEvidence.test.ts` — passed (2 tests).
- `npm --prefix helix-desktop run build` — passed. Vite reported only its existing bundle-size advisory.

## Notes

- The focused .NET run emitted pre-existing dependency/platform warnings (NU1603, NU1902, MSB3277, CS8425, and CA1416) but completed successfully.
- Existing unrelated desktop session/control changes were preserved and not included in this plan's implementation commit.
