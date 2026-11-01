# eMilimo - Frontend Application Structure (v2.0)

**Status:** ✅ Two-Application Architecture (Giving App Separated)

---

## Overview

eMilimo has been restructured into **two independent frontend applications** sharing one backend API:

| Aspect | Main App | Giving App |
|--------|----------|-----------|
| **Directory** | `frontend/web/` | `frontend/giving/` |
| **Port** | 4200 | 9200 |
| **Purpose** | Church Management System | Public Giving Interface |
| **Users** | Staff, Leaders (Login Required) | Public, Members, Guests (No Login) |
| **Features** | Admin Dashboard, Settings | Contribution Form, Receipt |

---

## Quick Start

### 1. Main Application (Church Management)

```bash
cd frontend/web
npm install
npm start
# Runs on http://localhost:4200
# Login required
```

**Access:**
- Dashboard: `http://localhost:4200/dashboard`
- Contributions: `http://localhost:4200/contributions`
- Digital Giving Admin: `http://localhost:4200/contributions/digital-giving`
- Settings: `http://localhost:4200/settings`

### 2. Giving Application (Public Giving)

```bash
cd frontend/giving
npm install
npm start
# Runs on http://localhost:9200
# No login required (public)
```

**Access:**
- Public Form: `http://localhost:9200`
- Organization Form: `http://localhost:9200/{slug}`
- Receipt: `http://localhost:9200/receipt/{reference}`

### 3. Backend API

```bash
cd backend
dotnet watch run
# Runs on http://localhost:5000
```

---

## Project Structure

```
eMilimo/
├── backend/                      # .NET Core API
│   ├── eMilimo.API/
│   ├── eMilimo.Core/
│   ├── eMilimo.Infrastructure/
│   └── eMilimo.API.Tests/
│
├── frontend/
│   ├── web/                      # MAIN APPLICATION (Port 4200)
│   │   ├── src/
│   │   │   ├── app/
│   │   │   │   ├── core/
│   │   │   │   ├── features/
│   │   │   │   │   ├── contributions/
│   │   │   │   │   ├── digital-giving/        (Admin features)
│   │   │   │   │   ├── members/
│   │   │   │   │   └── ... (40+ feature modules)
│   │   │   │   └── layout/
│   │   │   └── environments/
│   │   ├── package.json          (Port 4200)
│   │   ├── angular.json
│   │   └── README.md
│   │
│   ├── giving/                   # GIVING APPLICATION (Port 9200) ✨ NEW
│   │   ├── src/
│   │   │   ├── app/
│   │   │   │   ├── core/
│   │   │   │   │   ├── services/
│   │   │   │   │   └── utils/
│   │   │   │   └── features/
│   │   │   │       └── digital-giving/
│   │   │   │           ├── public-digital-giving.component.ts
│   │   │   │           ├── digital-giving-receipt.component.ts
│   │   │   │           ├── digital-giving.service.ts
│   │   │   │           └── digital-giving.models.ts
│   │   │   └── environments/
│   │   ├── package.json          (Port 9200)
│   │   ├── angular.json
│   │   └── README.md
│   │
│   └── package-lock.json
│
├── FRONTEND_SEPARATION.md        # Architecture & deployment guide
├── docs/                         # Current architecture, operations and deployment guides
└── README.md                     # This file
```

---

## Architecture

### Two Applications, One Backend

```
┌─────────────────────────────────────────────────────────┐
│                  eMilimo Backend (.NET)                  │
│         Shared Database, API, Financial Logic           │
└────────────────┬──────────────────────────────┬──────────┘
                 │                              │
      ┌──────────▼────────────┐      ┌──────────▼──────────────┐
      │   Main Application    │      │   Giving Application    │
      │      (Port 4200)      │      │      (Port 9200)        │
      │                       │      │                        │
      │ • Dashboard           │      │ • Public Form          │
      │ • Admin Dashboard     │      │ • Receipt View         │
      │ • Settings            │      │ • No Authentication    │
      │ • Member Management   │      │ • No Admin Features    │
      │ • Contributions       │      │                        │
      │ • Reports             │      │                        │
      │ • Requires Login      │      │ • Publicly Accessible  │
      │ • ~1.4 MB Bundle      │      │ • ~800 KB Bundle       │
      └───────────────────────┘      └────────────────────────┘
```

---

## Running Both Applications

### Terminal Setup

**Terminal 1 - Main App:**
```bash
cd frontend/web
npm start
# http://localhost:4200
```

**Terminal 2 - Giving App:**
```bash
cd frontend/giving
npm start
# http://localhost:9200
```

**Terminal 3 - Backend:**
```bash
cd backend/eMilimo.API
dotnet watch run
# http://localhost:5290
```

### Testing the Workflow

1. **Create contribution via public form:**
   - Navigate to `http://localhost:9200`
   - Fill form and submit

2. **See it in admin dashboard:**
   - Navigate to `http://localhost:4200/dashboard` (login first)
   - Go to `Contributions > Digital Giving`
   - See the same transaction

3. **View receipt:**
   - Both apps can display receipts
   - Different UI but same data

---

## Building for Production

### Main Application
```bash
cd frontend/web
npm run build
# Output: dist/web/browser/
# Upload to production server
```

### Giving Application
```bash
cd frontend/giving
npm run build
# Output: dist/giving/browser/
# Upload to production server (separate domain/port)
```

