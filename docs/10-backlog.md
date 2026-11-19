# 10. Engineering Backlog & Task Distribution

This backlog breaks down all remaining work, identified architectural gaps, and feature enhancements into self-contained engineering tasks. Tasks are structured to enable **two developers (Dev A and Dev B)** to work in parallel on isolated files with zero merge conflicts.

---

## 1. Work Division Strategy & Build Order

The work division strictly follows the project build sequence:
`Foundation > Login/Roles > Layout/Menu > Medicines & Categories > Suppliers > Purchases > Sales/Receipt > Stock/Reports > Backup/Activity Log > Polish > Publish`

| Developer | Primary Domain Ownership | Key Controllers & Services |
|---|---|---|
| **Dev A** | **Authentication, Security Gates, Reporting & Data Migrations** | `AccountController`, `CategoryController`, `SupplierController`, `ReportController`, `SaleController` (History filter), EF Migrations |
| **Dev B** | **Inventory Services, Purchasing, Backups & Forensic Audit** | `StockService`, `PurchaseController`, `BackupService`, `BackupController`, `ActivityLogController`, `MedicineController` (Audit logging) |

---

## 2. Milestone & Task Registry

### Milestone 01: Foundation & Database Infrastructure (Roadmap Step 1)

#### Task 1.1: Generate Initial EF Core Migration & Seeder Verification
- **Assignee**: Dev A
- **Labels**: `area:database`, `type:task`, `priority:high`
- **Dependencies**: None
- **Affected Files**: `PharmaDesk/Migrations/*`, `PharmaDesk/Data/PharmaDeskContext.cs`
- **Description**: Run `dotnet ef migrations add InitialCreate --project PharmaDesk` to create the foundational schema migration for all 10 tables, indexes, and delete behaviors. Verify `DbSeeder.cs` applies cleanly on an empty database.
- **Acceptance Criteria**:
  - `dotnet ef migrations add InitialCreate` creates a clean migration without warnings.
  - Running `dotnet ef database update` generates all 10 tables, foreign keys, and indexes.
  - First launch seeds default Admin user (`PIN 1234`) and standard categories.

#### Task 1.2: Remove Dead Model Files and Fix Controller Typo
- **Assignee**: Dev A
- **Labels**: `area:code-quality`, `type:chore`, `priority:low`
- **Dependencies**: None
- **Affected Files**: `PharmaDesk/Models/Batches.cs`, `Purchases.cs`, `'PurchasItem .cs'`, `Suppliers.cs`, `'CategoryController .cs'`
- **Description**: Delete the four 0-byte redundant model files (`Batches.cs`, `Purchases.cs`, `Suppliers.cs`, and `'PurchasItem .cs'`). Rename `'CategoryController .cs'` to remove the trailing whitespace before `.cs`.
- **Acceptance Criteria**:
  - Zero-byte duplicate files are removed.
  - `'CategoryController .cs'` is renamed cleanly to `CategoryController.cs`.
  - Project builds with 0 errors.

---

### Milestone 02: Authentication & Security Hardening (Roadmap Step 2)

#### Task 2.1: Enforce Server-Side Role Authorization on Admin Controllers (CRITICAL)
- **Assignee**: Dev A
- **Labels**: `area:security`, `type:bug`, `priority:critical`
- **Dependencies**: None
- **Affected Files**: `Controllers/CategoryController.cs`, `SupplierController.cs`, `PurchaseController.cs`, `MedicineController.cs`
- **Description**: Address **GAP 1** from `docs/design-vs-code.md`. Add `[Authorize(Roles = "Admin")]` to `CategoryController`, `SupplierController`, and `PurchaseController`. In `MedicineController`, protect `Create`, `Edit`, and `ToggleActive` with `[Authorize(Roles = "Admin")]` while preserving read-only access on `Index` for Staff.
- **Acceptance Criteria**:
  - Staff user navigating directly to `/Purchase/Create`, `/Supplier/Create`, or `/Category/Create` is redirected to `/Account/AccessDenied` (HTTP 403).
  - Staff user cannot POST to `/Medicine/Create` or `/Medicine/Edit`.
  - Admin retains full unrestricted access.

