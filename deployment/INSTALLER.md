# eMilimo installer

Build the small online Windows installer from the repository root:

```powershell
.\deployment\build-installer.ps1
```

The online package contains the complete eMilimo application and a compressed,
self-contained setup bootstrapper. It downloads the pinned .NET Hosting Bundle,
PostgreSQL, or pgAdmin installer only when the selected deployment requires it.
Every downloaded executable is accepted only when its SHA-256 digest matches the
version-pinned manifest embedded in setup. Docker Desktop remains an explicit
administrator prerequisite; Docker images are pulled by Docker only when that
deployment mode is selected.

For a fully offline package, supply licensed prerequisite installers and the
pre-built Docker image archive:

```powershell
.\deployment\build-installer.ps1 `
  -PackageMode Offline `
  -OutputDirectory release\eMilimo-v1-offline-installer `
  -HostingBundleInstaller C:\Installers\dotnet-hosting-8-win.exe `
  -PostgresInstaller C:\Installers\postgresql-16-windows-x64.exe `
  -PgAdminInstaller C:\Installers\pgadmin4-windows-x64.exe `
  -DockerDesktopInstaller 'C:\Installers\Docker Desktop Installer.exe' `
  -DockerImagesArchive C:\Installers\emilimo-images.tar `
  -RequireFullyBundled
```

eMilimo does not require Memurai or a native Windows Redis MSI. Docker deployment
uses the official Valkey 8.1.10 Alpine image pinned to its Linux/amd64 SHA-256
manifest. Valkey is Redis-protocol compatible and distributed under BSD-3-Clause.
Its notices are shipped in `deployment/THIRD-PARTY-NOTICES.txt`.

For IIS and standalone deployments, setup offers either an existing Redis-compatible
endpoint or the supported in-memory fallback. Existing endpoints can also be selected
for Docker. Setup collects host, port, optional ACL username/password, TLS, and logical
database number, and performs an authenticated PING before installation. The managed
Valkey container is internal-only: port 6379 is not published to the Windows host and
setup generates a strong password for the service. Online setup pulls the pinned image
from its official registry; a fully offline distribution must include that exact image
in `emilimo-images.tar`.

The online package is written to `release/eMilimo-v1-installer` as the single file
`eMilimo-Setup-v1.0.1.exe`. An offline build is named
`eMilimo-Setup-v1.0.1-Offline.exe`; it should use a separate output directory as in
the example above. Both variants embed and integrity-check the complete application.

PostgreSQL is selectable for new IIS or standalone deployments. pgAdmin is optional
and is only enabled when its official signed Windows installer was supplied through
`-PgAdminInstaller`. The legal acceptance controls remain disabled until the user has
opened each document and scrolled to its end.

## Version and publisher

The current maintenance release is versioned consistently as `1.0.1` in the setup UI, executable
metadata, and Windows Apps & features registration. Apps & features displays
`BAU Solutions Limited` as the publisher after a successful install or repair.

Windows Explorer and User Account Control only display a verified publisher when
the executable has an Authenticode signature from a certificate trusted by the
target computer. Import the organisation's code-signing certificate (including its
private key) into the build user's Personal certificate store, then build with:

```powershell
.\deployment\build-installer.ps1 `
  -CodeSigningCertificateThumbprint YOUR_CERTIFICATE_THUMBPRINT
```

The build signs with SHA-256 and a trusted timestamp. It fails rather than shipping
a partially or incorrectly signed executable. Never commit a PFX file or password
to this repository.

The build deliberately excludes source code, symbols, development dependencies,
logs, uploads, backups, nested build artifacts, and duplicate web assets. The API
retains its small cross-platform native assets because the same payload supports
Windows services and Linux Docker containers. The setup wizard is a compressed, self-contained Windows x64
executable; trimming and ReadyToRun are deliberately disabled because this WinForms
bootstrapper uses reflection-sensitive deployment libraries. IIS and standalone
modes verify the ASP.NET Core 8 runtime and acquire the pinned Hosting Bundle when
using the online package.

During installation, diagnostics are written under:

`%LocalAppData%\BAU Solutions\eMilimo\Installer`

Setup streams command output into its activity panel, applies bounded timeouts, can
cancel a running process safely, and waits for `/health/ready` before reporting
success.

## Windows maintenance lifecycle

After a successful installation, setup registers eMilimo machine-wide under
Windows **Apps & features / Add or Remove Programs** and stores a maintenance
executable under the installation folder. Running setup again compares the package
version with the installed version:

- newer package: Upgrade, Repair, Modify, Launch, or Uninstall;
- same package: Repair, Modify, Launch, or Uninstall;
- older package: downgrade is blocked, while Launch and Uninstall remain available.

Repair uses a clean local repair cache and preserves the database, uploaded files,
deployment identity, JWT secret, and bootstrap configuration. Uninstall removes IIS,
Docker, or standalone application registration as appropriate. PostgreSQL and other
shared prerequisite services are not automatically removed. The user may preserve
attachments and configuration under `%ProgramData%\BAU Solutions\eMilimo\PreservedData`.
