param([ValidateSet('Tracer')][string]$Mode = 'Tracer')
$ErrorActionPreference = 'Stop'

if ($Mode -ne 'Tracer') { throw 'Only the Phase 02 tracer mode is supported.' }
if (-not (Test-Path '\\.\pipe\helix-desktop-session')) { throw 'Native desktop session broker is unavailable.' }
if (-not (Get-NetTCPConnection -LocalPort 5080 -State Listen -ErrorAction SilentlyContinue)) { throw 'Agent listener 127.0.0.1:5080 is unavailable.' }

dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --filter 'FullyQualifiedName~ScientificEndToEndTests' --logger 'console;verbosity=minimal'
if ($LASTEXITCODE -ne 0) { throw 'Scientific .NET tracer test failed.' }

cargo test --manifest-path helix-desktop/src-tauri/Cargo.toml commands::agent::agent_security::agent_security_scientific_e2e -- --ignored --exact --nocapture
if ($LASTEXITCODE -ne 0) { throw 'Scientific native tracer test failed.' }

Write-Host 'Phase 02 tracer prerequisites and focused native/.NET checks passed.'
