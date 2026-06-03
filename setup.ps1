# setup.ps1 - Helix Orchestration Platform Windows bootstrap script
# Usage: .\setup.ps1
# Requirements: PowerShell 5.1+, winget available (Windows 10 1809+ / Windows Package Manager)
#
# This script is idempotent: re-running it is safe and will skip already-satisfied steps.

$ErrorActionPreference = "Continue"
$failed = $false

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "=== Helix Setup (Windows) ==="
Write-Host "Script directory: $scriptDir"
Write-Host ""

# ---------------------------------------------------------------------------
# STEP 1 -- Check .NET 10 SDK
# ---------------------------------------------------------------------------
Write-Host "[STEP 1] Checking .NET 10 SDK..."
$dotnetVersion = dotnet --version 2>$null
if ($LASTEXITCODE -ne 0 -or -not ($dotnetVersion -match "^10\.")) {
    Write-Host "  Installing .NET 10 SDK via winget..."
    winget install Microsoft.DotNet.SDK.10 --silent --accept-package-agreements --accept-source-agreements 2>$null
    $dotnetVersion = dotnet --version 2>$null
    if ($LASTEXITCODE -ne 0 -or -not ($dotnetVersion -match "^10\.")) {
        Write-Host "  FAIL: .NET 10 SDK not found. Install from: https://dotnet.microsoft.com/download/dotnet/10.0"
        $failed = $true
    } else {
        Write-Host "  PASS: .NET SDK $dotnetVersion"
    }
} else {
    Write-Host "  PASS: .NET SDK $dotnetVersion"
}

# ---------------------------------------------------------------------------
# STEP 2 -- Check Node.js 20+
# ---------------------------------------------------------------------------
Write-Host "[STEP 2] Checking Node.js 20+..."
$nodeVersion = node --version 2>$null
$nodeMajor = if ($nodeVersion) { [int]($nodeVersion -replace 'v(\d+).*','$1') } else { 0 }
if ($LASTEXITCODE -ne 0 -or $nodeMajor -lt 20) {
    Write-Host "  Installing Node.js LTS via winget..."
    winget install OpenJS.NodeJS.LTS --silent --accept-package-agreements --accept-source-agreements 2>$null
    $nodeVersion = node --version 2>$null
    $nodeMajor = if ($nodeVersion) { [int]($nodeVersion -replace 'v(\d+).*','$1') } else { 0 }
    if ($nodeMajor -lt 20) {
        Write-Host "  FAIL: Node.js 20+ not found. Install from: https://nodejs.org"
        $failed = $true
    } else {
        Write-Host "  PASS: Node.js $nodeVersion"
    }
} else {
    Write-Host "  PASS: Node.js $nodeVersion"
}

# ---------------------------------------------------------------------------
# STEP 3 -- Check Docker Desktop
# ---------------------------------------------------------------------------
Write-Host "[STEP 3] Checking Docker Desktop..."
docker info 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "  FAIL: Docker Desktop is not running or not installed."
    Write-Host "        Install from: https://www.docker.com/products/docker-desktop"
    Write-Host "        Ensure Docker Desktop is running before re-running this script."
    $failed = $true
} else {
    Write-Host "  PASS: Docker Desktop is running."
}

# ---------------------------------------------------------------------------
# STEP 4 -- Build Helix solution
# ---------------------------------------------------------------------------
Write-Host "[STEP 4] Building Helix solution..."
dotnet build "$scriptDir\Helix.Orchestration.slnx" -c Release --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host "  FAIL: dotnet build failed."
    $failed = $true
} else {
    Write-Host "  PASS: Solution built successfully."
}

# ---------------------------------------------------------------------------
# STEP 5 -- Build helix-dashboard
# ---------------------------------------------------------------------------
Write-Host "[STEP 5] Building helix-dashboard..."
Push-Location "$scriptDir\helix-dashboard"
npm ci
if ($LASTEXITCODE -ne 0) {
    Write-Host "  FAIL: npm ci failed."
    $failed = $true
    Pop-Location
    # Early exit from steps below but continue to final summary
} else {
    npm run build
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  FAIL: npm run build failed."
        $failed = $true
        Pop-Location
    } else {
        Pop-Location
        Write-Host "  PASS: helix-dashboard built."
    }
}

# ---------------------------------------------------------------------------
# STEP 6 -- Start compose stack
# ---------------------------------------------------------------------------
Write-Host "[STEP 6] Starting Helix stack (docker compose up)..."
docker compose -f "$scriptDir\docker-compose.yml" up -d --build
if ($LASTEXITCODE -ne 0) {
    Write-Host "  FAIL: docker compose up failed."
    $failed = $true
} else {
    Write-Host "  PASS: Stack started."
    Write-Host "  Run 'docker compose ps' to check service health."
}

# ---------------------------------------------------------------------------
# Final summary
# ---------------------------------------------------------------------------
Write-Host ""
if ($failed) {
    Write-Host "Setup FAILED. Fix the issues above and re-run."
    exit 1
} else {
    Write-Host "Setup complete. Helix stack is running."
    exit 0
}
