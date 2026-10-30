# Phase 02 Baseline Evidence

Generated UTC: 2026-10-30T16:07:05.9578598Z

This report records observed baseline evidence only. It neither accesses a live user database nor promotes Phase 1 waivers, unavailable SQL/native acceptance, or successful existing tests into Phase 2 scientific acceptance.

## Tool Availability

| Tool | Availability / version |
| --- | --- |
| dotnet | 11.0.100-rc.3.26522.148 |
| npm | 11.6.2 |
| cargo | cargo 1.95.0 (f2d3ce0bd 2026-03-21) |
| .NET SDK | .NET SDK:;  Version:           11.0.100-rc.3.26522.148;  Commit:            5254d8bcf5 |

## Command Evidence

| Name | Category | Command | Exit code | Discovered | Seconds | Disposition |
| --- | --- | --- | ---: | ---: | ---: | --- |
| Core unit tests | Core | `dotnet test tests/Helix.Orchestration.Core.Tests/Helix.Orchestration.Core.Tests.csproj --no-restore` | 1 | 643664 | 4.41 | baseline failure; not passed evidence |

<details><summary>Core unit tests output tail</summary>

```text
  Helix.Orchestration.Infrastructure -> C:\Users\u643664\source\repos\CSharpGitLab\Helix\Helix.Orchestration.Infrastructure\bin\Debug\net10.0\Helix.Orchestration.Infrastructure.dll
  Helix.Orchestration.Devices -> C:\Users\u643664\source\repos\CSharpGitLab\Helix\Helix.Orchestration.Devices\bin\Debug\net10.0\Helix.Orchestration.Devices.dll
  Helix.Orchestration.Agent -> C:\Users\u643664\source\repos\CSharpGitLab\Helix\Helix.Orchestration.Agent\bin\Debug\net10.0\Helix.Orchestration.Agent.dll
  Helix.Orchestration.Core.Tests -> C:\Users\u643664\source\repos\CSharpGitLab\Helix\tests\Helix.Orchestration.Core.Tests\bin\Debug\net10.0\Helix.Orchestration.Core.Tests.dll
Test run for C:\Users\u643664\source\repos\CSharpGitLab\Helix\tests\Helix.Orchestration.Core.Tests\bin\Debug\net10.0\Helix.Orchestration.Core.Tests.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
  Failed Helix.Orchestration.Core.Tests.Execution.WorkflowExecutorTests.ExecuteAsync_RunsSimpleWorkflow_Successfully [40 ms]
  Error Message:
   System.NotSupportedException : Generic workflow execution is disabled. Execute stable command steps through ICommandDispatcher.
  Stack Trace:
     at Helix.Orchestration.Core.Tests.Execution.WorkflowExecutorTests.ExecuteAsync_RunsSimpleWorkflow_Successfully() in C:\Users\u643664\source\repos\CSharpGitLab\Helix\tests\Helix.Orchestration.Core.Tests\Execution\WorkflowExecutorTests.cs:line 42
--- End of stack trace from previous location ---
  Failed Helix.Orchestration.Core.Tests.Execution.WorkflowExecutorTests.GetStatusAsync_ReturnsCompletedStatus_AfterWorkflowFinishes [1 ms]
  Error Message:
   System.NotSupportedException : Generic workflow execution is disabled. Execute stable command steps through ICommandDispatcher.
  Stack Trace:
     at Helix.Orchestration.Core.Tests.Execution.WorkflowExecutorTests.GetStatusAsync_ReturnsCompletedStatus_AfterWorkflowFinishes() in C:\Users\u643664\source\repos\CSharpGitLab\Helix\tests\Helix.Orchestration.Core.Tests\Execution\WorkflowExecutorTests.cs:line 73
--- End of stack trace from previous location ---

Failed!  - Failed:     2, Passed:   142, Skipped:     0, Total:   144, Duration: 393 ms - Helix.Orchestration.Core.Tests.dll (net10.0)
```
</details>

| Agent unit tests | Agent | `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore` | 1 | 643664 | 17.37 | baseline failure; not passed evidence |

<details><summary>Agent unit tests output tail</summary>

