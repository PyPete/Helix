# Helix Pre-flight Check Script
# Usage: .\preflight.ps1 [optional overrides]
# Exit codes: 0=PASS, 1=WARN, 2=FAIL
# Run before starting an experiment to verify all infrastructure is reachable.

param(
    [string]$PostgresHost = "localhost",
    [int]$PostgresPort = 5432,
    [string]$MqttHost = "localhost",
    [int]$MqttPort = 1883,
    [string]$MinioEndpoint = "http://localhost:9000",
    [string]$AgentGrpcHost = "localhost",
    [int]$AgentGrpcPort = 5080
)

$results = @()

function Add-Result {
    param([string]$Check, [string]$Status, [string]$Detail)
    $script:results += [PSCustomObject]@{ Check = $Check; Status = $Status; Detail = $Detail }
}

function Test-TcpPort {
    param([string]$Host, [int]$Port, [int]$TimeoutMs = 3000)
    try {
        $tcp = New-Object System.Net.Sockets.TcpClient
        $connect = $tcp.BeginConnect($Host, $Port, $null, $null)
        $wait = $connect.AsyncWaitHandle.WaitOne($TimeoutMs, $false)
        if ($wait) { $tcp.EndConnect($connect); $tcp.Close(); return $true }
        $tcp.Close(); return $false
    } catch { return $false }
}

# Check 1: PostgreSQL TCP
Write-Host "Checking PostgreSQL ($PostgresHost`:$PostgresPort)..."
if (Test-TcpPort -Host $PostgresHost -Port $PostgresPort) {
    Add-Result "PostgreSQL TCP" "PASS" "$PostgresHost`:$PostgresPort reachable"
} else {
    Add-Result "PostgreSQL TCP" "FAIL" "$PostgresHost`:$PostgresPort unreachable"
}

# Check 2: MQTT Broker TCP
Write-Host "Checking MQTT broker ($MqttHost`:$MqttPort)..."
if (Test-TcpPort -Host $MqttHost -Port $MqttPort) {
    Add-Result "MQTT Broker TCP" "PASS" "$MqttHost`:$MqttPort reachable"
} else {
    Add-Result "MQTT Broker TCP" "FAIL" "$MqttHost`:$MqttPort unreachable"
}

# Check 3: MinIO HTTP health
Write-Host "Checking MinIO ($MinioEndpoint)..."
try {
    $resp = Invoke-WebRequest -Uri "$MinioEndpoint/minio/health/live" -TimeoutSec 5 -UseBasicParsing -ErrorAction Stop
    Add-Result "MinIO Health" "PASS" "HTTP $($resp.StatusCode)"
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode) {
        Add-Result "MinIO Health" "WARN" "HTTP $statusCode (MinIO may not be running)"
    } else {
        Add-Result "MinIO Health" "WARN" "Connection refused (MinIO may not be running)"
    }
}

# Check 4: Agent HTTP health
Write-Host "Checking Agent (http://${AgentGrpcHost}:${AgentGrpcPort}/health)..."
try {
    $resp = Invoke-WebRequest -Uri "http://${AgentGrpcHost}:${AgentGrpcPort}/health" -TimeoutSec 5 -UseBasicParsing -ErrorAction Stop
    Add-Result "Agent Health" "PASS" "HTTP $($resp.StatusCode)"
} catch {
    Add-Result "Agent Health" "WARN" "Connection refused (Agent may not be started yet)"
}

# Check 5: OPC UA AutoAcceptUntrustedCertificates config
Write-Host "Checking OPC UA certificate config..."
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$agentSettings = Join-Path (Split-Path -Parent (Split-Path -Parent $scriptDir)) "Helix.Orchestration.Agent\appsettings.json"
if (Test-Path $agentSettings) {
    $settings = Get-Content $agentSettings | ConvertFrom-Json
    $opcUa = $settings.OpcUa
    if ($opcUa -and $opcUa.PSObject.Properties['AutoAcceptUntrustedCertificates']) {
        if ($opcUa.AutoAcceptUntrustedCertificates -eq $true) {
            Add-Result "OPC UA Certs" "WARN" "AutoAcceptUntrustedCertificates=true (insecure; set to false for production)"
        } else {
            Add-Result "OPC UA Certs" "PASS" "AutoAcceptUntrustedCertificates=false"
        }
    } else {
        Add-Result "OPC UA Certs" "WARN" "OpcUa section not found in appsettings.json (may not be configured)"
    }
} else {
    Add-Result "OPC UA Certs" "WARN" "appsettings.json not found at expected path"
}

# Check 6: Serial ports (Windows)
Write-Host "Checking serial ports..."
$comPorts = [System.IO.Ports.SerialPort]::GetPortNames()
if ($comPorts.Count -gt 0) {
    Add-Result "Serial Ports" "PASS" "Found: $($comPorts -join ', ')"
} else {
    Add-Result "Serial Ports" "WARN" "No COM ports detected -- check USB-serial adapters if serial devices are needed"
}

# Summary
Write-Host ""
Write-Host "=== Helix Pre-flight Report ==="
$results | Format-Table -AutoSize

$failCount = ($results | Where-Object { $_.Status -eq "FAIL" }).Count
$warnCount = ($results | Where-Object { $_.Status -eq "WARN" }).Count

if ($failCount -gt 0) {
    Write-Host "Result: FAIL ($failCount failure(s), $warnCount warning(s))"
    exit 2
} elseif ($warnCount -gt 0) {
    Write-Host "Result: WARN ($warnCount warning(s)) -- review warnings before starting experiment"
    exit 1
} else {
    Write-Host "Result: PASS (all $($results.Count) checks green)"
    exit 0
}
