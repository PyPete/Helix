param([ValidateSet('Tracer')][string]$Mode = 'Tracer')
$ErrorActionPreference = 'Stop'

if ($Mode -ne 'Tracer') { throw 'Only the Phase 02 tracer mode is supported.' }

# The focused test owns an isolated Testcontainers PostgreSQL instance and a production-composed,
# loopback Kestrel Agent. It issues a DesktopSessionService Bearer token, the same authenticated
# HTTP route used by the native Tauri client, so no desktop process or user service is required.
dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --filter 'FullyQualifiedName~ScientificEndToEndTests' --logger 'console;verbosity=normal'
if ($LASTEXITCODE -ne 0) { throw 'Scientific production-authenticated tracer test failed.' }

Write-Host 'Phase 02 tracer proved Agent Bearer authentication, owner-visible receipt, idempotency/conflict, restart readback, runtime mutation denial, and zero command dispatch.'
