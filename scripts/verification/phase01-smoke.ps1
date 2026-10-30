param(
    [ValidateSet('Tracer', 'All')]
    [string]$Scenario = 'Tracer'
)

$ErrorActionPreference = 'Stop'
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

function Get-ProcessTreeIds([int]$RootProcessId) {
    $processIds = [System.Collections.Generic.HashSet[int]]::new()
    $pending = [System.Collections.Generic.Queue[int]]::new()
    $pending.Enqueue($RootProcessId)

    while ($pending.Count -gt 0) {
        $processId = $pending.Dequeue()
        if (-not $processIds.Add($processId)) { continue }

        Get-CimInstance Win32_Process -Filter "ParentProcessId = $processId" -ErrorAction SilentlyContinue |
            ForEach-Object { $pending.Enqueue([int]$_.ProcessId) }
    }

    return $processIds
}

function Stop-ProcessTree($Process) {
    if ($null -eq $Process) { return }

    # `npm tauri dev` owns cargo, Vite and the native WebView host. Stopping only
    # npm leaves Helix Desktop running with a session for the previous Agent.
    # A later run can otherwise find that stale window by title and click it.
    Get-ProcessTreeIds $Process.Id |
        Sort-Object -Descending |
        ForEach-Object { Stop-Process -Id $_ -Force -ErrorAction SilentlyContinue }
}

