[CmdletBinding()]
param(
    [string]$BackupPath = "$env:USERPROFILE\Downloads\BakimYonetimiDb_yedek.bak",
    [string]$Server = "(localdb)\MSSQLLocalDB",
    [string]$DatabaseName = "BakimYonetimiDb",
    [switch]$Replace
)

$ErrorActionPreference = "Stop"

if ($DatabaseName -notmatch '^[A-Za-z0-9_]+$') {
    throw "Veritabanı adı yalnızca harf, rakam ve alt çizgi içerebilir."
}

$resolvedBackup = (Resolve-Path -LiteralPath $BackupPath).Path
$sqlcmd = Get-Command sqlcmd -ErrorAction Stop
$escapedBackup = $resolvedBackup.Replace("'", "''")

$dataRoot = [System.IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA "OnleyiciBakim\Data"))
$allowedRoot = [System.IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA "OnleyiciBakim"))
if (-not $dataRoot.StartsWith($allowedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Veritabanı hedef dizini güvenli kök altında değil."
}
New-Item -ItemType Directory -Path $dataRoot -Force | Out-Null

$exists = (& $sqlcmd.Source -S $Server -E -h -1 -W -Q `
    "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.databases WHERE name=N'$DatabaseName'" | Out-String).Trim()
if ($LASTEXITCODE -ne 0) {
    throw "SQL Server örneğine bağlanılamadı: $Server"
}

if ($exists -ne "0" -and -not $Replace) {
    throw "'$DatabaseName' zaten var. Üzerine yazmak için bilinçli olarak -Replace kullanın."
}

if ($exists -ne "0") {
    & $sqlcmd.Source -S $Server -E -b -Q `
        "ALTER DATABASE [$DatabaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$DatabaseName];"
    if ($LASTEXITCODE -ne 0) {
        throw "Mevcut veritabanı kaldırılamadı."
    }
}

$mdf = (Join-Path $dataRoot "$DatabaseName.mdf").Replace("'", "''")
$ldf = (Join-Path $dataRoot "${DatabaseName}_log.ldf").Replace("'", "''")

# Gönderilen dosyada iki tam yedek seti vardır; FILE=2 en güncel settir.
& $sqlcmd.Source -S $Server -E -b -Q `
    "RESTORE DATABASE [$DatabaseName] FROM DISK=N'$escapedBackup' WITH FILE=2, MOVE N'BakimYonetimiDb' TO N'$mdf', MOVE N'BakimYonetimiDb_log' TO N'$ldf', RECOVERY, STATS=10;"
if ($LASTEXITCODE -ne 0) {
    throw "Veritabanı geri yüklenemedi."
}

& $sqlcmd.Source -S $Server -E -b -d $DatabaseName -W -Q `
    "SET NOCOUNT ON; SELECT DB_NAME() AS DatabaseName, (SELECT COUNT(*) FROM dbo.Makineler) AS Makineler, (SELECT COUNT(*) FROM dbo.ArizaKartlari) AS Arizalar, (SELECT COUNT(*) FROM dbo.TelemetriGunluk) AS Telemetri;"
if ($LASTEXITCODE -ne 0) {
    throw "Geri yükleme sonrası doğrulama başarısız oldu."
}
