# Plan 01-04 Summary: Canonical Submission Transport Boundary

## Delivered

- Replaced the raw `POST /runspaces` creation route with a structured unsupported-proposal rejection; a client JSON `RunspaceDefinition` cannot create execution authority.
- Replaced name-only experiment creation with an authenticated `WslDocument` submission. The Agent recompiles the document, rejects invalid/empty/unsupported workflows, records the resulting definition hash, approved policy revision, and verified actor, then creates the runspace from the Agent-produced definition.
- Added Agent-owned canonical experiment definitions to the experiment projection. Legacy gRPC `SubmitExperiment` now accepts only an Agent-issued experiment reference and uses its recorded definition; raw, empty, or forged JSON is rejected before `IRunspaceManager.CreateAsync`.
- Preserved protobuf compatibility and cancellation handling while redacting unexpected submission errors.
- Added production-boundary tests proving raw gRPC payloads have zero runspace-creation effects and canonical references use only the stored Agent definition.

## Route and Executor Inventory

| Transport | Submission behavior |
| --- | --- |
| `POST /experiments` | Authenticated WSL proposal; Agent compilation and canonical hash recording required. |
| `POST /runspaces` | Explicitly rejects raw definitions. |
| gRPC `SubmitExperiment` | Accepts an Agent-issued experiment reference only; raw definitions reject. |
| REST/gRPC lifecycle routes | Retained compatibility lifecycle facades over the tracked runspace; durable-control/actual-host execution refinements remain assigned to Plan 01-11 as specified by the plan. |

## Verification

- `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --filter FullyQualifiedName~DispatchBoundaryTests --verbosity minimal` — passed (2 tests).

## Compatibility Rejections

- Direct raw runspace submissions are rejected with `runspace.raw_definition_unsupported`.
- gRPC serialized/empty/forged definitions are rejected with a canonical-experiment-reference message and no runspace creation.
