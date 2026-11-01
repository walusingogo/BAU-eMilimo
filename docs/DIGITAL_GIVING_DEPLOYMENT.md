# Digital Giving public deployment

Digital Giving separates the public browser origin from the authenticated administration boundary while continuing to use the same API and accounting services.

## Required public URLs

Configure three externally reachable HTTPS origins:

- `Frontend__Url` and the `frontend_base_url` system configuration: the authenticated eMilimo administration origin, for example `https://admin.church.example.org`.
- `Giving__PublicUrl` and the `giving_base_url` system configuration: the independent public Giving origin, for example `https://give.church.example.org`.
- `DigitalGiving__PublicApiBaseUrl`: the externally reachable API origin, for example `https://api.church.example.org`.

Add the exact administration and Giving origins to `Cors__AllowedOrigins`. Origins are matched exactly after removing a trailing slash. Production no longer implicitly trusts localhost, private-network addresses, or every site using ports 4200 or 9200.

The frontend runtime configuration should contain:

```javascript
window.__EMILIMO_CONFIG__ = {
  apiUrl: "https://api.church.example.org/api",
  fileBaseUrl: "https://api.church.example.org",
  givingBaseUrl: "https://give.church.example.org",
  congregationName: "Example Congregation"
};
```

The Giving application is a separate root application, not an administration route and not a `/give` subpath. Its runtime configuration is:

```javascript
window.EMILIMO_CONFIG = {
  API_URL: "https://api.church.example.org",
  CHURCH_NAME: "Example Congregation",
  ENVIRONMENT: "production"
};
```

When either browser application and the API share a reverse-proxied origin, relative `/api` routing remains supported.

## Cloudflare Tunnel-compatible routing

Route only the public HTTP services through the tunnel:

- `admin.church.example.org` to the administration web service.
- `give.church.example.org` to the Giving web service.
- `api.church.example.org` to the API service.

Do not expose PostgreSQL or Valkey/Redis through a public tunnel. Keep database, cache, and container-management ports on the private host network.

Provider callback URLs are generated from `DigitalGiving__PublicApiBaseUrl` as:

```text
https://api.church.example.org/api/public/digital-giving/webhooks/{providerKey}
```

In Production, the payment-intent service rejects missing, non-HTTP(S), loopback, or non-HTTPS public callback origins unless the explicit local-installer override is enabled. The Windows local installer enables that override for its LAN/local, mock-provider deployment only. Do not enable it for an internet-facing installation.

## Production checklist

1. Set the external administration, Giving, and API HTTPS origins.
2. Update `frontend_base_url` and `giving_base_url` to their respective origins.
3. Set `Cors__AllowedOrigins` to the exact administration and Giving origins.
4. Verify the Giving domain root opens without authentication.
5. Verify administrative routes still return `401` without authentication.
6. Verify the provider callback URL resolves publicly over HTTPS.
7. Keep real provider routes disabled until official credentials and callback-signature validation are configured.
8. Complete a sandbox payment and confirm that only an authenticated provider callback can post contributions.
9. Confirm receipt links contain the production Giving domain and never localhost.
10. Confirm PostgreSQL and Valkey/Redis are not publicly reachable.

## Windows installer

The setup wizard keeps the applications separate by default:

- Administration: port `4200`.
- API: port `5290` for Docker, or the administration listener for IIS/standalone.
- Public Giving: port `9200`.

Under **Advanced options**, production installations can supply full public administration, API, and Giving URLs. These values configure runtime links and CORS; DNS records, TLS certificates, and the reverse proxy or Cloudflare Tunnel must still be provisioned by the operator. The installer does not attempt to own public DNS or certificate issuance.

