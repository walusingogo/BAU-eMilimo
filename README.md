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
