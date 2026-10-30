#!/usr/bin/env bash
# Helix Pre-flight Check Script (Linux)
# Usage: ./preflight.sh
# Exit codes: 0=PASS, 1=WARN, 2=FAIL
# Environment variable overrides:
#   POSTGRES_HOST, POSTGRES_PORT, MQTT_HOST, MQTT_PORT,
#   MINIO_ENDPOINT, AGENT_GRPC_HOST, AGENT_GRPC_PORT
set -uo pipefail

POSTGRES_HOST=${POSTGRES_HOST:-localhost}
POSTGRES_PORT=${POSTGRES_PORT:-5432}
MQTT_HOST=${MQTT_HOST:-localhost}
MQTT_PORT=${MQTT_PORT:-1883}
MINIO_ENDPOINT=${MINIO_ENDPOINT:-http://localhost:19000}
AGENT_GRPC_HOST=${AGENT_GRPC_HOST:-localhost}
AGENT_GRPC_PORT=${AGENT_GRPC_PORT:-5080}

PASS_COUNT=0
WARN_COUNT=0
FAIL_COUNT=0

check_result() {
    local name=$1 status=$2 detail=$3
    printf "%-35s %-6s %s\n" "$name" "$status" "$detail"
    case "$status" in
        PASS) PASS_COUNT=$((PASS_COUNT+1)) ;;
        WARN) WARN_COUNT=$((WARN_COUNT+1)) ;;
        FAIL) FAIL_COUNT=$((FAIL_COUNT+1)) ;;
    esac
}

# Ensure nc is available
if ! command -v nc &>/dev/null; then
    echo "Installing netcat-openbsd..."
    sudo apt-get install -y -q netcat-openbsd 2>/dev/null || true
fi

echo "Running Helix pre-flight checks..."
echo ""

# Check 1: PostgreSQL TCP
if nc -z -w3 "$POSTGRES_HOST" "$POSTGRES_PORT" 2>/dev/null; then
    check_result "PostgreSQL TCP" "PASS" "$POSTGRES_HOST:$POSTGRES_PORT reachable"
else
    check_result "PostgreSQL TCP" "FAIL" "$POSTGRES_HOST:$POSTGRES_PORT unreachable"
fi

# Check 2: MQTT Broker TCP
if nc -z -w3 "$MQTT_HOST" "$MQTT_PORT" 2>/dev/null; then
    check_result "MQTT Broker TCP" "PASS" "$MQTT_HOST:$MQTT_PORT reachable"
else
    check_result "MQTT Broker TCP" "FAIL" "$MQTT_HOST:$MQTT_PORT unreachable"
fi

# Check 3: MinIO HTTP health
if curl -sf --max-time 5 "$MINIO_ENDPOINT/minio/health/live" &>/dev/null; then
    check_result "MinIO Health" "PASS" "HTTP 200"
else
    check_result "MinIO Health" "WARN" "Connection refused (MinIO may not be running)"
fi

# Check 4: Agent HTTP health
if curl -sf --max-time 5 "http://$AGENT_GRPC_HOST:$AGENT_GRPC_PORT/health" &>/dev/null; then
    check_result "Agent Health" "PASS" "HTTP 200"
else
    check_result "Agent Health" "WARN" "Connection refused (Agent may not be started yet)"
fi

# Check 5: OPC UA AutoAcceptUntrustedCertificates
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
AGENT_SETTINGS="$SCRIPT_DIR/../../Helix.Orchestration.Agent/appsettings.json"
if [ -f "$AGENT_SETTINGS" ]; then
    if command -v python3 &>/dev/null; then
        AUTO_ACCEPT=$(python3 -c "
import json, sys
try:
    with open('$AGENT_SETTINGS') as f: d=json.load(f)
    print(d.get('OpcUa',{}).get('AutoAcceptUntrustedCertificates','notset'))
except: print('error')
" 2>/dev/null)
    elif command -v jq &>/dev/null; then
        AUTO_ACCEPT=$(jq -r '.OpcUa.AutoAcceptUntrustedCertificates // "notset"' "$AGENT_SETTINGS" 2>/dev/null || echo "error")
    else
        AUTO_ACCEPT="notset"
    fi
    case "$AUTO_ACCEPT" in
        true)  check_result "OPC UA Certs" "WARN" "AutoAcceptUntrustedCertificates=true (set false for production)" ;;
        false) check_result "OPC UA Certs" "PASS" "AutoAcceptUntrustedCertificates=false" ;;
        *)     check_result "OPC UA Certs" "WARN" "OpcUa section not configured in appsettings.json" ;;
    esac
else
    check_result "OPC UA Certs" "WARN" "appsettings.json not found at expected path"
fi

# Check 6: Serial ports (Linux)
SERIAL_PORTS=$(ls /dev/ttyUSB* /dev/ttyACM* 2>/dev/null | tr '\n' ' ' || echo "")
if [ -n "$SERIAL_PORTS" ]; then
    check_result "Serial Ports" "PASS" "Found: $SERIAL_PORTS"
else
    check_result "Serial Ports" "WARN" "No ttyUSB/ttyACM ports detected -- check USB-serial adapters"
fi

echo ""
echo "=== Helix Pre-flight Report ==="
echo "PASS: $PASS_COUNT  WARN: $WARN_COUNT  FAIL: $FAIL_COUNT"

if [ "$FAIL_COUNT" -gt 0 ]; then
    echo "Result: FAIL ($FAIL_COUNT failure(s))"
    exit 2
elif [ "$WARN_COUNT" -gt 0 ]; then
    echo "Result: WARN ($WARN_COUNT warning(s)) -- review before starting experiment"
    exit 1
else
    echo "Result: PASS (all checks green)"
    exit 0
fi

