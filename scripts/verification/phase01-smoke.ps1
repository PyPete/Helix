param(
    [ValidateSet('Tracer')]
    [string]$Scenario = 'Tracer'
)

$ErrorActionPreference = 'Stop'
if ($Scenario -ne 'Tracer') { throw 'Unsupported scenario.' }
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Push-Location $repositoryRoot

function Wait-Until([scriptblock]$Condition, [string]$Failure) {
    $deadline = [DateTime]::UtcNow.AddSeconds(90)
    do {
        if (& $Condition) { return }
        Start-Sleep -Milliseconds 500
    } while ([DateTime]::UtcNow -lt $deadline)
    throw $Failure
}

function Invoke-Postgres([string]$Sql) {
    $Sql | docker exec -i helix-postgres psql -v ON_ERROR_STOP=1 -U helix -d helix_orchestration -At 2>$null
    if ($LASTEXITCODE -ne 0) { throw 'The required Docker PostgreSQL query failed.' }
}

$agent = $null
$desktop = $null
try {
    docker compose up -d postgres mosquitto | Out-Null
    docker compose ps postgres | Select-String 'healthy' | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL tracer prerequisite is not healthy.' }

    # Existing volumes do not rerun init scripts, so apply the append-only ledger
    # schema before launching the real Agent child process.
    Get-Content 'postgres/migrations/003_agent_command_ledger.sql' -Raw | docker exec -i helix-postgres psql -v ON_ERROR_STOP=1 -U helix -d helix_orchestration | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Unable to apply the command-ledger migration.' }
    Invoke-Postgres 'DELETE FROM command_evidence; DELETE FROM command_intents; DELETE FROM command_streams;' | Out-Null

    docker compose stop helix-agent | Out-Null
    $environment:ASPNETCORE_URLS = 'http://127.0.0.1:5080'
    $environment:ConnectionStrings__HelixOrchestration = 'Host=127.0.0.1;Port=5432;Database=helix_orchestration;Username=helix;Password=helix_dev_password'
    $agent = Start-Process dotnet -ArgumentList 'run','--no-build','--project','Helix.Orchestration.Agent/Helix.Orchestration.Agent.csproj' -PassThru
    Wait-Until { (Test-NetConnection -ComputerName 127.0.0.1 -Port 5080 -InformationLevel Quiet) } 'Real Agent child process did not listen on 127.0.0.1:5080.'

    # Tauri dev launches the real WebView2 main window and the production native
    # command module. UI Automation presses the visible renderer button; no invoke
    # call is fabricated by this tracer.
    $desktop = Start-Process npm.cmd -WorkingDirectory 'helix-desktop' -ArgumentList 'run','tauri','dev' -PassThru
    Add-Type -AssemblyName UIAutomationClient
    Add-Type -AssemblyName UIAutomationTypes
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $windowCondition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'Helix Desktop')
    $buttonCondition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'Set Flow')
    $button = $null
    Wait-Until {
        $window = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $windowCondition)
        if ($null -eq $window) { return $false }
        $script:button = $window.FindFirst([System.Windows.Automation.TreeScope]::Subtree, $buttonCondition)
        return $null -ne $script:button
    } 'Actual Tauri main webview did not render the Set Flow control.'
    $invoke = $button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    ([System.Windows.Automation.InvokePattern]$invoke).Invoke()

    Wait-Until {
        $row = Invoke-Postgres "SELECT i.command_id || '|' || e.admission_state || '|' || jsonb_array_length(e.checks_json) FROM command_intents i JOIN command_evidence e ON e.command_id=i.command_id WHERE i.capability='SetFlow' ORDER BY e.occurred_at DESC LIMIT 1;"
        if ($row -match '^[0-9a-f-]+\|Allowed\|7$') { $script:commandRow = $row; return $true }
        return $false
    } 'The Tauri-renderer action did not produce an allowed durable SetFlow evidence row.'

    $commandId = ($commandRow -split '\|')[0]
    $actor = Invoke-Postgres "SELECT verified_actor_sid FROM command_intents WHERE command_id='$commandId';"
    if ([string]::IsNullOrWhiteSpace($actor)) { throw 'Evidence is missing the verified desktop actor.' }
    $effectCount = Invoke-Postgres "SELECT count(*) FROM command_evidence WHERE command_id='$commandId';"
    if ($effectCount.Trim() -ne '1') { throw 'The command did not have exactly one immutable evidence record.' }

    # Restart the real Agent and verify the ledger remains available without a
    # duplicate mutation. The restarted process has no authority to replay it.
    Stop-Process -Id $agent.Id -Force
    $agent = Start-Process dotnet -ArgumentList 'run','--no-build','--project','Helix.Orchestration.Agent/Helix.Orchestration.Agent.csproj' -PassThru
    Wait-Until { (Test-NetConnection -ComputerName 127.0.0.1 -Port 5080 -InformationLevel Quiet) } 'Restarted Agent did not listen.'
    $postRestartCount = Invoke-Postgres "SELECT count(*) FROM command_evidence WHERE command_id='$commandId';"
    if ($postRestartCount.Trim() -ne '1') { throw 'Agent restart replayed or lost the command evidence.' }

    Write-Host "Tracer passed: real Tauri WebView action, native broker, Agent, PostgreSQL evidence, and restart persistence ($commandId)."
}
finally {
    if ($desktop -and -not $desktop.HasExited) { Stop-Process -Id $desktop.Id -Force -ErrorAction SilentlyContinue }
    if ($agent -and -not $agent.HasExited) { Stop-Process -Id $agent.Id -Force -ErrorAction SilentlyContinue }
    Remove-Item Env:ASPNETCORE_URLS -ErrorAction SilentlyContinue
    Remove-Item Env:ConnectionStrings__HelixOrchestration -ErrorAction SilentlyContinue
    Pop-Location
}
