# Helix Agent Windows Service Installer
# Requires NSSM (Non-Sucking Service Manager).
# Install NSSM via: winget install nssm  OR  download from https://nssm.cc
# Run as Administrator.

param(
    [string]$InstallPath = "C:\Helix\Agent",
    [string]$NssmPath = "nssm",
    [string]$DotnetPath = ""
)

if (-not $DotnetPath) {
    $DotnetPath = (Get-Command dotnet -ErrorAction SilentlyContinue).Source
    if (-not $DotnetPath) {
        Write-Error "dotnet not found on PATH. Install .NET 10 SDK first."
        exit 1
    }
}

$ServiceName = "HelixAgent"
$DisplayName = "Helix Orchestration Agent"
$Description = "Helix lab automation orchestration agent. Manages device adapters, workflow execution, and gRPC API."

# Idempotency: remove existing service before reinstalling
$serviceQuery = sc.exe query $ServiceName 2>&1
if ($serviceQuery -notmatch "FAILED 1060") {
    Write-Host "Stopping and removing existing $ServiceName service..."
    & $NssmPath stop $ServiceName 2>$null
    Start-Sleep -Seconds 2
    & $NssmPath remove $ServiceName confirm
}

Write-Host "Installing $ServiceName service..."
& $NssmPath install $ServiceName $DotnetPath "$InstallPath\Helix.Orchestration.Agent.dll"
& $NssmPath set $ServiceName AppDirectory $InstallPath
& $NssmPath set $ServiceName DisplayName $DisplayName
& $NssmPath set $ServiceName Description $Description
& $NssmPath set $ServiceName Start SERVICE_AUTO_START

& $NssmPath set $ServiceName AppEnvironmentExtra `
    "Kestrel__Endpoints__Http__Url=http://localhost:5080" `
    "Mqtt__BrokerHost=localhost" `
    "Mqtt__BrokerPort=1883"

& $NssmPath start $ServiceName

Write-Host "$ServiceName service installed and started."
Write-Host "It will start automatically on reboot."
Write-Host "Publish the Agent project to: $InstallPath before running this script."
