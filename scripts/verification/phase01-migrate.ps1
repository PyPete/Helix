[CmdletBinding()]
param(
    [ValidateSet('CheckAndApply')]
    [string]$Mode = 'CheckAndApply',
    [Alias('Container')]
    [string]$ContainerName = 'helix-postgres',
    [string]$Database = 'helix_orchestration',
    [string]$MigrationUser = $env:HELIX_MIGRATION_USER,
    [string]$RuntimeUser = $env:HELIX_RUNTIME_USER,
    [switch]$AllowDisposableTarget,
    [string]$FailAfterSqlMigration,
    [string]$MigrationDirectory = 'postgres/migrations'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Push-Location $repositoryRoot
try {
    if ([string]::IsNullOrWhiteSpace($MigrationUser) -or [string]::IsNullOrWhiteSpace($RuntimeUser)) {
        throw '[BLOCKING] Set distinct migration and runtime identities; schema acceptance cannot use an implicit account.'
    }
    if ($MigrationUser -eq $RuntimeUser) {
        throw '[BLOCKING] Migration and runtime database identities must be separate.'
    }

    docker container inspect $ContainerName *> $null
    if ($LASTEXITCODE -ne 0) { throw '[BLOCKING] Explicit PostgreSQL container is unavailable.' }
    if ($ContainerName -ne 'helix-postgres') {
        if (-not $AllowDisposableTarget) { throw '[BLOCKING] Non-live targets require -AllowDisposableTarget.' }
        $metadata = docker container inspect $ContainerName | ConvertFrom-Json
        $owned = $metadata[0].Config.Labels.'org.testcontainers'
        if ($LASTEXITCODE -ne 0 -or $owned -ne 'true') { throw '[BLOCKING] Disposable verification target must be Testcontainers-owned.' }
    }
    elseif ($MigrationDirectory -ne 'postgres/migrations') {
        throw '[BLOCKING] Alternate migration directories are only permitted for disposable verification targets.'
    }

    function Invoke-Database([string]$Sql) {
        $Sql | docker exec -i $ContainerName psql -v ON_ERROR_STOP=1 -U $MigrationUser -d $Database -At
        if ($LASTEXITCODE -ne 0) { throw '[BLOCKING] PostgreSQL command failed.' }
    }

    $migrations = Get-ChildItem (Resolve-Path $MigrationDirectory) -File -Filter '*.sql' | Sort-Object Name
    if ($migrations.Count -eq 0) { throw '[BLOCKING] No versioned migrations were found.' }
    foreach ($migration in $migrations) {
        $checksum = (Get-FileHash $migration.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        $migrationId = $migration.Name.Replace("'", "''")
        $sql = Get-Content $migration.FullName -Raw
        $transaction = @"
BEGIN;
SELECT pg_advisory_xact_lock(hashtext('helix_schema_migrations'));
CREATE TABLE IF NOT EXISTS helix_schema_migrations (migration_id text PRIMARY KEY, checksum text NOT NULL, applied_at timestamptz NOT NULL DEFAULT now());
DO `$verify$`
DECLARE existing_checksum text;
BEGIN
    SELECT checksum INTO existing_checksum FROM helix_schema_migrations WHERE migration_id = '$migrationId';
    IF existing_checksum IS NULL THEN
        EXECUTE `$migration$`
$sql
`$migration$`;
        INSERT INTO helix_schema_migrations (migration_id, checksum) VALUES ('$migrationId', '$checksum');
    ELSIF existing_checksum <> '$checksum' THEN
        RAISE EXCEPTION 'Applied migration checksum differs: %', '$migrationId';
    END IF;
END `$verify$`;
COMMIT;
"@
        if ($FailAfterSqlMigration -eq $migration.Name) {
            $transaction = $transaction.Replace("INSERT INTO helix_schema_migrations", "RAISE EXCEPTION 'Injected crash before receipt';`n        INSERT INTO helix_schema_migrations")
        }
        Invoke-Database $transaction
        Write-Host "Migration verified: $($migration.Name)"
    }

    Invoke-Database "SET ROLE $RuntimeUser; SELECT has_schema_privilege(current_user, 'public', 'CREATE'); RESET ROLE;" | Out-Null
    Write-Host '[PASS] Atomic migration receipts and explicit runtime role boundary verified.'
}
finally { Pop-Location }
