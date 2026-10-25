---
phase: 4
plan: phase-4
subsystem: tests
tags: [integration-tests, transport, e2e, postgresql]
requires: [phase-1, phase-2, phase-3]
provides: [transport-test-coverage, e2e-workflow-test, postgres-test-coverage]
affects: [ci]
tech-stack:
  added: []
  patterns: [xunit-collection-fixture, nsubstitute-dispose-verification, testcontainers-guard]
key-files:
  created:
    - tests/Helix.Orchestration.Agent.Tests/Integration/E2eWorkflowExecutionTests.cs
  modified:
    - tests/Helix.Orchestration.Devices.Tests/Transports/ModbusTransportTests.cs
    - tests/Helix.Orchestration.Devices.Tests/Transports/SerialTransportTests.cs
    - tests/Helix.Orchestration.Devices.Tests/Transports/OpcTransportTests.cs
    - tests/Helix.Orchestration.Devices.Tests/Transports/TcpTransportTests.cs
    - tests/Helix.Orchestration.Infrastructure.Tests/Persistence/PostgresRepositoriesTests.cs
    - Helix.Orchestration.slnx
key-decisions:
  - Added [CollectionDefinition] to PostgresRepositoriesTestsCollection (was missing, causing fixture injection failure when Skip was removed)
  - Used IsAvailable guard pattern instead of [Skip] for Docker-conditional tests
requirements-completed: [D2, D3, D5]
duration: "12 min"
completed: "2026-05-27"
---

# Phase 4 Plan: Integration Test Coverage Summary

Added 10 new test facts across 5 test files to close automated test gaps in transport layer, E2E workflow execution, and PostgreSQL repository coverage. All 264 tests pass with 0 skipped.

**Duration:** ~12 min | **Start:** 2026-05-27 | **Tasks:** 7 | **Files:** 6 modified, 1 created

## Tasks Completed

| Task | Description | Result |
|------|-------------|--------|
| T1 | ModbusTransportTests: 4 new facts (WriteMultipleRegisters x2, ReadInputRegisters x2) | PASS |
| T2 | SerialTransportTests: 2 new facts (BaudRate propagation, WriteException fault) | PASS |
| T3 | OpcTransportTests: 1 new fact (Unsubscribe_DisposesSubscription) | PASS |
| T4 | TcpTransportTests: 1 new fact (LengthPrefix framing round-trip) | PASS |
| T5 | E2eWorkflowExecutionTests: 2 new facts (happy path + capability failure path) | PASS |
| T6 | PostgresRepositoriesTests: removed [Fact(Skip)], added [CollectionDefinition] | PASS |
| T7 | slnx: moved Agent.Tests + Core.Tests into /tests/ folder group | PASS |

## Test Suite Summary

| Project | Tests | Passed | Skipped |
|---------|-------|--------|---------|
| Agent.Tests | 15 | 15 | 0 |
| Core.Tests | 107 | 107 | 0 |
| Devices.Tests | 81 | 81 | 0 |
| Infrastructure.Tests | 2 | 2 | 0 |
| Recorder.Tests | 15 | 15 | 0 |
| SiteServer.Tests | 44 | 44 | 0 |
| **Total** | **264** | **264** | **0** |

## Deviations from Plan

**[Rule 1 - Bug] CollectionDefinition missing from PostgresRepositoriesTestsCollection** — Found during: T6 | Issue: Removing [Fact(Skip)] exposed a pre-existing xUnit fixture injection failure because PostgresRepositoriesTestsCollection lacked [CollectionDefinition] | Fix: Added [CollectionDefinition(nameof(PostgresRepositoriesTestsCollection))] | Files: PostgresRepositoriesTests.cs | Verification: both facts pass | Commit: e84cc8f

**Total deviations:** 1 auto-fixed. **Impact:** minimal — pre-existing gap only visible when Skip was removed.

## Self-Check: PASSED

- All must_haves/truths verified: transport test assertions pass, E2E reaches Completed, InMemoryTrendProvider scalar asserted, PostgresRepositoriesTests run (not skipped), slnx includes all 6 test projects
- dotnet test Helix.Orchestration.slnx exits 0
- Zero [Skip] entries for PostgresRepositoriesTests

**Next:** Phase complete — ready for /gsd:verify-work 4 or /gsd:plan-phase 5.
