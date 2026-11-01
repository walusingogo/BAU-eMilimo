[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$PfxPath,
    [Parameter(Mandatory)] [string]$PasswordFile,
    [switch]$TrustForCurrentUser
)

$ErrorActionPreference = "Stop"
$password = ConvertTo-SecureString (Get-Content -Raw -LiteralPath $PasswordFile).Trim() -AsPlainText -Force
$certificate = Import-PfxCertificate -FilePath $PfxPath -Password $password -CertStoreLocation "Cert:\CurrentUser\My" -Exportable

if ($TrustForCurrentUser)
{
    $temporaryCer = Join-Path ([System.IO.Path]::GetTempPath()) "bau-emilimo-$($certificate.Thumbprint).cer"
    try
    {
        Export-Certificate -Cert $certificate -FilePath $temporaryCer -Force | Out-Null
        Import-Certificate -FilePath $temporaryCer -CertStoreLocation "Cert:\CurrentUser\Root" | Out-Null
        Import-Certificate -FilePath $temporaryCer -CertStoreLocation "Cert:\CurrentUser\TrustedPublisher" | Out-Null
    }
    finally
    {
        Remove-Item -LiteralPath $temporaryCer -Force -ErrorAction SilentlyContinue
    }
}

$certificate | Select-Object Subject, Thumbprint, NotAfter, HasPrivateKey
