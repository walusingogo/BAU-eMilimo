[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$OutputDirectory = "release\eMilimo-v1-installer",
    [ValidateSet("Online", "Offline")]
    [string]$PackageMode = "Online",
    [string]$HostingBundleInstaller,
    [string]$PostgresInstaller,
    [string]$PgAdminInstaller,
    [string]$DockerDesktopInstaller,
    [string]$DockerImagesArchive,
    [string]$CodeSigningCertificateThumbprint,
    [string]$TimestampServer = "http://timestamp.digicert.com",
    [switch]$SkipFrontendBuild,
    [switch]$RequireFullyBundled
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$outputPath = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $OutputDirectory))
$releaseRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot "release"))
$stagingPath = Join-Path $releaseRoot ".emilimo-installer-staging"
$payloadPath = Join-Path $stagingPath "payload"
$wizardPath = Join-Path $stagingPath "wizard"

if (-not $outputPath.StartsWith($releaseRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Installer output must be a child of $releaseRoot"
}

if ($SkipFrontendBuild) {
    $existingWebBuild = Join-Path $repoRoot "frontend\web\dist\web\browser\index.html"
    $existingGivingBuild = Join-Path $repoRoot "frontend\giving\dist\giving\browser\index.html"
    if (-not (Test-Path -LiteralPath $existingWebBuild) -or -not (Test-Path -LiteralPath $existingGivingBuild)) {
        throw "-SkipFrontendBuild requires compiled web and giving applications. Missing: $existingWebBuild or $existingGivingBuild"
    }
    Write-Host "Using the existing verified eMilimo web build..." -ForegroundColor Cyan
}
else {
    Write-Host "Building eMilimo web application..." -ForegroundColor Cyan
    Push-Location (Join-Path $repoRoot "frontend\web")
    try {
        & npm.cmd run build
        if ($LASTEXITCODE -ne 0) { throw "The frontend build failed with exit code $LASTEXITCODE." }
    }
    finally {
        Pop-Location
    }
    Write-Host "Building eMilimo public Giving application..." -ForegroundColor Cyan
    Push-Location (Join-Path $repoRoot "frontend\giving")
    try {
        & npm.cmd run build
        if ($LASTEXITCODE -ne 0) { throw "The Giving frontend build failed with exit code $LASTEXITCODE." }
    }
    finally {
        Pop-Location
    }
}

if (Test-Path -LiteralPath $outputPath) {
    Write-Host "Removing the previous generated installer package..." -ForegroundColor DarkGray
    Remove-Item -LiteralPath $outputPath -Recurse -Force
}
if (Test-Path -LiteralPath $stagingPath) {
    Remove-Item -LiteralPath $stagingPath -Recurse -Force
}
New-Item -ItemType Directory -Path $outputPath, $payloadPath, $wizardPath -Force | Out-Null

if ($RequireFullyBundled -and $PackageMode -ne "Offline") {
    throw "-RequireFullyBundled can only be used with -PackageMode Offline."
}

$apiPayload = Join-Path $payloadPath "backend\eMilimo.API\bin\Release\net8.0"
$webPayload = Join-Path $payloadPath "frontend\web\dist\web\browser"
$givingPayload = Join-Path $payloadPath "frontend\giving\dist\giving\browser"
$deploymentPayload = Join-Path $payloadPath "deployment"
$prerequisitePayload = Join-Path $payloadPath "prerequisites"
New-Item -ItemType Directory -Path $apiPayload, $webPayload, $givingPayload, $deploymentPayload, $prerequisitePayload -Force | Out-Null

Write-Host "Publishing the eMilimo API payload..." -ForegroundColor Cyan
& dotnet publish (Join-Path $repoRoot "backend\eMilimo.API\eMilimo.API.csproj") `
    -c $Configuration --self-contained false --no-restore `
    -p:DebugType=None -p:DebugSymbols=false -p:PublishReadyToRun=false -p:PublishTrimmed=false `
    -o $apiPayload
if ($LASTEXITCODE -ne 0) { throw "The API publish failed with exit code $LASTEXITCODE." }

Write-Host "Copying the optimized web payload..." -ForegroundColor Cyan
Copy-Item -Path (Join-Path $repoRoot "frontend\web\dist\web\browser\*") -Destination $webPayload -Recurse -Force
Copy-Item -Path (Join-Path $repoRoot "frontend\giving\dist\giving\browser\*") -Destination $givingPayload -Recurse -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "frontend\web\nginx.production.conf") `
    -Destination (Join-Path $payloadPath "frontend\web\nginx.production.conf") -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "frontend\giving\nginx.production.conf") `
    -Destination (Join-Path $payloadPath "frontend\giving\nginx.production.conf") -Force

# The web payload is copied into the API wwwroot at install time. Remove the API
# copy only when it is byte-for-byte identical, so the archive carries one logo.
$apiLogo = Join-Path $apiPayload "wwwroot\images\ucz-logo.png"
$webLogo = Join-Path $webPayload "images\ucz-logo.png"
if ((Test-Path -LiteralPath $apiLogo) -and (Test-Path -LiteralPath $webLogo)) {
    $apiLogoHash = (Get-FileHash -LiteralPath $apiLogo -Algorithm SHA256).Hash
    $webLogoHash = (Get-FileHash -LiteralPath $webLogo -Algorithm SHA256).Hash
    if ($apiLogoHash -eq $webLogoHash) {
        Remove-Item -LiteralPath $apiLogo -Force
    }
}
Get-ChildItem -LiteralPath $payloadPath -Recurse -File -Filter *.pdb | Remove-Item -Force

foreach ($file in @(
    "docker-compose.installer.yml",
    "Dockerfile.api-installer",
    "Dockerfile.web-installer",
    "Dockerfile.giving-installer",
    "THIRD-PARTY-NOTICES.txt"
)) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $file) -Destination $deploymentPayload -Force
}

$offlinePrerequisites = @(
    @{ Name = "ASP.NET Core Hosting Bundle"; Source = $HostingBundleInstaller; Target = "dotnet-hosting-win.exe" },
    @{ Name = "PostgreSQL"; Source = $PostgresInstaller; Target = "postgresql-windows-x64.exe" },
    @{ Name = "pgAdmin"; Source = $PgAdminInstaller; Target = "pgadmin-windows-x64.exe" },
    @{ Name = "Docker Desktop"; Source = $DockerDesktopInstaller; Target = "Docker Desktop Installer.exe" },
    @{ Name = "Offline Docker images"; Source = $DockerImagesArchive; Target = "emilimo-images.tar" }
)

if ($PackageMode -eq "Offline") {
    foreach ($prerequisite in $offlinePrerequisites) {
        if ([string]::IsNullOrWhiteSpace($prerequisite.Source)) {
            if ($RequireFullyBundled) { throw "$($prerequisite.Name) was not supplied for the fully bundled package." }
            Write-Warning "$($prerequisite.Name) is not bundled. Its deployment option will require an existing installation."
            continue
        }
        $resolvedPrerequisite = (Resolve-Path -LiteralPath $prerequisite.Source).Path
        Copy-Item -LiteralPath $resolvedPrerequisite -Destination (Join-Path $prerequisitePayload $prerequisite.Target) -Force
    }
}
else {
    if ($offlinePrerequisites.Source | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) {
        Write-Warning "Local prerequisite installers are ignored in Online mode. Use -PackageMode Offline to embed them."
    }
    $onlinePrerequisites = @{
        schemaVersion = 1
        prerequisites = @(
            @{
                key = "dotnet-hosting"
                displayName = "ASP.NET Core 8.0.31 Hosting Bundle"
                fileName = "dotnet-hosting-8.0.31-win.exe"
                url = "https://builds.dotnet.microsoft.com/dotnet/aspnetcore/Runtime/8.0.31/dotnet-hosting-8.0.31-win.exe"
                sha256 = "A6A40D15773F25A0DDE8826C2A6D652FC104F54B574535F3E84AD716D00F98CE"
            },
            @{
                key = "postgresql"
                displayName = "PostgreSQL 16.15"
                fileName = "postgresql-16.15-1-windows-x64.exe"
                url = "https://get.enterprisedb.com/postgresql/postgresql-16.15-1-windows-x64.exe"
                sha256 = "DE926FEFAD00E313E212CD438C0F04BF033E200099AD56C012724EFCEBED79F2"
            },
            @{
                key = "pgadmin"
                displayName = "pgAdmin 4 v9.18"
                fileName = "pgadmin4-9.18-x64.exe"
                url = "https://ftp.postgresql.org/pub/pgadmin/pgadmin4/v9.18/windows/pgadmin4-9.18-x64.exe"
                sha256 = "CA82B9A07007308A70A9AF55192F5CA826C9CE839FE716CEA465E2CFC4529245"
            }
        )
    }
    $manifestJson = $onlinePrerequisites | ConvertTo-Json -Depth 5
    [System.IO.File]::WriteAllText(
        (Join-Path $prerequisitePayload "online-prerequisites.json"),
        $manifestJson,
        [System.Text.UTF8Encoding]::new($false))
}

Write-Host "Publishing the self-contained setup wizard..." -ForegroundColor Cyan
$wizardProject = Join-Path $PSScriptRoot "eMilimo.SetupWizard\eMilimo.SetupWizard.csproj"
$wizardNuGetConfig = Join-Path $PSScriptRoot "NuGet.Config"
& dotnet restore $wizardProject -r win-x64 --configfile $wizardNuGetConfig
if ($LASTEXITCODE -ne 0) { throw "The setup wizard runtime restore failed with exit code $LASTEXITCODE." }
& dotnet publish $wizardProject `
    -c $Configuration -r win-x64 --self-contained true --no-restore `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None -p:DebugSymbols=false -p:PublishReadyToRun=false -p:PublishTrimmed=false `
    -o $wizardPath
