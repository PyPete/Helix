[CmdletBinding()]
param(
    [ValidateSet('CheckAndApply')]
    [string]$Mode = 'CheckAndApply',
    [string]$Container = 'helix-postgres',
    [string]$Database = 'helix_orchestration',
    [string]$MigrationUser = $env:HELIX_MIGRATION_USER,
    [string]$RuntimeUser = $env:HELIX_RUNTIME_USER
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Push-Location $repositoryRoot
try {
    if ([string]::IsNullOrWhiteSpace($MigrationUser) -or [string]::IsNullOrWhiteSpace($RuntimeUser)) {
        throw '[BLOCKING] Set HELIX_MIGRATION_USER and HELIX_RUNTIME_USER; schema acceptance cannot use an implicit account.'
    }
    if ($MigrationUser -eq $RuntimeUser) {
        throw '[BLOCKING] Migration and runtime database identities must be separate.'
    }
    docker container inspect $Container *> $null
    if ($LASTEXITCODE -ne 0) { throw '[BLOCKING] Real PostgreSQL container is unavailable.' }

    function Invoke-Database([string]$User, [string]$Sql) {
        $Sql | docker exec -i $Container psql -v ON_ERROR_STOP=1 -U $User -d $Database -At
        if ($LASTEXITCODE -ne 0) { throw '[BLOCKING] PostgreSQL command failed.' }
    }

    Invoke-Database $MigrationUser 'CREATE TABLE IF NOT EXISTS helix_schema_migrations (migration_id text PRIMARY KEY, checksum text NOT NULL, applied_at timestamptz NOT NULL DEFAULT now());' | Out-Null
    $migrations = Get-ChildItem 'postgres/migrations' -File -Filter '*.sql' | Sort-Object Name
    if ($migrations.Count -eq 0) { throw '[BLOCKING] No versioned migrations were found.' }
    foreach ($migration in $migrations) {
        $checksum = (Get-FileHash $migration.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        $existing = Invoke-Database $MigrationUser "SELECT checksum FROM helix_schema_migrations WHERE migration_id = '$($migration.Name.Replace("'", "''"))';"
        if ($existing) {
            if ($existing.Trim() -ne $checksum) { throw "[BLOCKING] Applied migration checksum differs: $($migration.Name)." }
        } else {
            Get-Content $migration.FullName -Raw | docker exec -i $Container psql -v ON_ERROR_STOP=1 -U $MigrationUser -d $Database | Out-Null
            if ($LASTEXITCODE -ne 0) { throw "[BLOCKING] Migration failed: $($migration.Name)." }
            Invoke-Database $MigrationUser "INSERT INTO helix_schema_migrations (migration_id, checksum) VALUES ('$($migration.Name.Replace("'", "''"))', '$checksum');" | Out-Null
        }
        Write-Host "Migration verified: $($migration.Name)"
    }

    $requiredTables = @('command_streams', 'command_intents', 'command_evidence')
    foreach ($table in $requiredTables) {
        $grant = Invoke-Database $RuntimeUser "SELECT has_table_privilege(current_user, 'public.$table', 'SELECT,INSERT');"
        if ($grant.Trim() -ne 't') { throw "[BLOCKING] Runtime identity lacks required fact-table grant: $table." }
    }
    Write-Host "[PASS] Applied migration IDs and runtime fact-table grants verified."
}
finally { Pop-Location }