```text
Test run for C:\Users\u643664\source\repos\CSharpGitLab\Helix\tests\Helix.Orchestration.Agent.Tests\bin\Debug\net10.0\Helix.Orchestration.Agent.Tests.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
  Failed Helix.Orchestration.Agent.Tests.Runspaces.DurableRunspaceTests.StartAsync_DelegatesToWorkflowExecutor_AndCompletes [10 ms]
  Error Message:
   System.NotSupportedException : Legacy DurableRunspace execution is disabled. Use canonical dispatch with a persisted cursor.
  Stack Trace:
     at Helix.Orchestration.Agent.Runspaces.DurableRunspace.StartAsync(CancellationToken cancellationToken) in C:\Users\u643664\source\repos\CSharpGitLab\Helix\Helix.Orchestration.Agent\Runspaces\DurableRunspace.cs:line 57
   at Helix.Orchestration.Agent.Tests.Runspaces.DurableRunspaceTests.StartAsync_DelegatesToWorkflowExecutor_AndCompletes() in C:\Users\u643664\source\repos\CSharpGitLab\Helix\tests\Helix.Orchestration.Agent.Tests\Runspaces\DurableRunspaceTests.cs:line 18
   at Helix.Orchestration.Agent.Tests.Runspaces.DurableRunspaceTests.StartAsync_DelegatesToWorkflowExecutor_AndCompletes() in C:\Users\u643664\source\repos\CSharpGitLab\Helix\tests\Helix.Orchestration.Agent.Tests\Runspaces\DurableRunspaceTests.cs:line 23
--- End of stack trace from previous location ---
  Failed Helix.Orchestration.Agent.Tests.Runspaces.DurableRunspaceTests.PauseAsync_CancelsCurrentExecution_AndSetsPausedStatus [1 ms]
  Error Message:
   System.NotSupportedException : Legacy DurableRunspace execution is disabled. Use canonical dispatch with a persisted cursor.
  Stack Trace:
     at Helix.Orchestration.Agent.Runspaces.DurableRunspace.StartAsync(CancellationToken cancellationToken) in C:\Users\u643664\source\repos\CSharpGitLab\Helix\Helix.Orchestration.Agent\Runspaces\DurableRunspace.cs:line 57
   at Helix.Orchestration.Agent.Tests.Runspaces.DurableRunspaceTests.PauseAsync_CancelsCurrentExecution_AndSetsPausedStatus() in C:\Users\u643664\source\repos\CSharpGitLab\Helix\tests\Helix.Orchestration.Agent.Tests\Runspaces\DurableRunspaceTests.cs:line 33
   at Helix.Orchestration.Agent.Tests.Runspaces.DurableRunspaceTests.PauseAsync_CancelsCurrentExecution_AndSetsPausedStatus() in C:\Users\u643664\source\repos\CSharpGitLab\Helix\tests\Helix.Orchestration.Agent.Tests\Runspaces\DurableRunspaceTests.cs:line 39
--- End of stack trace from previous location ---

Failed!  - Failed:     2, Passed:    48, Skipped:     0, Total:    50, Duration: 10 s - Helix.Orchestration.Agent.Tests.dll (net10.0)
```
</details>

| Infrastructure unit tests | Infrastructure | `dotnet test tests/Helix.Orchestration.Infrastructure.Tests/Helix.Orchestration.Infrastructure.Tests.csproj --no-restore` | 0 | 643664 | 7.85 | passed baseline command (not Phase 2 acceptance evidence) |

<details><summary>Infrastructure unit tests output tail</summary>

```text
C:\Program Files\dotnet\sdk\11.0.100-rc.3.26522.148\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.RuntimeIdentifierInference.targets(385,5): message NETSDK1057: You are using a preview version of .NET. See: https://aka.ms/dotnet-support-policy [C:\Users\u643664\source\repos\CSharpGitLab\Helix\tests\Helix.Orchestration.Infrastructure.Tests\Helix.Orchestration.Infrastructure.Tests.csproj]
C:\Program Files\dotnet\sdk\11.0.100-rc.3.26522.148\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.RuntimeIdentifierInference.targets(385,5): message NETSDK1057: You are using a preview version of .NET. See: https://aka.ms/dotnet-support-policy [C:\Users\u643664\source\repos\CSharpGitLab\Helix\Helix.Orchestration.Core\Helix.Orchestration.Core.csproj]
  Helix.Orchestration.Core -> C:\Users\u643664\source\repos\CSharpGitLab\Helix\Helix.Orchestration.Core\bin\Debug\net10.0\Helix.Orchestration.Core.dll
  Helix.Orchestration.Infrastructure -> C:\Users\u643664\source\repos\CSharpGitLab\Helix\Helix.Orchestration.Infrastructure\bin\Debug\net10.0\Helix.Orchestration.Infrastructure.dll
  Helix.Orchestration.Infrastructure.Tests -> C:\Users\u643664\source\repos\CSharpGitLab\Helix\tests\Helix.Orchestration.Infrastructure.Tests\bin\Debug\net10.0\Helix.Orchestration.Infrastructure.Tests.dll
Test run for C:\Users\u643664\source\repos\CSharpGitLab\Helix\tests\Helix.Orchestration.Infrastructure.Tests\bin\Debug\net10.0\Helix.Orchestration.Infrastructure.Tests.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:     4, Skipped:     0, Total:     4, Duration: 262 ms - Helix.Orchestration.Infrastructure.Tests.dll (net10.0)
```
</details>

