---
phase: 02-scientific-setup-foundation
plan: "26"
subsystem: scientific-native-client-security
tags: [rust, typescript, tauri, scientific-receipts, bounds]
requires: [02-24, 02-25]
provides: [bounded-scientific-json, original-key-receipt-validation]
affects: [APP-01, APP-02, APP-03, APP-04, SAMP-01, SAMP-02]
tech-stack:
  added: []
  patterns: [depth-bounded-json, receipt-identity-validation, stable-operation-key]
key-files:
  created: []
  modified:
    - helix-desktop/src-tauri/src/commands/agent.rs
    - helix-desktop/src/lib/scientificClient.ts
    - helix-desktop/src/lib/scientificClient.test.ts
decisions:
  - Native scientific JSON rejects nesting deeper than the approved 32-level contract before it can reach Agent authority.
  - Scientific client mutation success requires a Committed receipt matching its original operation ID, kind, and known resource identity.
metrics:
  duration: 10min
  completed: 2026-11-01
  tasks: 2
status: complete
plan_head_before: 96bfdb610f7c3638e39693040380c1513f84d696
plan_head_after: f5c11805e66e851edb19b6d2042bd519fd4a1377
commits: 4
actuals:
  tokens: 6000
  tasks: 2
  commits: 4
requirements-completed: [APP-01, APP-02, APP-03, APP-04, SAMP-01, SAMP-02]
---

# Phase 02 Plan 26: Scientific Native and Client Security Summary

Scientific payloads now have an explicit native nesting bound, and committed client acknowledgements remain bound to the caller's original operation identity.

## Accomplishments

- Added a 32-level recursive JSON nesting guard at the native scientific boundary while retaining existing request-size, fixed-route, loopback, and session protections.
- Added fail-first native coverage proving a 33-level scientific payload is rejected; ordinary Cargo execution still excludes the explicitly ignored live fixture.
- Hardened scientific create, save, publish, derive, and adopt acknowledgement parsing so only a matching `Committed` receipt and valid non-negative integer token can report success.
- Added a fail-first regression proving a malformed draft-save acknowledgement cannot fabricate a completed scientific mutation.

## Task Commits

1. **Task 1: Reject native and Agent adversarial inputs without invoking live-host fixture accidentally** — `f871ec1`, `73983f9`
2. **Task 2: Preserve exact scientific receipts and original-key recovery with legacy compatibility** — `999f2c4`, `f5c1180`

## Verification

- `cargo test --manifest-path helix-desktop/src-tauri/Cargo.toml agent_security` — passed: 14 guard tests passed and exactly one intended live fixture remained ignored.
- `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --filter FullyQualifiedName~ScientificSetupTests --verbosity minimal` — passed: 2 isolated PostgreSQL Agent tests, zero skipped.
- `npm --prefix helix-desktop test -- --run src/lib/scientificClient.test.ts src/lib/agentClient.test.ts` — passed: 20 tests across 2 files.

## TDD Evidence

- **RED:** `agent_security_rejects_scientific_json_deeper_than_the_contract_limit` failed on its intended assertion before the nesting guard existed; `rejects a draft save without a matching committed receipt or numeric token` failed because the client returned the malformed acknowledgement.
- **GREEN:** Added recursive depth validation and exact receipt/token validation; the focused Rust and Vitest suites pass.
- **REFACTOR:** No cleanup was needed beyond extracting the shared scientific receipt validator.

## Decisions Made

- Native request-size limits do not replace structural depth limits; deeply nested payloads are rejected before Agent authority.
- A scientific mutation acknowledgement is evidence only when its committed kind, operation ID, and applicable resource ID match the originating request.

## Deviations from Plan

None - plan executed exactly as written.

## Known Stubs

None.

## Self-Check: PASSED

- Native boundary and scientific client files exist.
- Task commits `f871ec1`, `73983f9`, `999f2c4`, and `f5c1180` are present in HEAD ancestry.
