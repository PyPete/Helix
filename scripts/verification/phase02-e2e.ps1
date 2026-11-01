param([ValidateSet('Tracer', 'Integration', 'Acceptance')][string]$Mode = 'Integration')

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

function Invoke-Verified {
    param([string]$Name, [scriptblock]$Command)

    Write-Host "== $Name =="
    $previousErrorAction = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $output = & $Command 2>&1 | Out-String
    $exitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorAction
    Write-Host $output
    if ($exitCode -ne 0) { throw "$Name failed with exit code $exitCode." }
    if ($output -notmatch '(?ims)(?:Test Run Successful\.\s*Total tests:\s*[1-9]\d*\s*Passed:\s*[1-9]\d*|test result: ok\.\s*[1-9]\d* passed)') {
        throw "$Name did not report a nonzero executed-test count."
    }
    return $output
}

function Invoke-Integration {
    $null = & docker info --format '{{.ServerVersion}}'
    if ($LASTEXITCODE -ne 0) { throw 'Docker is unavailable; integration evidence cannot be skipped.' }

    $dotnetOutput = Invoke-Verified 'Owned authenticated Agent and isolated SQL scenario' {
        dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --filter 'FullyQualifiedName~ScientificEndToEndTests' --logger 'console;verbosity=normal'
    }
    if ($dotnetOutput -notmatch 'Passed:\s*[1-9]\d*') { throw 'The owned Agent scenario did not execute.' }

    $nativeOutput = Invoke-Verified 'Explicit ignored native scientific fixture' {
        cargo test --manifest-path helix-desktop/src-tauri/Cargo.toml commands::agent::agent_security::agent_security_scientific_e2e -- --ignored --exact --nocapture
    }
    if ($nativeOutput -notmatch '1 passed') { throw 'The ignored native fixture did not execute exactly once.' }
}

Push-Location $repositoryRoot
try {
    switch ($Mode) {
        'Tracer' { Invoke-Integration }
        'Integration' { Invoke-Integration }
        'Acceptance' {
            Invoke-Integration
            Invoke-Verified 'Native security guard suite' {
                cargo test --manifest-path helix-desktop/src-tauri/Cargo.toml agent_security
            } | Out-Null
            Invoke-Verified 'Agent test suite' {
                dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --logger 'console;verbosity=normal'
            } | Out-Null
            & dotnet build Helix.Orchestration.Agent/Helix.Orchestration.Agent.csproj --no-restore -warnaserror
            if ($LASTEXITCODE -ne 0) { throw 'Agent warnings-as-errors build failed.' }
            & dotnet format Helix.Orchestration.slnx --verify-no-changes --no-restore
            if ($LASTEXITCODE -ne 0) { throw 'C# formatting verification failed.' }
            & npm --prefix helix-desktop test -- --run
            if ($LASTEXITCODE -ne 0) { throw 'Desktop test suite failed.' }
            & npm --prefix helix-desktop run build
            if ($LASTEXITCODE -ne 0) { throw 'Desktop production build failed.' }
        }
    }
}
finally {
    Pop-Location
}

Write-Host "Phase 02 $Mode evidence completed with checked nonzero discovery and exits."
