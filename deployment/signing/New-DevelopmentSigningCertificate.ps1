[CmdletBinding()]
param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot "..\..\.license-secrets\signing"),
    [ValidateRange(1, 5)]
    [int]$ValidYears = 3
)

$ErrorActionPreference = "Stop"
$output = [System.IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $output -Force | Out-Null

$alphabet = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#$%_-+="
$random = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$bytes = New-Object byte[] 48
$random.GetBytes($bytes)
$passwordText = -join ($bytes | ForEach-Object { $alphabet[$_ % $alphabet.Length] })
$password = ConvertTo-SecureString $passwordText -AsPlainText -Force

$certificate = New-SelfSignedCertificate `
    -Type CodeSigningCert `
    -Subject "CN=BAU Solutions Limited, O=BAU Solutions Limited, C=ZM" `
    -FriendlyName "BAU Solutions eMilimo Internal Code Signing" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyAlgorithm RSA `
    -KeyLength 3072 `
    -HashAlgorithm SHA256 `
    -KeyExportPolicy Exportable `
    -NotAfter (Get-Date).AddYears($ValidYears)

$pfxPath = Join-Path $output "BAU-Solutions-eMilimo-CodeSigning.pfx"
$cerPath = Join-Path $output "BAU-Solutions-eMilimo-CodeSigning.cer"
$passwordPath = Join-Path $output "BAU-Solutions-eMilimo-CodeSigning.password.txt"
Export-PfxCertificate -Cert $certificate -FilePath $pfxPath -Password $password | Out-Null
Export-Certificate -Cert $certificate -FilePath $cerPath | Out-Null
Set-Content -LiteralPath $passwordPath -Value $passwordText -Encoding utf8NoBOM

[pscustomobject]@{
    Subject = $certificate.Subject
    Thumbprint = $certificate.Thumbprint
    Expires = $certificate.NotAfter
    Pfx = $pfxPath
    PublicCertificate = $cerPath
    PasswordFile = $passwordPath
}
