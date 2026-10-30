# Phase 11 — Campaign Manager Handover

## Executive Summary

Phase 11 introduces a Site Server-owned campaign orchestration layer that coordinates multi-iteration experiment submissions through the existing `IAgentProxy` seam. Campaign lifecycle state remains centralized at the Site Server boundary, while execution of individual runs is delegated to the Agent transport selected in Phase 10.

## Delivered Scope

- Added `Helix.Orchestration.SiteServer/Campaigns/ICampaignIterationStrategy.cs`
  - Strategy seam and iteration plan model
- Added `Helix.Orchestration.SiteServer/Campaigns/SequentialStrategy.cs`
  - Default ordered iteration strategy
- Added `Helix.Orchestration.SiteServer/Campaigns/Campaign.cs`
  - Campaign aggregate and status model
- Added `Helix.Orchestration.SiteServer/Campaigns/CampaignIteration.cs`
  - Per-iteration state model and statuses
- Added `Helix.Orchestration.SiteServer/Campaigns/ICampaignManager.cs`
  - Campaign lifecycle contract
- Added `Helix.Orchestration.SiteServer/Campaigns/CampaignManager.cs`
  - In-memory campaign orchestration implementation
- Added `Helix.Orchestration.SiteServer/Contracts/CampaignContracts.cs`
  - REST request/response DTOs for campaigns
- Added `Helix.Orchestration.SiteServer/Endpoints/CampaignEndpoints.cs`
  - CRUD/start campaign REST endpoints
- Updated `Helix.Orchestration.SiteServer/Program.cs`
  - Registered campaign services and endpoint mapping
- Added tests:
  - `tests/Helix.Orchestration.SiteServer.Tests/Campaigns/CampaignManagerTests.cs`
  - `tests/Helix.Orchestration.SiteServer.Tests/Endpoints/CampaignEndpointsTests.cs`

## API / Contract Surface

New REST endpoints under `/api/campaigns`:

- `POST /api/campaigns/` — create campaign
- `GET /api/campaigns/` — list campaigns
- `GET /api/campaigns/{id}` — fetch single campaign
- `POST /api/campaigns/{id}/start` — start sequential iteration execution
- `DELETE /api/campaigns/{id}` — delete campaign

Campaign execution behavior:

- Iterations are constructed by `ICampaignIterationStrategy`
- `CampaignManager.StartAsync` submits each iteration to `IAgentProxy`
- Per-iteration status transitions tracked (`Pending` → `Running` → `Completed`/`Failed`)
- Campaign status transitions tracked (`Draft` → `Running` → `Completed`/`Failed`)

## Validation Results

- `dotnet build tests/Helix.Orchestration.SiteServer.Tests.csproj` succeeded
- SiteServer test run:
  - **44 passed, 0 failed**
  - Includes new campaign unit and endpoint integration coverage

## Architectural Notes

- Campaign ownership is intentionally Site Server-only to preserve the control-plane invariant established in `CONCEPT.md`.
- Current implementation is in-memory by design; persistence is deferred to Phase 12.
- Iteration strategy is pluggable, allowing future optimization policies (grid, adaptive, Bayesian, etc.) without endpoint contract churn.
- Iterations currently submit minimal `RunspaceDefinition` payloads with parameter metadata, consistent with existing Phase 9/10 workflow stubs.

## Recommended Next Phase

Proceed to **Phase 12 — PostgreSQL Persistence**:

- Persist campaigns and iterations across restarts
- Persist experiment submission/runspace correlation data
- Introduce infrastructure project for data access and connection management
- Wire dockerized PostgreSQL and migration scripts for local/dev environments
