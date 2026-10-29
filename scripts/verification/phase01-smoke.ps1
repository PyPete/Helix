param(
    [ValidateSet('Tracer')]
    [string]$Scenario = 'Tracer'
)

$ErrorActionPreference = 'Stop'

if ($Scenario -ne 'Tracer') { throw "Unsupported scenario: $Scenario" }

docker compose ps postgres | Select-String 'healthy' | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL tracer prerequisite is not healthy.' }

$desktopManifest = 'helix-desktop/src-tauri/Cargo.toml'
if (-not (Test-Path $desktopManifest)) { throw 'Tauri desktop manifest is missing.' }

# This gate intentionally refuses a mocked IPC or standalone broker. The complete
# tracer must drive the built main webview through the broker and inspect durable
# evidence; do not replace this failure with a unit-test pass.
throw 'Tracer is not yet runnable: actual Tauri webview automation and durable evidence retrieval are not implemented.'