function Invoke-AccessibleElement(
    [System.Windows.Automation.AutomationElement]$Element,
    [System.Windows.Automation.AutomationElement]$Window,
    [string]$Name) {
    # UI Automation may find the named text child rather than its actionable
    # ribbon/tab ancestor. Walk only its control ancestry so the real renderer
    # action is still invoked instead of substituting an HTTP request.
    if ($Window.Current.NativeWindowHandle -ne 0) {
        [HelixSmoke.NativeMouse]::SetForegroundWindow([IntPtr]$Window.Current.NativeWindowHandle) | Out-Null
        Start-Sleep -Milliseconds 250
    }

    $current = $Element
    while ($null -ne $current) {
        try {
            $pattern = $current.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
            ([System.Windows.Automation.InvokePattern]$pattern).Invoke()
            return
        } catch [System.InvalidOperationException] {
            if ($Name -match 'tab' -and $current.Current.IsKeyboardFocusable) {
                # WebView2 exposes the ribbon through its selection provider;
                # select first, then use keyboard activation for providers that
                # do not implement SelectionItem.
                try {
                    $selection = $current.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
                    ([System.Windows.Automation.SelectionItemPattern]$selection).Select()
                    return
                } catch [System.InvalidOperationException] {
                    $current.SetFocus()
                    [System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
                    return
                }
            }

            $point = [System.Windows.Point]::new(0, 0)
            if ($current.TryGetClickablePoint([ref]$point)) {
                # WebView2 can expose an HTML control as a text child without
                # InvokePattern. Click its actual screen point so the browser
                # dispatches the same renderer event as a scientist.
                [System.Windows.Forms.Cursor]::Position = [System.Drawing.Point]::new([int]$point.X, [int]$point.Y)
                Start-Sleep -Milliseconds 100
                [HelixSmoke.NativeMouse]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
                [HelixSmoke.NativeMouse]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
                return
            }

            if ($current.Current.IsKeyboardFocusable) {
                # WebView2 exposes some HTML buttons/tabs only as keyboard
                # controls. Enter travels through the rendered UI and raises
                # the real React click handler; SelectionItem.Select does not.
                $current.SetFocus()
                [System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
                return
            }

            $current = [System.Windows.Automation.TreeWalker]::ControlViewWalker.GetParent($current)
        }
    }

    throw "The $Name UI Automation element has no actionable control ancestor."
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

    if ($Scenario -eq 'All') {
        # This is intentionally a separate, blocking gate.  A smoke run cannot
        # certify a schema that was merely present in a Docker init volume.
        & "$PSScriptRoot/phase01-migrate.ps1" -Mode CheckAndApply
        if ($LASTEXITCODE -ne 0) { throw 'Final schema/runtime-grant gate failed.' }
    }

    # The tracer uses a disposable development database.  Full acceptance above
    # instead requires independently supplied migration and runtime identities.
    Get-Content 'postgres/migrations/003_agent_command_ledger.sql' -Raw | docker exec -i helix-postgres psql -v ON_ERROR_STOP=1 -U helix -d helix_orchestration | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Unable to apply the command-ledger migration.' }
    # The ledger's DELETE trigger intentionally rejects history rewriting. The
    # disposable tracer database must instead reset whole fact tables, including
    # related dispatch/observation facts that reference command intents.
    Invoke-Postgres 'TRUNCATE TABLE command_evidence, command_review_notes, command_dispatches, command_intents, command_streams, simulator_device_state;' | Out-Null

    docker compose stop helix-agent | Out-Null
    $env:ASPNETCORE_URLS = 'http://127.0.0.1:5080'
    $env:ConnectionStrings__HelixOrchestration = 'Host=127.0.0.1;Port=5432;Database=helix_orchestration;Username=helix;Password=helix_dev_password'
    $agent = Start-Process dotnet -ArgumentList 'run','--no-build','--project','Helix.Orchestration.Agent/Helix.Orchestration.Agent.csproj' -PassThru
    Wait-Until { (Test-NetConnection -ComputerName 127.0.0.1 -Port 5080 -InformationLevel Quiet) } 'Real Agent child process did not listen on 127.0.0.1:5080.'
    Write-Host '[TRACE] Local Agent is listening.'
    $listeners = Get-NetTCPConnection -State Listen -LocalPort 5080 -ErrorAction Stop
    if ($listeners | Where-Object { $_.LocalAddress -notin @('127.0.0.1', '::1') }) {
        throw 'Agent opened a non-loopback listener.'
    }
    try {
        Invoke-WebRequest -UseBasicParsing 'http://127.0.0.1:5080/health' | Out-Null
        throw 'Anonymous health request unexpectedly succeeded.'
    } catch {
        $statusCode = if ($null -ne $_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
        if ($statusCode -notin @(401, 403)) { throw }
    }

    if ($Scenario -eq 'All') {
        # The native binary is a second transport implementation and must not be
        # replaced with a PowerShell HTTP probe.
        cargo run --quiet --manifest-path helix-desktop/src-tauri/Cargo.toml --bin agent_broker_smoke
        if ($LASTEXITCODE -ne 0) { throw 'Native broker smoke failed.' }

        $priorUrls = $env:ASPNETCORE_URLS
        $env:ASPNETCORE_URLS = 'http://0.0.0.0:5081'
        $wildcard = Start-Process dotnet -ArgumentList 'run','--no-build','--project','Helix.Orchestration.Agent/Helix.Orchestration.Agent.csproj' -PassThru -NoNewWindow -RedirectStandardError "$env:TEMP/helix-wildcard-refusal.err" -RedirectStandardOutput "$env:TEMP/helix-wildcard-refusal.out"
        if (-not $wildcard.WaitForExit(15000)) {
            Stop-ProcessTree $wildcard
            throw 'Agent did not refuse a wildcard listener override within 15 seconds.'
        }
        $env:ASPNETCORE_URLS = $priorUrls
        if ($wildcard.ExitCode -eq 0) { throw 'Agent accepted a wildcard listener override.' }
        Write-Host '[TRACE] Wildcard listener override was refused.'
    }

    # Tauri dev launches the real WebView2 main window and the production native
    # command module. UI Automation presses the visible renderer button; no invoke
    # call is fabricated by this tracer. Match the window to this process tree,
    # rather than title alone: a stale prior test window may hold an invalid token.
    $desktop = Start-Process npm.cmd -WorkingDirectory 'helix-desktop' -ArgumentList 'run','tauri','dev' -PassThru
    Write-Host '[TRACE] Tauri development host started.'
    Add-Type -AssemblyName UIAutomationClient
    Add-Type -AssemblyName UIAutomationTypes
    Add-Type -AssemblyName System.Windows.Forms
    Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

namespace HelixSmoke
{
    public static class NativeMouse
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
'@
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $windowCondition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'Helix Desktop')
    $devicesTabCondition = [System.Windows.Automation.AndCondition]::new(
        [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty, 'Devices'),
        [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::TabItem))
    $manualControlCondition = [System.Windows.Automation.AndCondition]::new(
        [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty, 'Open Manual Control'),
        [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button))
    $buttonCondition = [System.Windows.Automation.AndCondition]::new(
        [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty, 'Set Flow'),
        [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button))
    $script:tracerWindow = $null
    $script:devicesTab = $null
    $script:manualControlButton = $null
    $script:tracerButton = $null
    Wait-Until {
        $desktopProcessIds = Get-ProcessTreeIds $desktop.Id
        $script:tracerWindow = $null
        $root.FindAll([System.Windows.Automation.TreeScope]::Children, $windowCondition) |
            ForEach-Object {
                if ($desktopProcessIds.Contains($_.Current.ProcessId)) { $script:tracerWindow = $_ }
            }
        if ($null -eq $script:tracerWindow) { return $false }
        $script:devicesTab = $script:tracerWindow.FindFirst([System.Windows.Automation.TreeScope]::Subtree, $devicesTabCondition)
        return $null -ne $script:devicesTab -and $script:devicesTab.Current.IsEnabled
    } 'The Tauri process started by this tracer did not render an enabled Devices ribbon tab.'
    Invoke-AccessibleElement $script:devicesTab $script:tracerWindow 'Devices ribbon tab'
    Write-Host '[TRACE] Devices ribbon tab opened.'
    Wait-Until {
        $script:manualControlButton = $script:tracerWindow.FindFirst([System.Windows.Automation.TreeScope]::Subtree, $manualControlCondition)
        return $null -ne $script:manualControlButton -and $script:manualControlButton.Current.IsEnabled
    } 'The Tauri process did not render an enabled Open Manual Control action.'
    Invoke-AccessibleElement $script:manualControlButton $script:tracerWindow 'Open Manual Control action'
    Write-Host '[TRACE] Manual control panel opened.'
    Wait-Until {
        $script:tracerButton = $script:tracerWindow.FindFirst([System.Windows.Automation.TreeScope]::Subtree, $buttonCondition)
        return $null -ne $script:tracerButton -and $script:tracerButton.Current.IsEnabled
    } 'The Tauri process did not render an enabled Set Flow control.'
    Invoke-AccessibleElement $script:tracerButton $script:tracerWindow 'Set Flow control'
    Write-Host '[TRACE] Set Flow proposal invoked.'

    Wait-Until {
        $row = Invoke-Postgres "SELECT i.command_id || '|' || e.admission_state || '|' || jsonb_array_length(e.checks_json) FROM command_intents i JOIN command_evidence e ON e.command_id=i.command_id WHERE i.capability='SetFlow' ORDER BY e.occurred_at DESC LIMIT 1;"
        if ($row -match '^[0-9a-f-]+\|Allowed\|7$') { $script:commandRow = $row; return $true }
        return $false
    } 'The Tauri-renderer action did not produce an allowed durable SetFlow evidence row.'

    $commandId = ($commandRow -split '\|')[0]
    Write-Host "[TRACE] Allowed durable SetFlow evidence found ($commandId)."
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

    Write-Host "Tracer passed: real Tauri WebView action, Agent, PostgreSQL evidence, and restart persistence ($commandId)."
}
finally {
    Stop-ProcessTree $desktop
    Stop-ProcessTree $agent
    Remove-Item Env:ASPNETCORE_URLS -ErrorAction SilentlyContinue
    Remove-Item Env:ConnectionStrings__HelixOrchestration -ErrorAction SilentlyContinue
    Pop-Location
}