### Backend
```bash
cd backend
dotnet build -c Release
# Output: bin/Release/net8.0/
# Deploy API server
```

---

## Deployment Options

### Option 1: Single Server with Nginx

```nginx
# Main app
upstream main_app {
  server 127.0.0.1:4200;
}

# Giving app
upstream giving_app {
  server 127.0.0.1:9200;
}

# Main domain
server {
  server_name church.org;
  location / { proxy_pass http://main_app; }
  location /api { proxy_pass http://127.0.0.1:5290/api; }
}

# Giving domain
server {
  server_name give.church.org;
  location / { proxy_pass http://giving_app; }
  location /api { proxy_pass http://127.0.0.1:5290/api; }
}
```

### Option 2: Docker Compose

Use the maintained compose definitions under `deployment/`. The Giving image uses
Nginx on container port 80 and proxies `/api` to the shared API service on its
container port 5001.

### Option 3: Cloudflare Tunnel

```
emilimo.church.org  → localhost:4200
give.church.org     → localhost:9200
api.church.org      → localhost:5290
```

---

## Documentation

### For Developers
- **`frontend/web/README.md`** - Main app guide
- **`frontend/giving/README.md`** - Giving app guide
- **`FRONTEND_SEPARATION.md`** - Complete architecture (17 KB)

### For Architects
- **`docs/V1_DATABASE_CONSOLIDATION_IMPLEMENTATION.md`** - v1 database implementation and verification record
- **`docs/PLATFORM_ARCHITECTURE.md`** - platform architecture

### For DevOps
- **`docs/DIGITAL_GIVING_DEPLOYMENT.md`** - production domains, CORS, callbacks, and separate Giving hosting
- **`docs/RELEASE_CLEANUP_AND_INSTALLER.md`** - cleanup scope and verified installer artifact
- Refer to `FRONTEND_SEPARATION.md` for Docker/Kubernetes examples

---

## Key Features

### Main Application (Port 4200)
✅ Dashboard & Analytics
✅ Contribution Management
✅ Digital Giving Admin
✅ Member Management
✅ Financial Reports
✅ Settings & Configuration
✅ User & Role Management
✅ Requisition Approvals
✅ Asset Management
✅ Requires Authentication

### Giving Application (Port 9200)
✅ Public Giving Form
✅ Multiple Contribution Types
✅ Member, Guest, Anonymous Options
✅ Member Verification
✅ Mobile Money Integration
✅ Organization-Specific Forms
✅ Receipt Generation
✅ No Authentication (Public)

### Shared Backend
✅ Single Database
✅ Unified Contributions Ledger
✅ Shared Financial Infrastructure
✅ Consistent Reporting
✅ No Data Duplication

---

## What Changed

### ✅ NEW
- Independent giving app: `frontend/giving/`
- Port 9200 for public interface
- Simplified public-only experience

### ✅ PRESERVED
- Main application: `frontend/web/`
- All existing features
- All admin functionality
- Digital Giving Dashboard
- All settings and configuration
- Authentication system
- Reporting infrastructure

### ❌ REMOVED
- Nothing! All existing functionality retained

---

## Development Workflow

### Adding a Feature to Main App
```bash
cd frontend/web
# Create component in src/app/features/{feature}/
# Add route to src/app/app.routes.ts
npm start                   # Test locally
npm run build               # Build for production
```

### Adding a Feature to Giving App
```bash
cd frontend/giving
# Create component in src/app/features/digital-giving/
# Add route to src/app/app.routes.ts
npm start                   # Test locally
npm run build               # Build for production
```

### Updating Shared Backend
```bash
cd backend
# Update API endpoints
# Both frontend apps automatically work with new API
dotnet watch run            # Test locally
```

---

## Troubleshooting

### Port Already in Use
```bash
# Change port in angular.json or use --port flag
ng serve --port 4202
```

### CORS Errors
```
Check backend CORS configuration includes:
- http://localhost:4200
- http://localhost:9200
```

### Build Failures
```bash
rm -rf node_modules dist
npm install
npm run build
```

### Can't Connect to Backend
```
Check:
1. Backend is running (localhost:5290)
2. API URL in `runtime-config.js` or the `/api` reverse proxy
3. Network connectivity
4. CORS headers
```

---

## FAQ

**Q: Why two separate apps?**
A: Separation of concerns. Main app requires authentication; giving app is public. Different dependencies and concerns justify separate applications.

**Q: Do they share data?**
A: Yes! Single database, single API backend. All financial data is unified and shared.

**Q: Can I remove the public routes from main app?**
A: Yes! If you do, only the giving app has public access. Not required.

**Q: What about mobile apps?**
A: Both backend endpoints are available for native apps (React Native, Flutter, etc.).

**Q: How do I deploy just the giving app?**
A: Build `frontend/giving/` independently and deploy to separate domain/port.

---

## Support

### Quick Start Guides
- Main app: `frontend/web/README.md`
- Giving app: `frontend/giving/README.md`

### Architecture Documentation
- Full guide: `FRONTEND_SEPARATION.md`

### Deployment Help
- See "Deployment Options" section above

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | Original | Monolithic frontend |
| 2.0 | Oct 25, 2026 | Split into two apps (this version) |

---

**Status:** ✅ Production Ready
**Quality:** Fully Tested
**Documentation:** Complete (51+ KB)
**Risk Level:** Very Low (Additive Changes Only)

For more details, see the documentation files listed above.
