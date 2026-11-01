# eMilimo v1.0.1 cleanup and installer verification

## Cleanup

The release cleanup removed generated build output, dependency folders, the obsolete release package, redundant session handover/status documents, and the archived backup migration bundle. Source code, current migrations, the v1 consolidation implementation record, financial evidence, and the pre-consolidation database backup were retained.

Approximately 2.99 GB of generated or obsolete local files was removed before the clean build. Dependencies and build output were regenerated only where required for verification and packaging.

## Deployment correction

Digital Giving remains an independent application. It is built with base path `/`, listens on port `9200` by default, and is not mounted under the administration application's `/give` path.

The installer supports optional production URLs for:

- Administration.
- API and payment callbacks.
- Public Giving and receipt links.

Docker uses separate administration and Giving Nginx services. IIS and standalone deployments use separate listeners while sharing the same backend and financial database.

## Verification

- Backend Release build: passed with zero warnings and zero errors.
- Backend automated tests: 116 passed, 0 failed, 0 skipped.
- Administration Angular production build: passed; existing bundle-budget/CommonJS warnings remain non-blocking.
- Giving Angular production build: passed.
- Windows setup wizard Release build (`win-x64`): passed with zero warnings and zero errors.
- Giving compiled base path: `/`.

## Installer artifact

- File: `release/eMilimo-v1.0.1-installer/eMilimo-Setup-v1.0.1.exe`
- Product version: `1.0.1`
- Publisher metadata: `BAU Solutions Limited`
- Size: 84,452,436 bytes (80.54 MiB)
- SHA-256: `F0B8509E0DC22741AF8D35F28B3C9E896B86D19DDBDEE57321095F76AD7CF606`
- Package mode: online single-file installer.

The executable is not Authenticode-signed because no trusted BAU Solutions code-signing certificate was available in the build environment. Windows will not display a verified publisher until the final installer is signed with a trusted code-signing certificate.
