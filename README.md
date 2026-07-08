# ⛪ United Church Platform (UCP)

[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![Angular](https://img.shields.io/badge/Angular-21.2-red)](https://angular.io/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-blue)](https://www.postgresql.org/)
[![License](https://img.shields.io/badge/License-Proprietary-green)](LICENSE)

A comprehensive enterprise‑grade church management system designed for the United Church of Zambia. The platform streamlines **payment requisitions**, **contributions**, **approval workflows**, and **fund management** with full audit trails, digital signatures, and real‑time reporting.

---

## 🚀 Live Demo

> *Coming soon*

---

## 📌 Table of Contents

- [Features](#-features)
- [Technology Stack](#-technology-stack)
- [System Architecture](#-system-architecture)
- [Getting Started](#-getting-started)
- [Development](#-development)
- [Database Schema](#-database-schema)
- [API Documentation](#-api-documentation)
- [Deployment](#-deployment)
- [Roadmap](#-roadmap)
- [Contributing](#-contributing)
- [License](#-license)

---

## ✨ Features

### Core Modules

| Module | Description |
|--------|-------------|
| **🔐 Authentication** | JWT-based login, role-based access control (RBAC), password policies |
| **👥 User Management** | Onboarding, signature registration, maker‑checker approval |
| **📋 Requisitions** | Create, submit, approve, reject, and track payment requests |
| **💰 Contributions** | Record, verify, deposit, and acknowledge incoming funds |
| **📁 Attachments** | Upload, store, and preview invoices, receipts, and supporting documents |
| **📊 Reporting** | Real‑time dashboards, exportable reports (CSV, Excel, PDF) |
| **🔄 Audit Trail** | Full audit logging with interceptor pattern |
| **🔒 Locking** | Optimistic & pessimistic concurrency control |
| **📱 Responsive** | Works on desktop, tablet, and mobile |

### Workflow Features

- **Multi‑stage approval** with configurable roles:
  - Group Representative → Senior Reps → Church Authorities → Minister
- **Conditional approvals** based on amount thresholds
- **Digital signatures** for all approval stages
- **Fund balance tracking** with committed/pending amounts
- **Automatic deduction** on approval

### Administrative Features

- **Section & Group management** (configurable 1-11 sections)
- **User activation/deactivation**
- **System configuration** (approval thresholds, max sections)
- **Audit log viewer**
- **License management** (per‑system licensing)

---

## 🏗️ Technology Stack

### Backend

| Technology | Version | Purpose |
|------------|---------|---------|
| [.NET](https://dotnet.microsoft.com/) | 8.0 | Runtime & framework |
| [ASP.NET Core](https://learn.microsoft.com/aspnet/core) | 8.0 | Web API |
| [Entity Framework Core](https://learn.microsoft.com/ef/core) | 8.0 | ORM |
| [PostgreSQL](https://www.postgresql.org/) | 15 | Primary database |
| [BCrypt.Net](https://github.com/BcryptNet/bcrypt.net) | Latest | Password hashing |
| [Serilog](https://serilog.net/) | Latest | Structured logging |
| [Swagger/OpenAPI](https://swagger.io/) | Latest | API documentation |

### Frontend

| Technology | Version | Purpose |
|------------|---------|---------|
| [Angular](https://angular.io/) | 21.2 | Framework |
| [TypeScript](https://www.typescriptlang.org/) | 5.0+ | Language |
| [Tailwind CSS](https://tailwindcss.com/) | 3.4 | Styling |
| [ngx-toastr](https://github.com/scttcper/ngx-toastr) | Latest | Notifications |
| [Axios](https://axios-http.com/) | Latest | HTTP client |

### Infrastructure

| Technology | Purpose |
|------------|---------|
| [Docker](https://www.docker.com/) | Containerization |
| [Docker Compose](https://docs.docker.com/compose/) | Orchestration |
| [Redis](https://redis.io/) | Caching (optional) |

---

## 🧩 System Architecture
┌─────────────────────────────────────────────────────────────────────────────┐
│ UNITED CHURCH PLATFORM │
├─────────────────────────────────────────────────────────────────────────────┤
│ │
│ ┌───────────────────────────────────────────────────────────────────────┐ │
│ │ PRESENTATION LAYER │ │
│ │ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ │ │
│ │ │ Login │ │ Dashboard │ │Requisitions │ │ │
│ │ │ Page │ │ (Stats) │ │ List │ │ │
│ │ └─────────────┘ └─────────────┘ └─────────────┘ │ │
│ │ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ │ │
│ │ │ Create │ │ Approval │ │ Groups │ │ │
│ │ │Requisition │ │ Dashboard │ │Management │ │ │
│ │ └─────────────┘ └─────────────┘ └─────────────┘ │ │
│ └───────────────────────────────────────────────────────────────────────┘ │
│ │ HTTPS │
│ ▼ │
│ ┌───────────────────────────────────────────────────────────────────────┐ │
│ │ API GATEWAY (ASP.NET Core 8) │ │
│ │ │ │
│ │ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ │ │
│ │ │ Auth │ │ Users │ │Requisitions │ │ Approval │ │ │
│ │ │ Service │ │ Service │ │ Service │ │ Service │ │ │
│ │ └─────────────┘ └─────────────┘ └─────────────┘ └─────────────┘ │ │
│ │ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ │ │
│ │ │ Audit │ │ Lock │ │ Fund │ │ Report │ │ │
│ │ │ Service │ │ Service │ │ Service │ │ Service │ │ │
│ │ └─────────────┘ └─────────────┘ └─────────────┘ └─────────────┘ │ │
│ └───────────────────────────────────────────────────────────────────────┘ │
│ │ │
│ ▼ │
│ ┌───────────────────────────────────────────────────────────────────────┐ │
│ │ DATA LAYER (PostgreSQL 15) │ │
│ │ │ │
│ │ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ │ │
│ │ │ Users │ │Requisitions │ │ Groups │ │ Sections │ │ │
│ │ └─────────────┘ └─────────────┘ └─────────────┘ └─────────────┘ │ │
│ │ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ │ │
│ │ │Requisition │ │ Audit │ │FundBalance │ │ Approval │ │ │
│ │ │ Items │ │ Logs │ │ │ │ Stages │ │ │
│ │ └─────────────┘ └─────────────┘ └─────────────┘ └─────────────┘ │ │
│ │ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ │ │
│ │ │Attachments │ │ System │ │ Locks │ │ │
│ │ │ (Atm) │ │ Config │ │ (Lck) │ │ │
│ │ └─────────────┘ └─────────────┘ └─────────────┘ │ │
│ └───────────────────────────────────────────────────────────────────────┘ │
│ │
│ ┌───────────────────────────────────────────────────────────────────────┐ │
│ │ INFRASTRUCTURE │ │
│ │ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ │ │
│ │ │ Docker │ │ Redis │ │ Nginx │ │ │
│ │ │ Container │ │ (Cache) │ │ (SSL) │ │ │
│ │ └─────────────┘ └─────────────┘ └─────────────┘ │ │
│ └───────────────────────────────────────────────────────────────────────┘ │
│ │
└─────────────────────────────────────────────────────────────────────────────┘

text

### Companion Tables Pattern

Each main entity has three companion tables for audit, attachments, and locking:

| Main Table | Audit Table | Attachments Table | Lock Table |
|------------|-------------|-------------------|------------|
| `Users` | `UserAud` | `UserAtm` | `UserLck` |
| `Requisitions` | `RequisitionAud` | `RequisitionAtm` | `RequisitionLck` |
| `Groups` | `GroupAud` | `GroupAtm` | `GroupLck` |
| `Sections` | `SectionAud` | `SectionAtm` | `SectionLck` |

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
Quick Start (Docker)
bash
# Start all services
docker-compose up --build

# The API will be available at http://localhost:5001
# The frontend will be available at http://localhost:4200
Manual Setup
Backend
bash
cd backend
dotnet restore
dotnet build
dotnet ef database update --project UCP.Infrastructure --startup-project UCP.API
dotnet run --project UCP.API
Frontend
bash
cd frontend/web
npm install
npm run tailwind:watch  # in a separate terminal
ng serve
Default Super Admin Credentials
⚠️ IMPORTANT: Change these credentials immediately!

Field	Value
Email	superadmin@ucp.org
Password	Admin@123!
🗄️ Database Schema
Key Entities
User
Id, FirstName, LastName, Email, PhoneNumber, MembershipId

Role (SuperAdmin, Admin, Accountant, Minister, ChurchAuth, SeniorRep, GroupRep)

Status (Pending, Active, Inactive)

SignatureData (Base64 encoded signature)

SectionId, GroupId

Requisition
RequisitionNumber (auto‑generated)

GroupId, RequisitorId, Payee

Amount, AmountWords

PaymentMethod (Cash, Cheque, BankTransfer, MobileMoney)

FundSource (Budgeted, OwnFunds)

Status (Draft, SeniorRepsPending, ChurchAuthPending, MinisterPending, Approved, MoneyReady, Collected, Completed, Rejected, Archived)

BudgetBalance, PaymentDetails

FundBalance
GroupId, FiscalYear

BudgetedBalance, OwnFundsBalance

CommittedBudgetedBalance, CommittedOwnFundsBalance

ApprovalStage
RequisitionId, StageOrder, StageName

RequiredRole (senior_rep_1, church_auth_1, minister, etc.)

UserId, SignatureData, SignedAt, Comments

Status (Pending, Approved, Rejected)

📚 API Documentation
Once running, Swagger UI is available at:

text
http://localhost:5001/swagger
Key Endpoints
Method	Endpoint	Description
POST	/api/auth/login	Authenticate user
POST	/api/auth/logout	Logout user
GET	/api/users	List all users
POST	/api/users	Create a new user
GET	/api/requisitions	List requisitions
POST	/api/requisitions	Create a requisition
POST	/api/requisitions/with-files	Create requisition with attachments
POST	/api/requisitions/{id}/submit	Submit for approval
POST	/api/approvals/{id}/sign	Sign/Approve a stage
POST	/api/approvals/{id}/reject	Reject a requisition
GET	/api/groups	List all groups
GET	/api/requisitions/stats	Dashboard statistics
🔧 Development
Environment Variables
Create a .env file in the backend folder:

env
ASPNETCORE_ENVIRONMENT=Development
JWT_SECRET=YourSuperSecretKeyHere
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=ucp_db;Username=postgres;Password=postgres
Code Style
Backend: Follow Microsoft's C# Coding Conventions

Frontend: Follow Angular Style Guide

Running Tests
bash
dotnet test
🚢 Deployment
Production Build
bash
# Backend
dotnet publish UCP.API -c Release -o ./publish

# Frontend
ng build --configuration production
Docker Production
bash
docker-compose -f docker-compose.prod.yml up -d
Environment Variables for Production
Variable	Description
ASPNETCORE_ENVIRONMENT	Set to Production
JWT_SECRET	Strong secret key
ConnectionStrings__DefaultConnection	Production PostgreSQL connection string
🗺️ Roadmap
Phase 1 (Completed)
✅ User authentication & RBAC

✅ Requisition creation & approval

✅ Digital signatures

✅ Fund balance management

✅ Attachments

✅ Basic reporting

✅ Group management

Phase 2 (In Progress)
🚧 Contributions module

🚧 Advanced reporting (Charts, Export)

🚧 Email notifications

Phase 3 (Planned)
🔜 Mobile app (React Native)

🔜 SMS notifications (Twilio / AfricasTalking)

🔜 Mobile money integration (M‑Pesa, Airtel Money)

🔜 Church membership directory

🔜 Budgeting module

🔜 Real‑time notifications (WebSockets)

🤝 Contributing
Fork the repository

Create a feature branch (git checkout -b feature/AmazingFeature)

Commit your changes (git commit -m 'Add some AmazingFeature')

Push to the branch (git push origin feature/AmazingFeature)

Open a Pull Request

Development Guidelines
Write unit tests for new features

Update documentation when changing functionality

Follow existing code style

Include JSDoc comments for public APIs

📄 License
This project is proprietary software. All rights reserved.

📞 Contact
United Church of Zambia

Email: support@ucp.org

Phone: +260 97 1234567

Website: www.ucp.org

🙏 Acknowledgements
United Church of Zambia leadership for the vision and support

All contributors and testers

Open source community for the tools and libraries

Project stakeholders for valuable feedback

Built with ❤️ for the Church

text

---

## ✅ What This README Includes

| Section | Content |
|---------|---------|
| **System Architecture** | Full visual diagram with all layers |
| **Companion Tables Pattern** | Explanation of Aud/Atm/Lck tables |
| **Database Schema** | Key entities and their fields |
| **Roadmap** | Completed, in-progress, and planned features |
| **Contact** | Church contact details |

---