#### Task 2.2: Standardize Account Lockout & Timezone Handling on UTC
- **Assignee**: Dev A
- **Labels**: `area:security`, `type:bug`, `priority:medium`
- **Dependencies**: None
- **Affected Files**: `Controllers/AccountController.cs`, `Models/TeamMember.cs`
- **Description**: Address **GAP 10**. Refactor `AccountController` lockout checks to compare against `DateTime.UtcNow` instead of `DateTime.Now`. Ensure `LockedUntil` calculation and `CreatedAt` use UTC across the entire authentication lifecycle.
- **Acceptance Criteria**:
  - Lockout time calculation functions accurately across daylight savings transitions.
  - Remaining lockout minutes display properly on the login screen.

---

### Milestone 06: Purchases & Inward Inventory Management (Roadmap Step 6)

#### Task 6.1: Resolve Current Cashier Context in Sales and Purchases
- **Assignee**: Dev A
- **Labels**: `area:sales`, `type:bug`, `priority:high`
- **Dependencies**: Task 2.1
- **Affected Files**: `Services/SaleService.cs`, `Controllers/PurchaseController.cs`
- **Description**: Address **GAP 2**. Replace hardcoded `TeamMemberId = 1` in `SaleService.ProcessSaleAsync` and `PurchaseController.Create` by resolving `int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier))` from the active `HttpContext`.
- **Acceptance Criteria**:
  - Sales records store the actual `TeamMemberId` of the cashier logged in.
  - Purchases store the actual `TeamMemberId` of the administrator who entered the invoice.
  - Receipts display the true cashier full name.

#### Task 6.2: Refactor Batch Creation into `StockService`
- **Assignee**: Dev B
- **Labels**: `area:inventory`, `type:refactor`, `priority:medium`
- **Dependencies**: Task 1.1
- **Affected Files**: `Services/StockService.cs`, `Controllers/PurchaseController.cs`, `Program.cs`
- **Description**: Address **GAP 6**. Create `StockService` to handle inventory operations. Extract the batch creation loop and stock balance raising logic out of `PurchaseController.Create` into `StockService.AddIncomingStockAsync`.
- **Acceptance Criteria**:
  - `StockService` is registered as a scoped service in `Program.cs`.
  - `PurchaseController` delegates batch creation to `StockService`.
  - Inward stock is added within the existing database transaction.

---

### Milestone 07: POS Sales Counter & History Scoping (Roadmap Step 7)

#### Task 7.1: Enforce Staff Sales History Scoping (Own Sales Only)
- **Assignee**: Dev A
- **Labels**: `area:sales`, `type:feature`, `priority:high`
- **Dependencies**: Task 6.1
- **Affected Files**: `Controllers/SaleController.cs`, `Views/Sale/History.cshtml`
- **Description**: Address **GAP 3**. In `SaleController.History`, check `User.IsInRole("Admin")`. If the user is `Staff`, scope the query strictly to `s.TeamMemberId == currentUserId`. Admin continues to see all store transactions.
- **Acceptance Criteria**:
  - Cashier logged in as Staff sees only receipts they personally dispensed.
  - Admin sees transactions from all team members with total revenue summary.

#### Task 7.2: Align Medicine Model Name Length
- **Assignee**: Dev A
- **Labels**: `area:catalog`, `type:bug`, `priority:low`
- **Dependencies**: Task 1.1
- **Affected Files**: `Models/Medicine.cs`
- **Description**: Update `[StringLength(100)]` on `Medicine.Name` to `[StringLength(150)]` to match the Technical Design specification and `MedicineViewModel.cs`.
- **Acceptance Criteria**:
  - `Medicine.Name` permits up to 150 characters without validation truncation.

---

### Milestone 09: Daily Sales Financial Reporting (Roadmap Step 9)

#### Task 9.1: Implement Daily Sales Financial Report Screen
- **Assignee**: Dev A
- **Labels**: `area:reports`, `type:feature`, `priority:high`
- **Dependencies**: Task 6.1, Task 7.1
- **Affected Files**: `Controllers/ReportController.cs`, `ViewModels/DailySalesReportViewModel.cs`, `Views/Report/DailySales.cshtml`, `Views/Shared/_Layout.cshtml`
- **Description**: Address **GAP 4**. Implement the missing Daily Sales report for administrators:
  - Add `DailySales(DateTime? date)` action in `ReportController` with `[Authorize(Roles = "Admin")]`.
  - Display today's gross revenue ($), total invoices, average invoice value, and a per-cashier revenue and invoice breakdown table.
  - Update `_Layout.cshtml` sidebar link from `/Report/Index` to `/Report/DailySales`.
