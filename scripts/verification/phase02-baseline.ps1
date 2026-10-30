[CmdletBinding()]
param(
    [ValidateSet('Run', 'Check')]
    [string]$Mode = 'Check'
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$reportPath = Join-Path $repositoryRoot '.planning/phases/02-scientific-setup-foundation/02-BASELINE.md'
$requiredCommands = @('dotnet', 'npm', 'cargo')
$candidateLimits = [ordered]@{
    SetupRequestBytes = 64KB
    SetupResponseBytes = 256KB
    SampleRequestBytes = 16KB
    SampleResponseBytes = 128KB
    JsonDepth = 32
    Components = 100
    PortsPerComponent = 16
    Connections = 256
    Assertions = 100
    ProcedureNodes = 128
    Parents = 32
    Children = 32
}

function Get-ToolVersion {
    param([string]$Name)

    $command = Get-Command $Name -ErrorAction SilentlyContinue
    if ($null -eq $command) {
        return $null
    }

    try {
        return (& $Name --version 2>&1 | Select-Object -First 1).ToString().Trim()
    }
    catch {
        return 'available (version probe failed)'
    }
}

function Get-DiscoveryCount {
    param([string[]]$Output)

    $text = $Output -join [Environment]::NewLine
    $match = [regex]::Match($text, '(?im)(?:Passed|Total tests|Tests)[^0-9]{0,32}(\d+)')
    if ($match.Success) {
        return [int]$match.Groups[1].Value
    }

    $match = [regex]::Match($text, '(?im)running\s+(\d+)\s+tests')
    if ($match.Success) {
        return [int]$match.Groups[1].Value
    }

    return 0
}

function Invoke-BaselineCommand {
    param(
        [string]$Name,
        [string]$Command,
        [string]$WorkingDirectory,
        [string]$Category
    )

    $started = Get-Date
    Push-Location $WorkingDirectory
    try {
        # Baseline failures are evidence to record, not terminating script errors.
        $previousErrorActionPreference = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        try {
            $output = Invoke-Expression $Command 2>&1
            $exitCode = $LASTEXITCODE
        }
        finally {
            $ErrorActionPreference = $previousErrorActionPreference
        }
    }
    finally {
        Pop-Location
    }

    $elapsed = [math]::Round(((Get-Date) - $started).TotalSeconds, 2)
    $discovered = Get-DiscoveryCount -Output @($output)
    $disposition = if ($exitCode -eq 0 -and $discovered -gt 0) {
        'passed baseline command (not Phase 2 acceptance evidence)'
    }
    elseif ($exitCode -ne 0) {
        'baseline failure; not passed evidence'
    }
    else {
        'unexecuted acceptance: zero discovery; not passed evidence'
    }

    return [pscustomobject]@{
        Name = $Name
        Category = $Category
        Command = $Command
        ExitCode = $exitCode
        Discovered = $discovered
        Seconds = $elapsed
        Disposition = $disposition
        Output = @($output | Select-Object -Last 20)
    }
}

function Get-FixtureMeasurements {
    $components = 1..$candidateLimits.Components | ForEach-Object {
        [ordered]@{
            id = "c$_"
            type = 'DeviceModule'
            roles = @('Control')
            ports = @(1..$candidateLimits.PortsPerComponent | ForEach-Object {
                    [ordered]@{ id = "p$_"; media = 'Signal'; direction = 'Bidirectional'; cardinality = 'Many' }
                })
        }
    }
    $connections = 1..$candidateLimits.Connections | ForEach-Object {
        [ordered]@{ id = "e$_"; from = "c$((($_ - 1) % $candidateLimits.Components) + 1)"; to = "c$(($_ % $candidateLimits.Components) + 1)" }
    }
    $topology = @{ schema = 'scientific-setup/1'; components = $components; connections = $connections }
    $procedure = @{ schema = 'scientific-setup/1'; nodes = @(1..$candidateLimits.ProcedureNodes | ForEach-Object { @{ id = "n$_"; kind = 'step' } }) }
    $transformation = @{ schema = 'scientific-setup/1'; parents = @(1..$candidateLimits.Parents | ForEach-Object { "p$_" }); children = @(1..$candidateLimits.Children | ForEach-Object { "c$_" }); operation = 'Split' }

    $measure = {
        param($Name, $Object, $Limit, $Counts)
        $json = $Object | ConvertTo-Json -Depth 32 -Compress
        $bytes = [Text.Encoding]::UTF8.GetByteCount($json)
        [pscustomobject]@{
            Name = $Name
            Bytes = $bytes
            Limit = $Limit
            Counts = $Counts
            Adjustment = if ($bytes -gt $Limit) { 'Proposed adjustment required before schema stabilization; do not widen a native/API bound without tracer evidence.' } else { 'None.' }
        }
    }

    return @(
        & $measure 'Topology at candidate count limit' $topology $candidateLimits.SetupRequestBytes '100 components; 16 ports/component; 256 connections'
        & $measure 'Procedure at candidate node limit' $procedure $candidateLimits.SetupRequestBytes '128 nodes'
        & $measure 'Transformation at candidate parent/child limit' $transformation $candidateLimits.SampleRequestBytes '32 parents; 32 children'
    )
}

function Test-BaselineReport {
    if (-not (Test-Path $reportPath)) {
        throw "Baseline report is absent: $reportPath"
    }

    $report = Get-Content -Raw $reportPath
    $requiredHeadings = @(
        '## Tool Availability',
        '## Command Evidence',
        '## Candidate Fixture Budgets',
        '## Phase 1 Waivers and Unverified Acceptance',
        '## Dispositions'
    )
    foreach ($heading in $requiredHeadings) {
        if ($report -notmatch [regex]::Escape($heading)) {
            throw "Baseline report is missing required section: $heading"
        }
    }
    if ($report -match 'Phase 1 waiver[^\r\n]*passed') {
        throw 'A Phase 1 waiver is incorrectly reported as passed evidence.'
    }
    if ($report -notmatch 'Exit code' -or $report -notmatch 'Discovered') {
        throw 'Baseline report lacks exact exits or discovery counts.'
    }
    if ($report -notmatch 'baseline failure; not passed evidence|passed baseline command \(not Phase 2 acceptance evidence\)|unexecuted acceptance: zero discovery; not passed evidence') {
        throw 'Baseline report lacks an explicit failure/success disposition.'
    }
    if ($report -match 'Candidate limit exceeded(?![^\r\n]*Proposed adjustment)') {
        throw 'A candidate limit is exceeded without a proposed adjustment.'
    }
    if ($report -match 'Discovered \| 0 \|.*\| passed baseline command') {
        throw 'A zero-discovery command is incorrectly reported as passed.'
    }
}

if ($Mode -eq 'Check') {
    Test-BaselineReport
    Write-Host "Baseline report check passed: $reportPath"
    exit 0
}

$toolVersions = [ordered]@{}
foreach ($tool in $requiredCommands) {
    $toolVersions[$tool] = Get-ToolVersion $tool
    if ($null -eq $toolVersions[$tool]) {
        throw "Required tool is unavailable: $tool"
    }
}

$commands = @(
    @{ Name = 'Core unit tests'; Category = 'Core'; Command = 'dotnet test tests/Helix.Orchestration.Core.Tests/Helix.Orchestration.Core.Tests.csproj --no-restore'; WorkingDirectory = $repositoryRoot },
    @{ Name = 'Agent unit tests'; Category = 'Agent'; Command = 'dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore'; WorkingDirectory = $repositoryRoot },
    @{ Name = 'Infrastructure unit tests'; Category = 'Infrastructure'; Command = 'dotnet test tests/Helix.Orchestration.Infrastructure.Tests/Helix.Orchestration.Infrastructure.Tests.csproj --no-restore'; WorkingDirectory = $repositoryRoot },
    @{ Name = 'Desktop Vitest suite'; Category = 'Desktop'; Command = 'npm test --prefix helix-desktop -- --run'; WorkingDirectory = $repositoryRoot },
    @{ Name = 'Native Cargo guard tests'; Category = 'Native'; Command = 'cargo test --manifest-path helix-desktop/src-tauri/Cargo.toml agent_security'; WorkingDirectory = $repositoryRoot }
)
$results = @($commands | ForEach-Object {
        Invoke-BaselineCommand -Name $_.Name -Category $_.Category -Command $_.Command -WorkingDirectory $_.WorkingDirectory
    })
$fixtures = Get-FixtureMeasurements
$sdk = (& dotnet --info 2>&1 | Select-Object -First 3) -join '; '
$generatedAt = (Get-Date).ToUniversalTime().ToString('o')

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add('# Phase 02 Baseline Evidence')
$lines.Add('')
$lines.Add("Generated UTC: $generatedAt")
$lines.Add('')
$lines.Add('This report records observed baseline evidence only. It neither accesses a live user database nor promotes Phase 1 waivers, unavailable SQL/native acceptance, or successful existing tests into Phase 2 scientific acceptance.')
$lines.Add('')
$lines.Add('## Tool Availability')
$lines.Add('')
$lines.Add('| Tool | Availability / version |')
$lines.Add('| --- | --- |')
foreach ($tool in $requiredCommands) { $lines.Add("| $tool | $($toolVersions[$tool]) |") }
$lines.Add("| .NET SDK | $sdk |")
$lines.Add('')
$lines.Add('## Command Evidence')
$lines.Add('')
$lines.Add('| Name | Category | Command | Exit code | Discovered | Seconds | Disposition |')
$lines.Add('| --- | --- | --- | ---: | ---: | ---: | --- |')
foreach ($result in $results) {
    $lines.Add("| $($result.Name) | $($result.Category) | ``$($result.Command)`` | $($result.ExitCode) | $($result.Discovered) | $($result.Seconds) | $($result.Disposition) |")
    $lines.Add('')
    $lines.Add("<details><summary>$($result.Name) output tail</summary>")
    $lines.Add('')
    $lines.Add('```text')
    foreach ($outputLine in $result.Output) { $lines.Add($outputLine.ToString()) }
    $lines.Add('```')
    $lines.Add('</details>')
    $lines.Add('')
}
$lines.Add('## Candidate Fixture Budgets')
$lines.Add('')
$lines.Add('| Fixture | Bytes | Candidate byte limit | Counts | Disposition |')
$lines.Add('| --- | ---: | ---: | --- | --- |')
foreach ($fixture in $fixtures) {
    $disposition = if ($fixture.Bytes -gt $fixture.Limit) { "Candidate limit exceeded. $($fixture.Adjustment)" } else { 'Within candidate limit.' }
    $lines.Add("| $($fixture.Name) | $($fixture.Bytes) | $($fixture.Limit) | $($fixture.Counts) | $disposition |")
}
$lines.Add('')
$lines.Add('## Phase 1 Waivers and Unverified Acceptance')
$lines.Add('')
$lines.Add('- `phase01-smoke.ps1 -Scenario All`, final UI/Tauri smoke, distinct-role migration apply, and clean production build gates remain **waived or unverified, not passed evidence**.')
$lines.Add('- Existing `NU1603`, `NU1902`, and `MSB3277` diagnostics remain visible baseline concerns when emitted by the affected commands; this script does not suppress or remediate them.')
$lines.Add('- SQL tests use their existing Testcontainers fixture when available. This script never reads `PGHOST` and never directs a command to a user database.')
$lines.Add('- The ignored live native fixture and authenticated desktop/native/Agent/isolated-PostgreSQL tracer acceptance remain unexecuted until the dedicated tracer plan.')
$lines.Add('')
$lines.Add('## Dispositions')
$lines.Add('')
$lines.Add('- `passed baseline command (not Phase 2 acceptance evidence)`: a pre-existing command completed with nonzero discovery; it does not prove new scientific behavior.')
$lines.Add('- `baseline failure; not passed evidence`: command failure is recorded for remediation/disposition and must not be described as a pass.')
$lines.Add('- `unexecuted acceptance: zero discovery; not passed evidence`: a command discovered no tests and is not accepted as verification.')
$lines.Add('- Candidate-budget exceedance remains a blocking contract adjustment, not authorization to widen limits.')

Set-Content -Path $reportPath -Value ($lines -join [Environment]::NewLine) -Encoding utf8
Test-BaselineReport
Write-Host "Baseline report written and checked: $reportPath"