if ($LASTEXITCODE -ne 0) { throw "The setup wizard publish failed with exit code $LASTEXITCODE." }

$wizardExe = Join-Path $wizardPath "eMilimo-Setup.exe"
if (-not (Test-Path -LiteralPath $wizardExe)) { throw "Setup executable was not created: $wizardExe" }

Write-Host "Embedding the complete application payload into the standalone installer..." -ForegroundColor Cyan
$payloadArchive = Join-Path $stagingPath "payload.zip"
Compress-Archive -Path (Join-Path $payloadPath "*") -DestinationPath $payloadArchive -CompressionLevel Optimal -Force
# The compressed archive is now the canonical payload; release the expanded staging copy
# before constructing the large final executable to keep peak disk usage bounded.
Remove-Item -LiteralPath $payloadPath -Recurse -Force
$packageSuffix = if ($PackageMode -eq "Offline") { "-Offline" } else { "" }
$setupExe = Join-Path $outputPath "eMilimo-Setup-v1.0.1$packageSuffix.exe"
Move-Item -LiteralPath $wizardExe -Destination $setupExe -Force
$payloadInfo = Get-Item -LiteralPath $payloadArchive
$payloadHashHex = (Get-FileHash -LiteralPath $payloadArchive -Algorithm SHA256).Hash
$payloadHash = New-Object byte[] ($payloadHashHex.Length / 2)
for ($index = 0; $index -lt $payloadHash.Length; $index++) {
    $payloadHash[$index] = [Convert]::ToByte($payloadHashHex.Substring($index * 2, 2), 16)
}
$marker = [Text.Encoding]::ASCII.GetBytes("EMILIMO_PAYLOAD_V1")
$destinationStream = [IO.File]::Open($setupExe, [IO.FileMode]::Append, [IO.FileAccess]::Write, [IO.FileShare]::None)
$archiveStream = [IO.File]::OpenRead($payloadArchive)
try {
    $archiveStream.CopyTo($destinationStream)
    $payloadLengthBytes = [BitConverter]::GetBytes([long]$payloadInfo.Length)
    $destinationStream.Write($payloadLengthBytes, 0, $payloadLengthBytes.Length)
    $destinationStream.Write($payloadHash, 0, $payloadHash.Length)
    $destinationStream.Write($marker, 0, $marker.Length)
}
finally {
    $archiveStream.Dispose()
    $destinationStream.Dispose()
}
Remove-Item -LiteralPath $payloadArchive -Force