- **Acceptance Criteria**:
  - Admin can select dates or view current day's totals.
  - Correctly aggregates `TotalAmount` and counts invoices per team member.
  - Staff role cannot access `/Report/DailySales`.

---

### Milestone 10: Database Backup & Activity Audit Trail (Roadmap Step 10)

#### Task 10.1: Build Database Backup Service & Controller
- **Assignee**: Dev B
- **Labels**: `area:backup`, `type:feature`, `priority:high`
- **Dependencies**: Task 2.1
- **Affected Files**: `Services/BackupService.cs`, `Controllers/BackupController.cs`, `Views/Backup/Index.cshtml`, `Program.cs`
- **Description**: Address **GAP 5**. Create `BackupService` to execute SQL Server `BACKUP DATABASE` to a dedicated local directory (e.g. `App_Data/Backups/` or user-selected folder). Build `BackupController` with `[Authorize(Roles = "Admin")]` and a Razor view providing a one-click backup button and history list.
- **Acceptance Criteria**:
  - Clicking "Backup Now" generates a valid timestamped `.bak` file on disk.
  - Successfully logged to `ActivityLog` (Action = "Backup").
  - Accessible strictly to Admin role.

#### Task 10.2: Build Activity Audit Log Viewer Screen
- **Assignee**: Dev B
- **Labels**: `area:audit`, `type:feature`, `priority:medium`
- **Dependencies**: Task 2.1
- **Affected Files**: `Controllers/ActivityLogController.cs`, `Views/ActivityLog/Index.cshtml`
- **Description**: Address **GAP 5**. Create `ActivityLogController` with `[Authorize(Roles = "Admin")]` and `Views/ActivityLog/Index.cshtml` displaying chronological system events (timestamp, user, action, details) with search/filter by action verb.
- **Acceptance Criteria**:
  - Admin can inspect the append-only audit trail.
  - Records cannot be modified or deleted via UI.
  - Staff access is blocked.

#### Task 10.3: Attach Audit Log Triggers to Master Data & Purchase Operations
- **Assignee**: Dev B
- **Labels**: `area:audit`, `type:feature`, `priority:medium`
- **Dependencies**: Task 10.2
- **Affected Files**: `Controllers/MedicineController.cs`, `Controllers/PurchaseController.cs`, `Controllers/TeamMemberController.cs`
- **Description**: Address **GAP 7**. Add `ActivityLog` entries for:
  - `MedicineController.Edit`: Log action `PriceChange` when `SellingPrice` is updated.
  - `PurchaseController.Create`: Log action `Purchase` with supplier name and total cost.
  - `TeamMemberController.ToggleActive`: Log action `DeactivateUser` / `ActivateUser`.
- **Acceptance Criteria**:
  - Modifying a medicine's price immediately records an audit log row showing old and new prices.
  - Recording a purchase writes an audit entry with invoice details.

---

### Milestone 11: UI Polish & Ergonomics (Roadmap Step 11)

#### Task 11.1: Counter Usability & Style Guide Compliance Audit
- **Assignee**: Dev B
- **Labels**: `area:ui`, `type:task`, `priority:low`
- **Dependencies**: None
- **Affected Files**: `wwwroot/css/site.css`, `Views/Sale/New.cshtml`, `Views/Shared/_Layout.cshtml`
- **Description**: Verify styling rules from `docs/06-screens.md`: calm green primary branding (`#2e7d32`), red for expired batches (`#dc3545`), orange for low stock/close expiry (`#fd7e14`), 16px base font size, and 28-32px bold numbers for totals.
- **Acceptance Criteria**:
  - All buttons, tables, and alerts follow the design tokens.
  - POS counter operates with complete keyboard flow without mouse intervention.

---

### Milestone 12: Standalone Offline Deployment (Roadmap Step 12)

#### Task 12.1: Self-Contained Deployment Configuration & Setup Script
- **Assignee**: Dev A
- **Labels**: `area:deployment`, `type:task`, `priority:medium`
- **Dependencies**: Milestone 01 through Milestone 11
- **Affected Files**: `PharmaDesk.csproj`, `docs/installation-guide.md`, `scripts/setup-db.ps1`
- **Description**: Configure self-contained .NET 9 publishing for `win-x64`. Prepare documentation and setup script for deploying on a clean Windows machine with SQL Server Express and no internet access.
- **Acceptance Criteria**:
  - `dotnet publish -c Release -r win-x64 --self-contained true` produces a standalone folder.
  - Application boots and operates offline with default seeded credentials.
