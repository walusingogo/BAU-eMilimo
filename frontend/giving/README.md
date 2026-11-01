# eMilimo Digital Giving Application

Independent public-facing digital giving application for eMilimo Church Management System.

## Overview

This is a standalone Angular application dedicated to the public digital giving interface. It runs independently on **port 9200** (while the main eMilimo application runs on port 4200).

**Features:**
- Public-facing digital giving interface (no authentication required)
- Support for members, guests, and anonymous giving
- QR code support for physical giving campaigns
- Payment method selection and mobile money integration
- Receipt generation and viewing
- Multi-level contribution allocation (organization, section, group, committee)

**Note:** The administrative Digital Giving Dashboard remains in the main eMilimo application under **Contributions > Digital Giving**.

## Architecture

```
eMilimo Backend (Shared)
       ↓
   ────┬────
   │        │
   ▼        ▼
Main App   Giving App
(Port 4200) (Port 9200)
```

Both applications communicate with the same eMilimo backend API and share the same financial infrastructure.

## Quick Start

### Installation

```bash
cd frontend/giving
npm install
```

### Development

```bash
npm start
```

The application will be available at `http://localhost:9200`

Public giving interface: `http://localhost:9200` or `http://localhost:9200/{slug}`

Receipt view: `http://localhost:9200/receipt/{reference}`

### Production Build

```bash
npm run build
```

Output directory: `dist/giving/browser`

## Configuration

### Runtime configuration

The compiled application is environment-independent. Configure `public/runtime-config.js`
at deployment time instead of rebuilding the Angular bundle:

```javascript
window.EMILIMO_CONFIG = {
  API_URL: '/api',
  CHURCH_NAME: 'Trinity Congregation',
  ENVIRONMENT: 'production'
};
```

`API_URL: '/api'` is recommended when the web server proxies `/api` to eMilimo.
For a separately hosted API, use its HTTPS origin, for example
`https://api.church.example.org/api`, and add the Giving origin to the API's
production CORS allow-list. Local development automatically uses
`http://localhost:5290/api` (or the matching `127.0.0.1` host).

### Runtime Configuration

The application expects these endpoints from the backend:
- `GET /api/public/digital-giving` - Public configuration
- `GET /api/public/digital-giving/{slug}` - Organization-specific configuration
- `POST /api/public/digital-giving/{slug}/payment-intents` - Create payment
- `GET /api/public/digital-giving/payment-intents/{reference}/status` - Payment status
- `POST /api/public/digital-giving/member-verification/request` - Request verification code
- `POST /api/public/digital-giving/member-verification/verify` - Verify code

## Component Structure

```
src/app/
├── core/
│   ├── services/
│   │   ├── api.service.ts          # HTTP wrapper
│   │   └── auth.service.ts         # Church context
│   └── utils/
│       └── api-error.ts            # Error handling
├── features/
│   └── digital-giving/
│       ├── public-digital-giving.component.ts    # Main form
│       ├── digital-giving-receipt.component.ts   # Receipt display
│       ├── digital-giving.service.ts             # API calls
│       └── digital-giving.models.ts              # Type definitions
└── app.component.ts                # Root component
```

## Styling

The application uses:
- **Tailwind CSS** for utility-first styling
- **Responsive design** (mobile-first)
- **Accessible color scheme** with high contrast
- **Custom fonts:** Plus Jakarta Sans (body), Fraunces (headings)

## Payment Workflow

1. User selects contribution types and amounts
2. User chooses giver type (member, guest, anonymous)
3. Member verification (if member)
4. Payment method and phone number entry
5. Payment intent creation (idempotent)
6. User completes mobile money prompt on phone
7. Payment status polling
8. Receipt generation and viewing

## Integration with Main App

The main eMilimo application includes:
- **Administrative Dashboard** at `Contributions > Digital Giving`
- **Transaction management** (view, reconcile, flag, reverse)
- **Provider configuration** at `Settings > Digital Giving`
- **Financial reporting** integration

The public giving app operates independently but uses the same backend financial infrastructure.

## Deployment

### Docker

Use the included `Dockerfile` and `nginx.production.conf`. The Nginx configuration
provides Angular history fallback, prevents stale runtime configuration, and
proxies `/api` to the shared `api` service without exposing an admin application.

### Cloudflare Tunnel

Route the subdomain:
```
give.church.org → localhost:9200
```

Then access at: `https://give.church.org`

## Development Notes

- **Standalone Components**: This app uses Angular 21's standalone API (no NgModules)
- **Signals**: Reactive state management with Angular Signals
- **Lazy Loading**: Route components lazy-loaded on demand
- **No Store**: Simple service-based state (no NgRx/Akita needed)

## Shared Code

This application replicates only the public-facing components. Shared utilities with the main app:
- Digital giving models (interfaces)
- API service pattern
- Error handling utilities

Admin-only features remain in the main application.

## Testing

```bash
npm run test
```

## Troubleshooting

### CORS Issues

If the app can't reach the backend, check:
1. Backend CORS configuration includes `http://localhost:9200`
2. `public/runtime-config.js` points to the backend or `/api` is reverse-proxied
3. Backend is running and accessible

### Port Conflicts

If port 9200 is in use, change it in `angular.json`:
```json
{
  "serve": {
    "options": {
      "port": 4202
    }
  }
}
```

Or pass `--port` when running:
```bash
npm start -- --port 4202
```

## License

Part of eMilimo Church Management System.

## Support

For issues or questions:
- Check main eMilimo documentation
- Review backend API documentation
- Contact BAU Solutions support