| Desktop Vitest suite | Desktop | `npm test --prefix helix-desktop -- --run` | 1 | 5 | 13.96 | baseline failure; not passed evidence |

<details><summary>Desktop Vitest suite output tail</summary>

```text
     ├ù renders error message (role=alert) when fetch fails 108ms
     ├ù does NOT render DeviceManualControl inline ÔÇö only fires onOpenDevice on click 1005ms
 ÔØ» src/components/WorkflowBuilder/DevicePalette.test.tsx (4 tests | 4 failed) 4129ms
     ├ù renders palette-group-Alicat details element after fetch resolves 1079ms
     ├ù capability-card element is draggable 1007ms
     ├ù firing dragstart on a card calls dataTransfer.setData with application/helix-capability key 1023ms
     ├ù shows Failed to load devices message when fetch errors 1016ms
 ÔØ» src/components/WorkflowBuilder/WorkflowBuilder.test.tsx (15 tests | 6 failed) 5412ms
     ├ù shows error when devices fetch fails 95ms
     ├ù fetches devices on mount 1017ms
     ├ù capability-picker renders when devices are fetched 1012ms
     ├ù drop on canvas creates a DeviceAction node 1019ms
     ├ù step-started WebSocket event transitions node to running and shows timing-footer-running 1023ms
     ├ù exportWsl produces nodeType Step with capability parameter for DeviceAction node 1010ms

 Test Files  4 failed | 22 passed (26)
      Tests  20 failed | 105 passed (125)
   Start at  16:06:43
   Duration  12.77s (transform 8.22s, setup 14.15s, import 22.68s, tests 18.30s, environment 118.24s)

```
</details>

| Native Cargo guard tests | Native | `cargo test --manifest-path helix-desktop/src-tauri/Cargo.toml agent_security` | 0 | 0 | 9.4 | unexecuted acceptance: zero discovery; not passed evidence |

<details><summary>Native Cargo guard tests output tail</summary>

```text
test commands::agent::agent_security::broker_never_serializes_its_session_state ... ok
test commands::agent::agent_security::command_ids_do_not_accept_paths ... ok
test commands::agent::agent_security::cursor_does_not_accept_query_injection ... ok
test commands::agent::agent_security::command_ids_require_uuid_shape ... ok
test commands::agent::agent_security::device_ids_do_not_accept_paths ... ok
test commands::agent::agent_security::fixed_agent_target_is_loopback_only_and_uses_no_proxy ... ok
test commands::agent::agent_security::redirect_suppression_uses_request_handle_and_fails_closed ... ok

test result: ok. 7 passed; 0 failed; 0 ignored; 0 measured; 0 filtered out; finished in 0.00s


running 0 tests

test result: ok. 0 passed; 0 failed; 0 ignored; 0 measured; 0 filtered out; finished in 0.00s


running 0 tests

test result: ok. 0 passed; 0 failed; 0 ignored; 0 measured; 0 filtered out; finished in 0.00s

```
</details>

## Candidate Fixture Budgets

| Fixture | Bytes | Candidate byte limit | Counts | Disposition |
| --- | ---: | ---: | --- | --- |
| Topology at candidate count limit | 141625 | 65536 | 100 components; 16 ports/component; 256 connections | Candidate limit exceeded. Proposed adjustment required before schema stabilization; do not widen a native/API bound without tracer evidence. |
| Procedure at candidate node limit | 3517 | 65536 | 128 nodes | Within candidate limit. |
| Transformation at candidate parent/child limit | 442 | 16384 | 32 parents; 32 children | Within candidate limit. |

## Phase 1 Waivers and Unverified Acceptance

- `phase01-smoke.ps1 -Scenario All`, final UI/Tauri smoke, distinct-role migration apply, and clean production build gates remain **waived or unverified, not passed evidence**.
- Existing `NU1603`, `NU1902`, and `MSB3277` diagnostics remain visible baseline concerns when emitted by the affected commands; this script does not suppress or remediate them.
- SQL tests use their existing Testcontainers fixture when available. This script never reads `PGHOST` and never directs a command to a user database.
- The ignored live native fixture and authenticated desktop/native/Agent/isolated-PostgreSQL tracer acceptance remain unexecuted until the dedicated tracer plan.

## Dispositions

- `passed baseline command (not Phase 2 acceptance evidence)`: a pre-existing command completed with nonzero discovery; it does not prove new scientific behavior.
- `baseline failure; not passed evidence`: command failure is recorded for remediation/disposition and must not be described as a pass.
- `unexecuted acceptance: zero discovery; not passed evidence`: a command discovered no tests and is not accepted as verification.
- Candidate-budget exceedance remains a blocking contract adjustment, not authorization to widen limits.