if (-not [string]::IsNullOrWhiteSpace($CodeSigningCertificateThumbprint)) {
    $thumbprint = $CodeSigningCertificateThumbprint.Replace(" ", "")
    $certificate = Get-ChildItem Cert:\CurrentUser\My, Cert:\LocalMachine\My |
        Where-Object { $_.Thumbprint -eq $thumbprint -and $_.HasPrivateKey } |
        Select-Object -First 1
    if ($null -eq $certificate) {
        throw "No code-signing certificate with a private key was found for thumbprint $thumbprint. Import it into CurrentUser or LocalMachine Personal certificates first."
    }

    Write-Host "Signing the standalone eMilimo installer as $($certificate.Subject)..." -ForegroundColor Cyan
    $signature = Set-AuthenticodeSignature -FilePath $setupExe -Certificate $certificate `
        -HashAlgorithm SHA256 -TimestampServer $TimestampServer
    if ($signature.Status -in @(
        [System.Management.Automation.SignatureStatus]::NotSigned,
        [System.Management.Automation.SignatureStatus]::HashMismatch,
        [System.Management.Automation.SignatureStatus]::NotSupported,
        [System.Management.Automation.SignatureStatus]::Incompatible)) {
        throw "Authenticode signing failed: $($signature.StatusMessage)"
    }
    if ($null -eq $signature.SignerCertificate -or $signature.SignerCertificate.Thumbprint -ne $certificate.Thumbprint) {
        throw "Authenticode signing did not use the requested certificate."
    }
    if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
        Write-Warning "The executable is signed, but this build computer does not trust the signing chain: $($signature.StatusMessage)"
    }
}
else {
    Write-Warning "The standalone installer is not Authenticode-signed. Windows will not show a verified publisher until a trusted code-signing certificate is supplied."
}

$payloadCount = (Get-ChildItem -LiteralPath $outputPath -File).Count
if ($payloadCount -ne 1) { throw "The installer output must contain exactly one file, but contains $payloadCount." }
Remove-Item -LiteralPath $stagingPath -Recurse -Force
$sizeMb = [math]::Round((Get-ChildItem -LiteralPath $outputPath -File -Recurse | Measure-Object Length -Sum).Sum / 1MB, 1)
Write-Host ""
Write-Host "Standalone installer ready: $setupExe" -ForegroundColor Green
Write-Host "Package mode: $PackageMode" -ForegroundColor Green
Write-Host "Single-file size: $sizeMb MB" -ForegroundColor Green
