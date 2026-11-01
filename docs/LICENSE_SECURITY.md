# Licence signing and key custody

Version 2 licences use RSA-3072 with RSA-PSS/SHA-256. The standalone `LicenseGen` system signs a canonical payload with BAU's private key. eMilimo contains only the corresponding public key, so a customer can inspect the verifier without gaining the ability to issue or alter a licence.

## Key custody

- Never deploy, email, commit or place the private PEM on an eMilimo server.
- Keep the production private key in an offline encrypted drive, HSM or managed key vault accessible only to authorised BAU staff.
- Back up the private key securely. Losing it prevents issuing compatible licences; leaking it requires immediate key rotation and an eMilimo update.
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

The standalone system returns the signed XML file consumed by eMilimo.

## Migration

Only signed version 2 licences are accepted. Unsigned checksum licence formats are not part of the eMilimo v1 contract.

No client-side mechanism is mathematically impossible to bypass by an attacker who can replace the server binary. Production deployments should additionally use restricted server administration, signed release artifacts, image digests, file-integrity monitoring and secrets stored outside the application directory.

## Signed product-key rotation

eMilimo does not accept standalone public-key uploads. A SuperAdmin may upload only a versioned JSON rotation package produced by SecureLicenseGen. eMilimo authenticates that package against three embedded BAU recovery public keys and requires signatures from at least two distinct recovery private keys.

Rotation packages are product-bound, expire after 48 hours, contain the new RSA-3072 public-key fingerprint, and carry a strictly increasing sequence number. Reused packages, reused key identifiers, expired packages, fingerprint mismatches and sequence rollback are rejected and audited in `license_key_rotation_audits`. Accepted product public keys are stored in `license_trusted_keys`; private keys never enter eMilimo.

Use `RETAIN` when the previous private key was safely lost or during a planned transition, allowing existing licences to continue validating. Use `REVOKE` when compromise is suspected, which invalidates licences signed by that key.

Recovery private keys are not application data. Keep encrypted offline backups under separate custody. Do not copy them into either source repository or any eMilimo deployment.
