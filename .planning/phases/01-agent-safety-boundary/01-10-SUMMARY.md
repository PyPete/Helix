# Plan 01-10 Summary: Safe Proposal Reads and Compile Feedback

## Delivered

- Replaced capability-variable invocation with a canonical, read-only `ISimulatorObservation` allowlist: `GetFlow` (`flowRate`, `mL/min`), `GetTemperature` (`temperature`, `°C`), and `GetMode` (`mode`, enum). Missing, stale, malformed, mismatched, or unreadable observations return explicit `Unknown`; setters, legacy names, and arbitrary names are invalid and never invoke a capability registry.
- Added structured variable-resolution results and diagnostics. Compatibility APIs now preserve non-device substitution but fail closed for unresolved device variables instead of substituting empty/default values.
- Made WSL compilation fail closed for unsupported executor semantics (loop, branch, parallel, safety guards, guarded/fan-out edges), legacy/unknown capabilities, non-canonical parameters, invalid action values, and invalid delays. Supported sequential definitions carry deterministic SHA-256 revision/hash metadata.
- Added `CompileWithFeedback`, which returns all seven fresh `Passed`/`Failed`/`Unknown` admission checks without creating dispatch authority. The Agent endpoint authenticates the actor from claims, builds fresh admission context, returns structured compile feedback, and explicitly exposes `DispatchAuthorized: false`.
- Updated compiler tests for canonical sequential actions and explicit unsupported-construct rejection; added observation allowlist/staleness, legacy capability, and changed-context/non-authorizing feedback coverage.

## Verification

- `dotnet test tests/Helix.Orchestration.Core.Tests/Helix.Orchestration.Core.Tests.csproj --no-restore --filter FullyQualifiedName~WslCompilerTests --verbosity minimal` — passed, 19 tests.
- `dotnet test tests/Helix.Orchestration.Core.Tests/Helix.Orchestration.Core.Tests.csproj --no-restore --verbosity minimal` — passed, 144 tests.
- `dotnet build Helix.Orchestration.slnx --no-restore --verbosity minimal` — succeeded. Existing NuGet/OPC UA, System.Reactive, and platform warnings remain.

## Commit

- `6f6363c feat: harden workflow compiler admission`

## Scope Notes

The compatibility `ICapabilityInvoker` substitution overload intentionally does not invoke the invoker. It delegates device references to an unavailable read-only observation port so existing callers retain non-device substitution while device reads remain fail-closed until migrated to `ISimulatorObservation`.
