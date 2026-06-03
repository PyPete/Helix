---
id: production-hardening
title: "Production Hardening"
sidebar_position: 6
---

:::warning
The default development credentials in `docker-compose.yml` and `appsettings.json` files **must be rotated** before exposing any Helix service on a shared or internet-accessible network.
:::

This guide covers the security steps required before deploying Helix to a production lab environment.

## Secrets Checklist

| Secret | Default dev value | Action required |
|--------|-------------------|-----------------|
| PostgreSQL password | `helix_dev_password` | Rotate before production |
| MinIO root credentials | `minioadmin` / `minioadmin` | Rotate before production |
| MinIO access key | `minioadmin` / `minioadmin` | Create dedicated key, rotate root |
| MQTT listener | Anonymous (no auth) | Enable TLS + password auth |
| OPC UA `AutoAcceptUntrustedCertificates` | `true` (dev) | Set to `false` |

## TLS for MQTT (Mosquitto)

Enable TLS on the Mosquitto broker to encrypt lab device communications.

### 1. Generate CA and Server Certificates

```bash
# Create CA key and certificate
openssl genrsa -out ca.key 4096
openssl req -new -x509 -days 3650 -key ca.key -out ca.crt -subj "/CN=HelixCA"

# Create server key and certificate
openssl genrsa -out server.key 2048
openssl req -new -key server.key -out server.csr -subj "/CN=helix-mosquitto"
openssl x509 -req -days 3650 -in server.csr -CA ca.crt -CAkey ca.key -CAcreateserial -out server.crt
```

Place the files in `mosquitto/certs/`.

### 2. Update mosquitto.conf

```
listener 8883
cafile /mosquitto/config/certs/ca.crt
certfile /mosquitto/config/certs/server.crt
keyfile /mosquitto/config/certs/server.key
tls_version tlsv1.3
require_certificate false
```

Update `docker-compose.yml` to mount `./mosquitto/certs:/mosquitto/config/certs:ro` and expose port `8883`.

### 3. Update Service Configuration

In `appsettings.json` (Agent and Recorder), add:
```json
"Mqtt": {
  "BrokerHost": "localhost",
  "BrokerPort": 8883,
  "UseTls": true,
  "CaFile": "/opt/helix/certs/ca.crt"
}
```

## MinIO Access Key Rotation

1. Open the MinIO Console at `http://localhost:9002` and log in with root credentials.
2. Navigate to **Access Keys → Create access key**.
3. Generate a new key pair and record it securely.
4. Update `Minio__AccessKey` and `Minio__SecretKey` in all service configurations (Recorder `appsettings.json`, `docker-compose.yml` environment, systemd `Environment=` lines or `EnvironmentFile`).
5. Rotate the root credentials: update `MINIO_ROOT_USER` and `MINIO_ROOT_PASSWORD` in `docker-compose.yml`.
6. Restart affected services: `docker compose restart helix-recorder`.

:::caution
The MinIO console port (9002) must **not** be exposed on public network interfaces with default credentials.
:::

## PostgreSQL Password Rotation

1. Connect to the running container:
   ```bash
   docker exec -it helix-postgres psql -U helix helix_orchestration
   ```
2. Rotate the password:
   ```sql
   ALTER USER helix WITH PASSWORD 'new_strong_password';
   ```
3. Update the connection string in **all** locations:
   - `Helix.Orchestration.Agent/appsettings.json` → `ConnectionStrings__HelixOrchestration`
   - `docker-compose.yml` → `POSTGRES_PASSWORD` environment variable
   - Systemd unit: `ConnectionStrings__HelixOrchestration` environment variable (or `EnvironmentFile`)
   - Windows service NSSM AppEnvironmentExtra
4. Restart the Agent:
   ```bash
   docker compose restart helix-agent
   # or: systemctl restart helix-agent
   ```

## OPC UA Certificate Validation

In development, `AutoAcceptUntrustedCertificates: true` allows connecting to OPC UA devices without pre-configuring their certificates. **This must be disabled in production.**

1. Set `AutoAcceptUntrustedCertificates: false` in `Helix.Orchestration.Agent/appsettings.json` under the `OpcUa` section.
2. Export the OPC UA device's certificate from its management interface.
3. Import the certificate into the Agent's trusted store (path configured in `OpcUa__CertificateStore`).
4. Run the pre-flight script -- it will **WARN** if `AutoAcceptUntrustedCertificates` is `true`. Treat this as a blocker before connecting to production OPC UA devices:
   ```powershell
   .\deploy\preflight\preflight.ps1
   ```

## Systemd Secrets (Linux)

Instead of storing credentials in `Environment=` lines within the unit file (visible in `systemctl show helix-agent`), use an `EnvironmentFile`:

1. Create `/etc/helix/agent-secrets` (mode `0600`, owned by `helix`):
   ```
   ConnectionStrings__HelixOrchestration=Host=localhost;Port=5432;Database=helix_orchestration;Username=helix;Password=new_strong_password
   ```
2. In `helix-agent.service`, replace the inline `Environment=ConnectionStrings__...` line with:
   ```ini
   EnvironmentFile=/etc/helix/agent-secrets
   ```
3. Reload and restart: `sudo systemctl daemon-reload && sudo systemctl restart helix-agent`
