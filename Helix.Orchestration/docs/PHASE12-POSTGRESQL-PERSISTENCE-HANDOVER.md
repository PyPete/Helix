# Phase 12 — PostgreSQL Persistence Handover

## Executive Summary

Phase 12 adds a dedicated persistence layer for campaign and experiment state using PostgreSQL, including infrastructure project wiring, repository implementations, schema migration scripts, and local Docker composition support. This phase establishes a durable storage foundation for campaign lifecycle and runspace correlation beyond in-memory process lifetime.

## Delivered Scope

- Added new project: `Helix.Orchestration.Infrastructure`
  - `Helix.Orchestration.Infrastructure.csproj` (net10.0, Dapper, Npgsql, Microsoft.Extensions.* abstractions)
- Added persistence contracts and models:
  - `Persistence/IExperimentRepository.cs`
  - `Persistence/ICampaignRepository.cs`
- Added PostgreSQL infrastructure implementations:
  - `Persistence/DbConnectionFactory.cs`
  - `Persistence/PostgresExperimentRepository.cs`
  - `Persistence/PostgresCampaignRepository.cs`
  - `Persistence/InfrastructureServiceExtensions.cs`
- Added initial SQL migration:
  - `postgres/migrations/001_initial.sql`
- Updated local dev composition:
  - `docker-compose.yml` includes new `postgres:16` service, health check, volume, migration mount
- Solution/application wiring:
  - Added `Helix.Orchestration.Infrastructure` to `Helix.Orchestration.slnx`
  - Added SiteServer project reference to Infrastructure
- Added integration test project:
  - `tests/Helix.Orchestration.Infrastructure.Tests/Helix.Orchestration.Infrastructure.Tests.csproj`
  - `tests/Helix.Orchestration.Infrastructure.Tests/Persistence/PostgresRepositoriesTests.cs`
  - Test project linked in solution test folder

## Schema Overview

`001_initial.sql` provisions:

- `campaigns`
  - primary key: `campaign_id`
  - status and lifecycle timestamps
- `campaign_iterations`
  - composite primary key: `(campaign_id, iteration_index)`
  - FK to `campaigns` with `ON DELETE CASCADE`
  - parameter payload persisted as `JSONB`
- `experiments`
  - primary key: `runspace_id`
  - optional FK to `campaigns` with `ON DELETE SET NULL`
  - definition payload persisted as `JSONB`

Indexes:

- `ix_experiments_campaign_id`
- `ix_campaign_iterations_runspace_id`

## API / Contract Surface

New infrastructure registration method:

- `services.AddHelixInfrastructure(configuration)`
  - requires `ConnectionStrings:HelixOrchestration`
  - registers `DbConnectionFactory`
  - registers `IExperimentRepository` and `ICampaignRepository`

Repository capabilities:

- `PostgresExperimentRepository`
  - upsert experiment state by runspace
  - query by runspace id
  - query experiments by campaign id
- `PostgresCampaignRepository`
  - upsert campaign + replace iterations transactionally
  - query campaign with ordered iterations
  - query all campaigns with iteration expansion
  - delete campaign (iteration cascade via FK)

## Validation Results

Build and test status:

- `run_build` (solution): **successful**
- SiteServer tests: **44 passed, 0 failed**
- Core tests: **107 passed, 0 failed**
- Infrastructure tests:
  - Build/discovery: successful (2 tests discovered)
  - Runtime execution in this environment: failed due to missing Docker endpoint (`npipe://./pipe/docker_engine`) required by Testcontainers

## Architectural Notes

- Persistence responsibilities are isolated into `Helix.Orchestration.Infrastructure` to keep Site Server and Core contracts clean.
- Campaign repository writes are wrapped in a transaction to keep campaign row and iteration rows consistent.
- JSON payload columns preserve flexibility for evolving parameter and runspace definition shapes.
- Infrastructure integration tests are intentionally container-backed and require Docker availability in CI/dev runtime environments.

## Recommended Next Phase

Proceed to operationalization and runtime integration hardening:

1. Wire `ICampaignManager` state transitions to `ICampaignRepository` writes.
2. Persist experiment lifecycle updates from `IAgentProxy` status polling/notifications.
3. Add CI path that runs Infrastructure tests on Docker-enabled agents.
4. Introduce schema migration tooling/versioning workflow (e.g., migration runner in startup pipeline).
