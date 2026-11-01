# Application configuration

eMilimo's application-wide configuration is stored in `backend/eMilimo.API/appsettings.json`. Use the normal ASP.NET Core environment-specific files or environment variables (for example, `Security__Session__IdleTimeoutMinutes`) for deployment overrides. These settings are loaded and validated at API startup, so changing them requires an API restart.

## Security

- `Security:PasswordPolicy` controls minimum/maximum length, character requirements, and password-expiry days. The API is authoritative; the browser receives the same non-sensitive policy through `GET /api/auth/client-settings` only to provide immediate guidance.
- `Security:Session:LifetimeMinutes` controls JWT lifetime.
- `Security:Session:IdleTimeoutMinutes` controls inactivity logout.
- `Security:Session:WarningCountdownSeconds` controls how long before logout the browser warning appears. The warning cannot extend the server-issued token beyond its expiry.

Invalid password or session values stop the API during startup instead of silently applying unsafe settings.

## User interface

- `UI:LoadingOverlay:DelayMilliseconds` prevents flicker for short operations.
- `UI:LoadingOverlay:MinimumDisplayMilliseconds` prevents a displayed overlay from flashing away immediately.
- `UI:Notifications:DisplayDurationMilliseconds`, `UI:Pagination:DefaultPageSize`, and `UI:Requests:TimeoutMilliseconds` are the central defaults for shared UI behaviour.

The request timeout is applied by the shared browser API service and the notification duration is applied to the shared toast service after client configuration loads.

## Uploads

- `Uploads:MaximumFileSizeBytes` sets the application-wide per-file limit (1 KB to 100 MB).
- `Uploads:AllowedExtensions` and `Uploads:AllowedContentTypes` must both accept a file before it can be stored. Contribution and Service Collection attachments use this shared policy.

Upload configuration is validated when the API starts and its non-sensitive limits are returned to the browser for consistent client-side guidance. Server-side validation remains authoritative.

Only clamped, non-sensitive UI settings are returned to the browser by `GET /api/auth/client-settings`. Database credentials, signing secrets, SMTP credentials, license material, and other server-only settings are never included.

## Deployment notes

- `appsettings.Development.json` may override development-only values.
- Production secrets should be supplied by the installer, protected configuration provider, or environment variables, not committed JSON.
- Configuration changes currently require an API restart and browser reload; they are not runtime-maintenance settings.
