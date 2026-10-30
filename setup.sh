#!/usr/bin/env bash
# setup.sh - Helix Orchestration Platform Linux/Raspberry Pi bootstrap script
# Usage: bash setup.sh   (or: chmod +x setup.sh && ./setup.sh)
#
# This script is idempotent: re-running it is safe and will skip already-satisfied steps.
# Tested on: Debian 12, Ubuntu 22.04 LTS, Raspberry Pi OS (64-bit)

set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
FAILED=0

pass() { echo "  PASS: $1"; }
fail() { echo "  FAIL: $1"; FAILED=1; }

echo "=== Helix Setup (Linux) ==="
echo "Script directory: $SCRIPT_DIR"
echo ""

# ---------------------------------------------------------------------------
# STEP 1 -- Check .NET 10 SDK
# ---------------------------------------------------------------------------
echo "[STEP 1] Checking .NET 10 SDK..."
if ! command -v dotnet &>/dev/null || ! dotnet --version 2>/dev/null | grep -q "^10\."; then
    echo "  Installing .NET 10 SDK..."
    wget -q https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb
    sudo dpkg -i /tmp/packages-microsoft-prod.deb
    sudo apt-get update -y -q
    sudo apt-get install -y -q dotnet-sdk-10.0
    if dotnet --version 2>/dev/null | grep -q "^10\."; then
        pass ".NET SDK $(dotnet --version)"
    else
        fail ".NET 10 SDK installation failed. See: https://dotnet.microsoft.com/download/dotnet/10.0"
    fi
else
    pass ".NET SDK $(dotnet --version)"
fi

# ---------------------------------------------------------------------------
# STEP 2 -- Check Node.js 20+
# ---------------------------------------------------------------------------
echo "[STEP 2] Checking Node.js 20+..."
NODE_MAJOR=0
if command -v node &>/dev/null; then
    NODE_MAJOR=$(node --version | sed 's/v\([0-9]*\).*/\1/')
fi
if [ "$NODE_MAJOR" -lt 20 ]; then
    echo "  Installing Node.js 20 LTS..."
    curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash - 2>/dev/null
    sudo apt-get install -y -q nodejs
    if command -v node &>/dev/null; then
        pass "Node.js $(node --version)"
    else
        fail "Node.js installation failed. See: https://nodejs.org"
    fi
else
    pass "Node.js $(node --version)"
fi

# ---------------------------------------------------------------------------
# STEP 3 -- Check Docker
# ---------------------------------------------------------------------------
echo "[STEP 3] Checking Docker..."
if ! command -v docker &>/dev/null; then
    echo "  Installing Docker..."
    sudo apt-get install -y -q docker.io docker-compose-plugin
fi
if ! docker info &>/dev/null; then
    echo "  Adding user to docker group (re-login required)..."
    sudo usermod -aG docker "$USER" || true
    fail "Docker not accessible. Log out and back in, then re-run."
else
    pass "Docker $(docker --version)"
fi

# ---------------------------------------------------------------------------
# STEP 4 -- Build Helix solution
# ---------------------------------------------------------------------------
echo "[STEP 4] Building Helix solution..."
if dotnet build "$SCRIPT_DIR/Helix.Orchestration.slnx" -c Release --nologo; then
    pass "Solution built."
else
    fail "dotnet build failed."
fi

# ---------------------------------------------------------------------------
# STEP 5 -- Build helix-dashboard
# ---------------------------------------------------------------------------
echo "[STEP 5] Building helix-dashboard..."
if (cd "$SCRIPT_DIR/helix-dashboard" && npm ci && npm run build); then
    pass "helix-dashboard built."
else
    fail "helix-dashboard build failed."
fi

# ---------------------------------------------------------------------------
# STEP 6 -- Install systemd service files
# ---------------------------------------------------------------------------
echo "[STEP 6] Installing systemd service files..."
if command -v systemctl &>/dev/null; then
    if [ ! -f "$SCRIPT_DIR/deploy/systemd/helix-agent.service" ]; then
        fail "deploy/systemd/helix-agent.service not found — complete Plan 10-03 first, then re-run setup.sh"
    else
        sudo cp "$SCRIPT_DIR/deploy/systemd/helix-agent.service" /etc/systemd/system/helix-agent.service
        sudo cp "$SCRIPT_DIR/deploy/systemd/helix-recorder.service" /etc/systemd/system/helix-recorder.service
        sudo systemctl daemon-reload
        sudo systemctl enable helix-agent helix-recorder
        sudo systemctl start helix-agent helix-recorder
        pass "helix-agent and helix-recorder systemd services installed and started."
    fi
else
    pass "systemd not available; skipping service installation (Docker-only mode)."
fi

# ---------------------------------------------------------------------------
# STEP 7 -- Start Helix stack (docker compose up)
# ---------------------------------------------------------------------------
echo "[STEP 7] Starting Helix stack (docker compose up)..."
if docker compose -f "$SCRIPT_DIR/docker-compose.yml" up -d --build; then
    pass "Stack started. Run 'docker compose ps' to check service health."
else
    fail "docker compose up failed."
fi

# ---------------------------------------------------------------------------
# Final summary
# ---------------------------------------------------------------------------
echo ""
echo "=== Setup Summary ==="
if [ "$FAILED" -eq 1 ]; then
    echo "Setup FAILED. Fix the issues above and re-run."
    exit 1
else
    echo "Setup complete. Helix stack is running."
    exit 0
fi
