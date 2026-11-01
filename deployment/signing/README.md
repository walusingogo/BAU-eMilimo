# eMilimo internal signing certificate

`New-DevelopmentSigningCertificate.ps1` creates a password-protected, exportable
BAU Solutions code-signing PFX and its public CER under the ignored
`.license-secrets/signing` directory. The private key and password must never be
committed or included in the customer installer.

Import it for signing and, on internal test machines only, trust its public key:

```powershell
.\deployment\signing\Import-DevelopmentSigningCertificate.ps1 `
  -PfxPath .\.license-secrets\signing\BAU-Solutions-eMilimo-CodeSigning.pfx `
  -PasswordFile .\.license-secrets\signing\BAU-Solutions-eMilimo-CodeSigning.password.txt `
  -TrustForCurrentUser
```

Build and sign with the returned thumbprint:

```powershell
.\deployment\build-installer.ps1 -CodeSigningCertificateThumbprint THUMBPRINT
```

This self-signed certificate is suitable only for controlled internal deployments
where administrators distribute the CER through a trusted channel. It cannot create
a publicly verified Windows publisher on a machine that does not already trust it.
Production customer releases require an organisation-validated Authenticode
certificate from a publicly trusted provider.
