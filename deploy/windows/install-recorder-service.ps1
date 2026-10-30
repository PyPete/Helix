# Helix Recorder Windows Service Installer
# Requires NSSM (Non-Sucking Service Manager).
# Install NSSM via: winget install nssm  OR  download from https://nssm.cc
# Run as Administrator.

param(
    [string]$InstallPath = "C:\Helix\Recorder",
    [string]$NssmPath = "nssm",
    [string]$DotnetPath = ""
)

if (-not $DotnetPath) {
    $DotnetPath = (Get-Command dotnet -ErrorAction SilentlyContinue).Source
    if (-not $DotnetPath) { Write-Error "dotnet not found on PATH. Install .NET 10 SDK first."; exit 1 }
}

$ServiceName = "HelixRecorder"
$DisplayName = "Helix Orchestration Recorder"
$Description = "Helix experiment data recorder. Subscribes to MQTT and writes experiment events to MinIO."

# Idempotency: remove existing service before reinstalling
$serviceQuery = sc.exe query $ServiceName 2>&1
if ($serviceQuery -notmatch "FAILED 1060") {
    Write-Host "Stopping and removing existing $ServiceName service..."
    & $NssmPath stop $ServiceName 2>$null
    Start-Sleep -Seconds 2
    & $NssmPath remove $ServiceName confirm
}

Write-Host "Installing $ServiceName service..."
& $NssmPath install $ServiceName $DotnetPath "$InstallPath\Helix.Orchestration.Recorder.dll"
& $NssmPath set $ServiceName AppDirectory $InstallPath
& $NssmPath set $ServiceName DisplayName $DisplayName
& $NssmPath set $ServiceName Description $Description
& $NssmPath set $ServiceName Start SERVICE_AUTO_START

& $NssmPath set $ServiceName AppEnvironmentExtra `
    "Recorder__MqttBrokerHost=localhost" `
    "Recorder__MqttBrokerPort=1883" `
    "Minio__ServiceUrl=http://localhost:9000" `
    "Minio__AccessKey=minioadmin" `
    "Minio__SecretKey=minioadmin" `
    "Minio__DefaultBucket=helix-recordings"

& $NssmPath start $ServiceName

Write-Host "$ServiceName service installed and started."
Write-Host "It will start automatically on reboot."
Write-Host "Publish the Recorder project to: $InstallPath before running this script."
