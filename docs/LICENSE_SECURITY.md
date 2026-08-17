# Licence signing and key custody

Version 2 licences use RSA-3072 with RSA-PSS/SHA-256. The standalone `LicenseGen` system signs a canonical payload with BAU's private key. UCP contains only the corresponding public key, so a customer can inspect the verifier without gaining the ability to issue or alter a licence.

## Key custody

- Never deploy, email, commit or place the private PEM on a UCP server.
- Keep the production private key in an offline encrypted drive, HSM or managed key vault accessible only to authorised BAU staff.
- Back up the private key securely. Losing it prevents issuing compatible licences; leaking it requires immediate key rotation and a UCP update.
- The repository and Docker build exclude `.license-secrets` and private PEM/PFX files.
- Generate production keys on the dedicated licence-issuing machine, not on a customer installation.

## Commands

Create a keypair once:

```powershell
Configure `Issuer__PrivateKeyPath` in the standalone LicenseGen system with the protected private PEM location and configure `Issuer__ApiKey` through environment variables or a secret store.
```

Issue a term licence:

```powershell
POST the customer, deployment identity and expiry/perpetual selection to the authenticated standalone `api/licenses/issue` endpoint.
```

The standalone system returns the signed XML file consumed by UCP.

## Migration

`License:AllowLegacyLicenses` temporarily permits existing checksum licences and reports them as `LEGACY_ACTIVE_REISSUE_REQUIRED`. Issue and install a version 2 licence for every deployment, then set this option to `false`. Leaving legacy support enabled preserves the old forgery route.

No client-side mechanism is mathematically impossible to bypass by an attacker who can replace the server binary. Production deployments should additionally use restricted server administration, signed release artifacts, image digests, file-integrity monitoring and secrets stored outside the application directory.
