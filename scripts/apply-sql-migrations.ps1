param(
    [string]$ContainerName = "avcndb-db",
    [string]$Database = "MEDICDB",
    [string]$User = "medwin",
    [string]$Password = "0101",
    [string]$MigrationsPath = "database/migrations",
    [string]$BackupsPath = "database/backups",
    [switch]$NoBackup,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Invoke-MariaDbQuery {
    param(
        [Parameter(Mandatory = $true)][string]$Sql
    )

    $output = & docker exec $ContainerName mariadb --batch --skip-column-names "-u$User" "-p$Password" "-D" $Database -e $Sql 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "MariaDB query failed.`n$output"
    }

    return $output
}

function Invoke-MariaDbScriptFile {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath
    )

    $sqlContent = Get-Content -Raw -LiteralPath $FilePath
    $output = $sqlContent | & docker exec -i $ContainerName mariadb "-u$User" "-p$Password" "-D" $Database 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Migration failed for '$FilePath'.`n$output"
    }
}

function New-DatabaseBackup {
    param(
        [Parameter(Mandatory = $true)][string]$OutputFile
    )

    $dump = & docker exec $ContainerName mariadb-dump "-u$User" "-p$Password" $Database 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Database backup failed.`n$dump"
    }

    $dump | Set-Content -LiteralPath $OutputFile -Encoding UTF8
}

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$migrationsDir = if ([System.IO.Path]::IsPathRooted($MigrationsPath)) { $MigrationsPath } else { Join-Path $projectRoot $MigrationsPath }
$backupsDir = if ([System.IO.Path]::IsPathRooted($BackupsPath)) { $BackupsPath } else { Join-Path $projectRoot $BackupsPath }

if (-not (Test-Path -LiteralPath $migrationsDir)) {
    throw "Migrations path not found: $migrationsDir"
}

$null = Invoke-MariaDbQuery -Sql @"
CREATE TABLE IF NOT EXISTS schema_sql_migrations (
    version VARCHAR(64) NOT NULL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    checksum CHAR(64) NOT NULL,
    applied_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6)
);
"@

$appliedRows = @(Invoke-MariaDbQuery -Sql "SELECT version, checksum FROM schema_sql_migrations ORDER BY version;")
$applied = @{}

foreach ($line in $appliedRows) {
    if ([string]::IsNullOrWhiteSpace($line)) {
        continue
    }

    $parts = $line -split "`t"
    if ($parts.Count -ge 2) {
        $applied[$parts[0]] = $parts[1].ToLowerInvariant()
    }
}

$migrationFiles = @(Get-ChildItem -LiteralPath $migrationsDir -File |
    Where-Object { $_.Name -match '^V\d+__.+\.sql$' } |
    Sort-Object Name)

if ($migrationFiles.Count -eq 0) {
    Write-Host "No migration files found in '$migrationsDir'."
    exit 0
}

$pending = New-Object System.Collections.Generic.List[object]

foreach ($file in $migrationFiles) {
    if ($file.BaseName -notmatch '^(V\d+)__(.+)$') {
        continue
    }

    $version = $Matches[1]
    $name = $Matches[2]
    $checksum = (Get-FileHash -Algorithm SHA256 -LiteralPath $file.FullName).Hash.ToLowerInvariant()

    if ($applied.ContainsKey($version)) {
        if ($applied[$version] -ne $checksum) {
            throw "Checksum mismatch for already applied migration $version ($($file.Name)). File was modified after execution."
        }

        Write-Host "Skip $version ($name): already applied."
        continue
    }

    $pending.Add([PSCustomObject]@{
        Version = $version
        Name = $name
        FilePath = $file.FullName
        Checksum = $checksum
    }) | Out-Null
}

if ($pending.Count -eq 0) {
    Write-Host "No pending migrations."
    exit 0
}

Write-Host "Pending migrations:"
foreach ($m in $pending) {
    Write-Host " - $($m.Version) $($m.Name)"
}

if ($DryRun) {
    Write-Host "DryRun enabled: no changes applied."
    exit 0
}

if (-not $NoBackup) {
    New-Item -ItemType Directory -Force -Path $backupsDir | Out-Null
    $backupName = "{0}-{1}-before-sql-migrations.sql" -f $Database, (Get-Date -Format "yyyyMMdd-HHmmss")
    $backupFile = Join-Path $backupsDir $backupName
    Write-Host "Creating backup: $backupFile"
    New-DatabaseBackup -OutputFile $backupFile
}

foreach ($m in $pending) {
    Write-Host "Applying $($m.Version) ($($m.Name))..."
    Invoke-MariaDbScriptFile -FilePath $m.FilePath

    $safeName = $m.Name -replace "'", "''"
    $insertSql = "INSERT INTO schema_sql_migrations(version, name, checksum, applied_at) VALUES ('$($m.Version)', '$safeName', '$($m.Checksum)', NOW(6));"
    $null = Invoke-MariaDbQuery -Sql $insertSql
}

Write-Host "Migrations applied successfully."
