# eMilimo installer size optimization report

Date: 2026-10-18  
Release: 1.0.1  
Target: Windows x64

## Executive result

The distributable online installer was reduced from **850.14 MB** to
**79.4 MB**, a reduction of approximately **770.7 MB (90.7%)**. The result remains one
executable containing the complete eMilimo API, web application, setup
wizard, deployment definitions, legal resources, and a pinned online
prerequisite manifest.

The application was not trimmed. No runtime-used library was removed merely
because its purpose was unclear.

## Measured baseline

The original signed executable was measured before changes. Its embedded ZIP
was inspected directly, without first changing or rebuilding the package.

| Component | Files | Raw size | Compressed size | Finding |
|---|---:|---:|---:|---|
| Setup wizard host | 1 | 158.70 MB | n/a | Self-contained single-file runtime was not compressed |
| Prerequisites | 3 | 684.08 MB | 680.48 MB | PostgreSQL, pgAdmin, and Hosting Bundle were already compressed executables |
| API | 85 | 18.60 MB | 6.86 MB | Included symbols, four required cross-platform runtime assets, and a duplicate logo |
| Web | 26 | 6.32 MB | 4.07 MB | Production output had no source maps; two oversized raster assets dominated |
| Deployment files | 4 | 0.01 MB | 0.01 MB | Already minimal |
| Embedded ZIP total | 118 | 709.01 MB | 691.44 MB | Prerequisite executables limited further ZIP gains |
| **Signed installer** | **1** | **850.14 MB** | n/a | Baseline SHA-256: `8787DACFAE05F82057B6BB6B1A5D8D59853C58E8E8B0E8423AEE9BC0D10A86F5` |

Largest baseline files:

1. PostgreSQL 16.15 installer: 347.92 MB
2. pgAdmin 4 v9.18 installer: 229.44 MB
3. .NET 8.0.31 Hosting Bundle: 106.71 MB
4. Login photograph: 2.39 MB
5. Entity Framework Core assembly: 2.42 MB raw

The three prerequisite installers represented 80.5% of the complete file and
compressed by only 3.60 MB in total. Switching ZIP compression levels could
therefore never deliver a material reduction.

## Implemented packaging architecture

### Online installer (default)

`build-installer.ps1 -PackageMode Online` creates
`eMilimo-Setup-v1.0.1.exe`.

- Prerequisite executables are not embedded.
- Setup downloads the .NET Hosting Bundle, PostgreSQL, and pgAdmin only when
  the chosen deployment needs them.
- Each source is HTTPS-only, version-pinned, and protected by an embedded
  SHA-256 digest. A mismatched download is deleted and never executed.
- Verified downloads are cached locally for repair/retry.
- Docker Desktop remains an administrator-managed prerequisite. Docker pulls
  the pinned application images only when Docker deployment is selected.

### Offline installer

`build-installer.ps1 -PackageMode Offline` creates
`eMilimo-Setup-v1.0.1-Offline.exe` and embeds only the installers explicitly
provided to the build. `-RequireFullyBundled` fails the build if any declared
offline prerequisite or Docker image archive is missing.

With the same three baseline prerequisite executables, the optimized offline
package is projected at approximately **759 MB**. That is about 91 MB smaller
than the baseline, but cannot reasonably approach 150 MB because those three
unaltered third-party installers alone occupy 680.48 MB after ZIP compression.
Additional Docker artifacts increase the offline package by their compressed
sizes.

## Safe optimizations applied

- Enabled .NET single-file compression for the self-contained WinForms setup
  host: **158.70 MB → 71.61 MB**.
- Kept setup self-contained so it can start on a clean Windows machine.
- Kept the API framework-dependent. Its four small cross-platform runtime assets
  are intentionally retained because the same publish output runs in Windows
  IIS/standalone deployments and the Linux Docker image.
- Disabled symbols in distributable publishes and excluded residual PDB files.
- Kept `PublishTrimmed=false` and `PublishReadyToRun=false`. WinForms, Npgsql,
  IIS administration, JSON, and deployment code are reflection-sensitive;
  trimming would trade a small saving for unacceptable runtime risk, while
  ReadyToRun would increase size.
- Removed the API copy of `ucz-logo.png` only after proving it was byte-for-byte
  identical to the web copy. Setup copies the web payload into API `wwwroot`.
- Converted the photographic login background from a 2.39 MB PNG to a visually
  checked 361 KB JPEG.
- Reduced the 1600×1600 transparent logo to 512×512 (795 KB → 77 KB), still far
  above its normal 40–64 px display size.
- Retained Angular production hashing/minification/lazy chunks. The production
  output contains no source maps, `node_modules`, test files, logs, or temporary
  artifacts.
- Preserved optimal ZIP compression for the application payload.
- The build deletes its staging and previous output before rebuilding, then
  requires exactly one file in the release output.

## Final measured package

| Component | Files | Size |
|---|---:|---:|
| Compressed self-contained setup host | 1 | 71.613 MB |
| Embedded application ZIP | 112 | 7.322 MB |
| Extracted API payload | 81 | 17.370 MB raw |
| Extracted web payload | 26 | 3.580 MB raw |
| Deployment files | 4 | 0.010 MB raw |
| Online prerequisite manifest | 1 | <0.01 MB |
| **Online installer** | **1** | **79.4 MB** |

Final SHA-256:
`E6575996A4BC35DAB50D1F0B2E364BB49DE0C9BF7509D804CD23C36B467C8F7A`

The file metadata reports version `1.0.1` and publisher
`BAU Solutions Limited`. This build is not Authenticode-signed; provide the
production BAU Solutions code-signing certificate thumbprint to the build script
before external distribution so Windows can verify the publisher.

## Verification performed

- PowerShell build script parsed successfully.
- Setup wizard built with zero warnings and zero errors.
- Angular production build completed; existing bundle-budget and CommonJS
  advisories remain warnings and do not add development files to the package.
- All 62 backend automated tests passed.
- Final embedded-payload SHA-256 validation and extraction succeeded.
- Final wizard pages were launched and visually captured from the signed file.
- Extracted payload contains zero PDBs, zero source maps, and zero prerequisite
  executables.
- Pinned Microsoft, EnterpriseDB, and PostgreSQL/pgAdmin URLs each returned
  HTTP 200 on 2026-10-11.
- The pinned hashes match the official installers used to create the baseline
  offline package.

## Maintenance guidance

When a prerequisite version is upgraded, update its URL and digest together,
verify the upstream signature, build both package modes, and repeat the size and
launch checks. Never replace a pinned URL with an unversioned “latest” link.
Keep the online package as the normal distribution and generate the offline
package only for sites that have a documented disconnected-install requirement.
