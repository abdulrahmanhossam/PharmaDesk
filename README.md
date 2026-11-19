# 💊 PharmaDesk — Pharmacy Management System

> A fast, reliable, offline-first pharmacy management system built with **ASP.NET Core MVC (.NET 9)**, **Entity Framework Core**, and **Microsoft SQL Server Express**. Designed for community retail pharmacies to run smoothly on a counter PC with no internet required.

[![.NET 9](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/)
[![EF Core 9](https://img.shields.io/badge/EF%20Core-9.0-purple.svg)](https://learn.microsoft.com/en-us/ef/core/)
[![Database](https://img.shields.io/badge/Database-SQL%20Server%20Express-red.svg)](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

---

## 📖 Table of Contents
- [Overview](#-overview)
- [Key Features (Version 1.0)](#-key-features-version-10)
- [Technology Stack](#-technology-stack)
- [System Architecture](#-system-architecture)
- [Project Status](#-project-status)
- [Getting Started & Local Setup](#-getting-started--local-setup)
- [Project Documentation](#-project-documentation)
- [Contributing & Development Workflow](#-contributing--development-workflow)

---

## 🌟 Overview

Community retail pharmacies need speed, precision, and bulletproof offline reliability at the dispensing counter. Cloud-dependent POS software frequently fails during network disruptions, while manual stock management leads to expired medications sitting unnoticed on shelves.

**PharmaDesk** is designed to solve these problems directly:
- **Zero Internet Requirement**: Runs 100% locally on standard desktop hardware; all styling, fonts, and scripts are stored in the repository.
- **Strict FEFO Inventory**: Batches expiring first are automatically allocated during sales, preventing inventory waste.
- **Fast Barcode Counter**: Automatic cursor focus on the barcode field allows cashiers to scan and checkout without touching the mouse.
- **Two-Tier Role Security**: Admin and Staff accounts protected by hashed PINs and a 5-attempt lockout policy.

---

## ✨ Key Features (Version 1.0)

### 🛒 Point of Sale & Dispensing
- **Barcode-Driven Checkout**: Autofocus barcode scanner input, instant quantity incrementing, and single-click / enter checkout.
- **FEFO Stock Allocation**: First-Expired, First-Out rule automatically splits quantities across multiple batches when needed.
- **On-Screen Thermal Receipts**: Monospaced printable receipt slip formatted for 80mm/58mm thermal printers with automatic `window.print()` trigger.
- **Immutable Price Snapshots**: Historical sales preserve the exact price at checkout, immune to future catalog price changes.

### 📦 Batch-Level Inventory Control
- **Multi-Batch Tracking**: Each shipment creates a distinct batch with its lot number, expiry date, unit cost, and available quantity.
- **Expiry Color Warnings**: Real-time shelf-life alerts: 🔴 **Red** for expired batches ($\le$ today) and 🟠 **Orange** for items expiring within 90 days.
- **Low Stock Thresholds**: Configurable alert level per medicine with prominent dashboard warning cards.

### 🚚 Supplier & Purchase Invoices
- **Supplier Directory**: Vendor management with soft-delete protection for historical purchase records.
- **Multi-Line Purchase Entry**: Receiving form with searchable dropdowns (Select2); saving an invoice instantly creates active inventory batches.

### 🔒 Administration & Security
- **Role-Based Access Control**: Strict separation between `Admin` (full system access) and `Staff` (counter dispensing & view-only catalog).
- **Hashed Numeric PINs**: 4-digit PIN authentication hashed with PBKDF2 (`PasswordHasher<TeamMember>`).
- **5-Attempt Lockout**: 5 consecutive incorrect PIN entries lock the account for 5 minutes.
- **Forensic Activity Log**: Append-only audit trail recording logins, sales, purchases, catalog price modifications, and backups.
- **Database Backup**: One-click SQL Server `.bak` database snapshot generation to local folder or USB.

---

## 🛠 Technology Stack

| Layer | Technology | Details |
|---|---|---|
| **Framework** | ASP.NET Core MVC | .NET 9.0 (C# 13) |
| **Data Access** | Entity Framework Core 9 | Code-First with Fluent API relationships and indexes |
| **Database Engine** | SQL Server Express / LocalDB | Microsoft SQL Server (LAN-accessible for future multi-terminal use) |
| **Frontend UI** | Razor Views + Bootstrap 5 | Locally stored Bootstrap 5.3 + Bootstrap Icons (zero CDN calls) |
| **Client Scripting** | JavaScript (ES6) + jQuery | POS cart logic (`sale.js`), Select2 searchable dropdowns |
| **Authentication** | Cookie Authentication | Encrypted session cookies with custom PIN verification |
| **Password Hashing**| PBKDF2 (`PasswordHasher`) | ASP.NET Core Identity PBKDF2 with HMAC-SHA256 & salt |

---

## 🏗 System Architecture

PharmaDesk operates as an offline-first modular monolith:

```
┌────────────────────────────────────────────────────────┐
│                   Web Browser / POS Terminal           │
└───────────────────────────┬────────────────────────────┘
                            │ HTTP (Localhost / LAN)
┌───────────────────────────▼────────────────────────────┐
│ Presentation Layer: ASP.NET Core MVC (.NET 9)          │
│ • Razor Views & ViewModels                             │
│ • Controllers (Account, Home, Sale, Medicine, etc.)    │
│ • Cookie Authentication & [Authorize(Roles = "Admin")] │
└───────────────────────────┬────────────────────────────┘
                            │
┌───────────────────────────▼────────────────────────────┐
│ Domain & Service Layer                                 │
│ • SaleService (FEFO Multi-Batch Allocation)            │
│ • StockService (Inventory Balances & Expiry Rules)     │
│ • ReportService (Daily Sales Analytics)                │
│ • BackupService (SQL Server .bak Automation)           │
└───────────────────────────┬────────────────────────────┘
                            │
┌───────────────────────────▼────────────────────────────┐
│ Data Access Layer: EF Core 9 (PharmaDeskContext)       │
│ • 10 Domain Entities                                   │
│ • Restrict & Cascade Delete Behaviors                  │
│ • Unique Indexes (Barcode, ReceiptNumber, Category)    │
└───────────────────────────┬────────────────────────────┘
                            │ SQL Connection
┌───────────────────────────▼────────────────────────────┐
│ Microsoft SQL Server Express (PharmaDeskDB)            │
└────────────────────────────────────────────────────────┘
```

---

## 📊 Project Status

The project is actively being developed following the 12-step implementation roadmap:

| Step | Milestone | Status | Details |
|:---:|---|:---:|---|
| **01** | Foundation & Database Tables | 🟡 In Progress | Domain models, DbContext, relationships complete; migrations pending. |
| **02** | Authentication & Roles | 🟡 In Progress | PIN login, lockout, and seeder complete; server-side role attributes pending. |
| **03** | Layout & Navigation Menu | 🟢 Done | Responsive sidebar, calm green theme, dynamic role menu. |
| **04** | Medicines & Categories | 🟢 Done | Category & Medicine CRUD, barcode lookup, active toggle. |
| **05** | Suppliers Directory | 🟢 Done | Supplier management and purchase protection. |
| **06** | Purchases & Inward Stock | 🟡 In Progress | Multi-item purchase form works; user context resolution pending. |
| **07** | Sales Counter POS & Receipts | 🟡 In Progress | Autofocus cart, FEFO deduction, receipt view working; user scoping pending. |
| **08** | Stock & Warning Reports | 🟢 Done | Consolidated stock, batch drill-down, expiry & low stock reports. |
| **09** | Daily Sales Financial Report | 🔴 Not Started | End-of-day revenue reconciliation screen. |
| **10** | Database Backup & Activity Log | 🟡 In Progress | Audit table & login/sale logging exist; UI screens pending. |
| **11** | UI Polish & Counter Ergonomics | 🟡 In Progress | Style guide applied; accessibility audit ongoing. |
| **12** | Self-Contained Offline Publish | 🔴 Not Started | Release packaging for Windows counter PCs. |

---

## 🚀 Getting Started & Local Setup

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (verify with `dotnet --version`).
- [Microsoft SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) or **SQL Server LocalDB** (included with Visual Studio / Rider).
- [Git](https://git-scm.com/).
- *(Optional)* [EF Core CLI Tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet):
  ```bash
  dotnet tool install --global dotnet-ef
  ```

### 1. Clone the Repository
```bash
git clone https://github.com/abdulrahmanhossam/PharmaDesk.git
cd PharmaDesk
```

### 2. Configure Connection String
Open `PharmaDesk/appsettings.json` and verify your local SQL Server instance:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=PharmaDeskDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```
*Note: If using LocalDB, replace `Server=.` with `Server=(localdb)\\mssqllocaldb`.*

### 3. Generate Database & Migrations
*(Status note: Migrations are currently being created in the initial engineering cycle)*. Run the following command from the repository root:
```bash
dotnet ef migrations add InitialCreate --project PharmaDesk
dotnet ef database update --project PharmaDesk
```

### 4. Build and Run
```bash
dotnet build PharmaDesk/PharmaDesk.csproj
dotnet run --project PharmaDesk
```
Open your browser and navigate to:
```
http://localhost:5000  (or https://localhost:5001)
```

### 5. First Login (Default Seeded Admin)
On initial application launch, `DbSeeder.cs` automatically provisions the primary administrator account:
- **User**: Select **Admin** from the login dropdown.
- **Default PIN**: `1234`
*(Important: Reset this PIN immediately upon first login under Team Members).*

---

## 📚 Project Documentation

Comprehensive architectural, data modeling, and operational documentation is located in the [`docs/`](docs/) directory:

- [**01. Product Overview**](docs/01-product-overview.md) — Business context, target users, v1 scope, and deferred features.
- [**02. Roles and Permissions**](docs/02-roles-and-permissions.md) — Matrix of permissions per role and enforcement architecture.
- [**03. System Architecture**](docs/03-architecture.md) — Architectural style, layers, folder layout, and request flows.
- [**04. Data Model & Dictionary**](docs/04-data-model.md) — Corrected Mermaid ERD, table-by-table dictionary, delete behaviors, and indexes.
- [**05. Core Business Rules**](docs/05-business-rules.md) — FEFO algorithm, stock calculations, color coding, and transaction sequence diagrams.
- [**06. Screens & Style Guide**](docs/06-screens.md) — Complete screen directory, routes, role access, and UI design tokens.
- [**07. Security Architecture**](docs/07-security.md) — PIN hashing, lockout mechanics, RBAC gates, and audit trails.
- [**08. Development Workflow**](docs/08-development-workflow.md) — Git branching, Conventional Commits, PR rules, DoD, and work splitting.
- [**09. Implementation Roadmap**](docs/09-roadmap.md) — 12-step plan with acceptance criteria and live status.
- [**Design vs. Code Audit**](docs/design-vs-code.md) — Detailed gap analysis between technical design and current code.
- [**Architecture Decision Records (ADRs)**](docs/adr/) — 10 architectural decisions documenting context, decisions, and consequences.
- [**Original Design Assets**](docs/assets/README.md) — Archived PDF specifications and design notes.

---

## 🤝 Contributing & Development Workflow

We welcome contributions! Please review our [**Contributing Guide**](CONTRIBUTING.md) and [**Development Workflow**](docs/08-development-workflow.md) before submitting code.

1. Create a feature branch from `main`: `git checkout -b feat/your-feature-name`
2. Follow **Conventional Commits**: `feat:`, `fix:`, `docs:`, `refactor:`, `chore:`
3. Ensure `dotnet build` succeeds with 0 errors and 0 warnings.
4. Open a Pull Request referencing the corresponding issue.

---

## 📄 License
This project is licensed under the [MIT License](LICENSE).
