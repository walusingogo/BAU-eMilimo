# eMilimo Version 1

[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![Angular](https://img.shields.io/badge/Angular-21.2-red)](https://angular.io/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-blue)](https://www.postgresql.org/)
[![License](https://img.shields.io/badge/License-Proprietary-green)](LICENSE)

A comprehensive enterprise‑grade church management system designed for the **United Church of Zambia**.  
The platform streamlines **payment requisitions**, **contributions**, **approval workflows**, and **fund management** with full audit trails, digital signatures, and real‑time reporting.

---

## 🚀 Live Demo
> Coming soon

---

## 📌 Table of Contents
- [Features](#-features)
- [Technology Stack](#-technology-stack)
- [System Architecture](#-system-architecture)
- [Getting Started](#-getting-started)
- [Database Schema](#-database-schema)
- [API Documentation](#-api-documentation)
- [Development](#-development)
- [Deployment](#-deployment)
- [Roadmap](#-roadmap)
- [Contributing](#-contributing)
- [License](#-license)
- [Contact](#-contact)

---

## ✨ Features

### Core Modules
| Module | Description |
|--------|-------------|
| **🔐 Authentication** | JWT-based login, RBAC, password policies |
| **👥 User Management** | Onboarding, signature registration, maker‑checker approval |
| **📋 Requisitions** | Create, submit, approve, reject, and track payment requests |
| **💰 Contributions** | Record, verify, deposit, and acknowledge incoming funds |
| **📁 Attachments** | Upload, store, and preview invoices, receipts, supporting docs |
| **📊 Reporting** | Real‑time dashboards, exportable reports (CSV, Excel, PDF) |
| **🔄 Audit Trail** | Full audit logging with interceptor pattern |
| **🔒 Locking** | Optimistic & pessimistic concurrency control |
| **📱 Responsive** | Works on desktop, tablet, and mobile |

### Workflow Features
- Multi‑stage approval with configurable roles:
  - Group Rep → Senior Reps → Church Authorities → Minister
- Conditional approvals based on thresholds
- Digital signatures at all approval stages
- Fund balance tracking (committed/pending)
- Automatic deduction on approval

### Administrative Features
- Section & Group management (configurable 1–11 sections)
- User activation/deactivation
- System configuration (approval thresholds, max sections)
- Audit log viewer
- License management (per‑system licensing)

---

## 🏗️ Technology Stack

### Backend
- [.NET 8](https://dotnet.microsoft.com/) – Runtime & framework  
- [ASP.NET Core 8](https://learn.microsoft.com/aspnet/core) – Web API  
- [Entity Framework Core 8](https://learn.microsoft.com/ef/core) – ORM  
- [PostgreSQL 15](https://www.postgresql.org/) – Database  
- [BCrypt.Net](https://github.com/BcryptNet/bcrypt.net) – Password hashing  
- [Serilog](https://serilog.net/) – Structured logging  
- [Swagger/OpenAPI](https://swagger.io/) – API docs  

### Frontend
- [Angular 21.2](https://angular.io/) – Framework  
- [TypeScript 5+](https://www.typescriptlang.org/) – Language  
- [Tailwind CSS 3.4](https://tailwindcss.com/) – Styling  
- [ngx-toastr](https://github.com/scttcper/ngx-toastr) – Notifications  
- [Axios](https://axios-http.com/) – HTTP client  

### Infrastructure
- [Docker](https://www.docker.com/) – Containerization  
- [Docker Compose](https://docs.docker.com/compose/) – Orchestration  
- [Redis](https://redis.io/) – Distributed cache and shared-instance coordination

---

## 🧩 System Architecture
```
Presentation Layer → API Gateway (ASP.NET Core 8) → Data Layer (PostgreSQL 15) → Infrastructure (Docker, Redis, Nginx)
```

Each main entity has three companion tables for **audit**, **attachments**, and **locking**.

| Main Table | Audit Table | Attachments Table | Lock Table |
|------------|-------------|-------------------|------------|
| Users | UserAud | UserAtm | UserLck |
| Requisitions | RequisitionAud | RequisitionAtm | RequisitionLck |
| Groups | GroupAud | GroupAtm | GroupLck |
| Sections | SectionAud | SectionAtm | SectionLck |

---

## 🚀 Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)  
- [Node.js 18+](https://nodejs.org/)  
- [PostgreSQL 15](https://www.postgresql.org/download/)  
- [Docker & Docker Compose](https://www.docker.com/products/docker-desktop/) (optional)  
- [Angular CLI 21+](https://angular.io/cli)  

### Clone the Repository
```bash
git clone https://github.com/yourusername/ucp.git
cd ucp
```

### Version 1 setup wizard (Windows)

Run `deployment/installer/eMilimo-Setup.exe`. The BAU Solutions deployment wizard validates Docker Desktop, creates cryptographically random deployment secrets, starts PostgreSQL, Redis, the API and the web portal, then opens the protected first-run setup screen. Completing a new setup installs a 30-day machine-bound trial.

### Quick Start (Docker)
```bash
docker-compose up --build
```
- API → `http://localhost:5001`  
- Frontend → `http://localhost:4200`

### Manual Setup
**Backend**
```bash
cd backend
dotnet restore
dotnet build
dotnet ef database update --project UCP.Infrastructure --startup-project UCP.API
dotnet run --project UCP.API
```

**Frontend**
```bash
cd frontend/web
npm install
npm run tailwind:watch   # run in separate terminal
ng serve
```

### Bootstrap SuperAdmin

There are no default credentials. A locked, inactive bootstrap record is created only when the user table is empty. The first-run wizard securely claims that record and requires a password conforming to the configured policy.

Only essential configuration is seeded. Demo users, entities, funds, contribution types, sources and bank accounts are never inserted into a clean installation.

---

## 🗄️ Database Schema

### User
- Id, FirstName, LastName, Email, PhoneNumber, MembershipId  
- Role (SuperAdmin, Admin, Accountant, Minister, ChurchAuth, SeniorRep, GroupRep)  
- Status (Pending, Active, Inactive)  
- SignatureData (Base64)  
- SectionId, GroupId  

### Requisition
- RequisitionNumber (auto‑generated)  
- GroupId, RequisitorId, Payee  
- Amount, AmountWords  
- PaymentMethod (Cash, Cheque, BankTransfer, MobileMoney)  
- FundSource (Budgeted, OwnFunds)  
- Status (Draft → Approved → Completed, etc.)  
- BudgetBalance, PaymentDetails  

### FundBalance
- GroupId, FiscalYear  
- BudgetedBalance, OwnFundsBalance  
- CommittedBudgetedBalance, CommittedOwnFundsBalance  

### ApprovalStage
- RequisitionId, StageOrder, StageName  
- RequiredRole, UserId, SignatureData, SignedAt, Comments  
- Status (Pending, Approved, Rejected)  

---

## 📚 API Documentation
Once running, Swagger UI is available at:  
`http://localhost:5001/swagger`

### Key Endpoints
- `POST /api/auth/login` – Authenticate user  
- `POST /api/auth/logout` – Logout  
- `GET /api/users` – List users  
- `POST /api/users` – Create user  
- `GET /api/requisitions` – List requisitions  
- `POST /api/requisitions` – Create requisition  
- `POST /api/requisitions/with-files` – Create requisition with attachments  
- `POST /api/requisitions/{id}/submit` – Submit for approval  
- `POST /api/approvals/{id}/sign` – Approve stage  
- `POST /api/approvals/{id}/reject` – Reject requisition  
- `GET /api/groups` – List groups  
- `GET /api/requisitions/stats` – Dashboard stats  

---

## 🔧 Development

### Environment Variables
Create `.env` in backend:
```env
ASPNETCORE_ENVIRONMENT=Development
JWT_SECRET=YourSuperSecretKeyHere
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=emilimo_db;Username=postgres;Password=postgres
```

### Code Style
- Backend → Microsoft C# conventions  
- Frontend → Angular Style Guide  

### Running Tests
```bash
dotnet test
```

---

## 🚢 Deployment

### Production Build
```bash
# Backend
dotnet publish UCP.API -c Release -o ./publish

# Frontend
ng build --configuration production
```

### Docker Production
```bash
docker-compose -f docker-compose.prod.yml up -d
```

### Environment Variables
- `ASPNETCORE_ENVIRONMENT=Production`  
- `JWT_SECRET=StrongSecretKey`  
- `ConnectionStrings__DefaultConnection=Production PostgreSQL connection string`  

---

## 🗺️ Roadmap

**Phase 1 (Completed)**  
✅ Authentication & RBAC  
✅ Requisition creation & approval  
✅ Digital signatures  
✅ Fund balance management  
✅ Attach
