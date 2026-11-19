# 09. Project Roadmap & Implementation Plan

This roadmap updates and formalizes the original **12-Step Build Order** from `PharmaDesk_-_Full_Design.pdf`, reconciled against the canonical **Technical Design** and the current state of the codebase. Each step defines an explicit **Goal**, actionable **Tasks**, **Acceptance Criteria**, and current **Implementation Status** verified directly against the code.

---

## Roadmap Overview & Milestone Status

```
Step 01: Foundation & Database Tables ────────► [IN PROGRESS] (Models complete, Migrations pending)
Step 02: Authentication, Lockout & Roles ────► [IN PROGRESS] (Auth/Lockout working, Server gates pending)
Step 03: Layout, Navigation & Menu ──────────► [DONE]
Step 04: Categories & Medicines Catalog ─────► [DONE]
Step 05: Suppliers Directory ────────────────► [DONE]
Step 06: Purchases & Inward Stock ───────────► [IN PROGRESS] (UI & DB working, Service extraction pending)
Step 07: POS Sales Counter & Receipts ───────► [IN PROGRESS] (Cart & FEFO working, User scoping pending)
Step 08: Stock View & Expiry/Low Stock ──────► [DONE]
Step 09: Daily Sales Financial Report ───────► [NOT STARTED]
Step 10: Database Backup & Activity Audit ───► [IN PROGRESS] (Audit entity exists, UI & Backup pending)
Step 11: Design Polish & Usability ──────────► [IN PROGRESS] (Styles in place, final audit pending)
Step 12: Self-Contained Offline Publish ─────► [NOT STARTED]
```

---

## Detailed Step-by-Step Breakdown

### Step 01: Foundation & Database Tables
- **Goal**: Create ASP.NET Core MVC project on .NET 9 with Entity Framework Core, SQL Server connectivity, Code-First domain models, and initial database migrations.
- **Tasks**:
  1. Configure `PharmaDesk.csproj` with EF Core SQL Server and Design tooling.
  2. Implement all 10 domain entity models in `PharmaDesk/Models/` matching the Technical Design Data Dictionary.
  3. Configure entity relationships, composite/unique indexes, and foreign key delete behaviors in `PharmaDeskContext.cs`.
  4. Generate and commit the initial EF Core migration (`dotnet ef migrations add InitialCreate`).
  5. Clean up redundant zero-byte model files (`Batches.cs`, `Purchases.cs`, `Suppliers.cs`, `'PurchasItem .cs'`).
- **Acceptance Criteria**:
  - `dotnet build` succeeds with 0 errors.
  - Applying migrations (`dotnet ef database update`) creates all 10 tables, indexes, and foreign keys cleanly.
  - Delete behaviors strictly follow ADR 0005 (`Restrict` for master entities, `Cascade` for line items).
- **Status**: **In Progress** (Models, relationships, and indexes are implemented; migrations and file cleanup remain).

---

### Step 02: Authentication, Lockout & Role Security
- **Goal**: Implement PIN-based operator login with cryptographic hashing, 5-try lockout protection, session cookies, and administrative seed data.
- **Tasks**:
  1. Build `AccountController` with `Login` and `Logout` actions.
  2. Implement PIN verification using `PasswordHasher<TeamMember>`.
  3. Implement 5-failed-attempts tracking with a 5-minute cooldown (`LockedUntil`).
  4. Create `DbSeeder.cs` to seed default administrator (`Admin`, PIN `1234`).
  5. Apply `[Authorize(Roles = "Admin")]` to all administrative controllers and actions (ADR 0007).
  6. Standardize lockout timestamps on UTC (`DateTime.UtcNow`).
- **Acceptance Criteria**:
  - Logging in with invalid PIN increments `FailedPinCount`.
  - 5th failed attempt locks the account for 5 minutes, displaying remaining lockout minutes.
  - Entering correct PIN signs in the user, clears failed attempts, and sets authentication cookie with `ClaimTypes.Role`.
  - Staff users navigating directly to administrative URLs receive HTTP 403 / Access Denied.
- **Status**: **In Progress** (Login, hashing, and lockout implemented; server-side role attributes and UTC refactoring pending).

---

### Step 03: Layout, Navigation Shell & Dynamic Menu
- **Goal**: Build the responsive desktop shell with local Bootstrap assets, side navigation menu, and role-based link visibility.
- **Tasks**:
  1. Package Bootstrap 5, Bootstrap Icons, and jQuery locally into `wwwroot/lib/`.
  2. Construct `_Layout.cshtml` with fixed 240px sidebar, calm green brand header, and top greeting bar.
  3. Conditionally render administrative links (`Suppliers`, `Purchases`, `Daily Sales`, `Team Members`, `Backup`, `Activity Log`) only when `User.IsInRole("Admin")`.
  4. Add active link CSS highlighting based on the current controller/action.
- **Acceptance Criteria**:
  - Application renders with full visual fidelity with the network disconnected.
  - Staff users do not see administrative menu items in the sidebar.
  - Top bar displays current user name and provides working Logout button.
- **Status**: **Done** (Implemented in `Views/Shared/_Layout.cshtml`).

---

### Step 04: Categories & Medicines Catalog Management
- **Goal**: Enable full product catalog management with barcode uniqueness, categories classification, and soft-delete capabilities.
- **Tasks**:
  1. Build `CategoryController` and views for Category CRUD with duplicate name prevention.
  2. Build `MedicineController` and views (`Index`, `Create`, `Edit`).
  3. Implement barcode uniqueness check and search auto-filtering (by barcode, brand name, generic name).
  4. Implement `ToggleActive` soft delete action for medicines.
  5. Add audit log trigger on `SellingPrice` modification (`ActivityLog.PriceChange`).
- **Acceptance Criteria**:
  - Adding a duplicate barcode is prevented with an actionable validation error.
  - Categories containing medicines cannot be hard-deleted (enforced by `DeleteBehavior.Restrict`).
  - Medicines with sales history cannot be hard-deleted; `ToggleActive` hides them from counter sales.
- **Status**: **Done** (CRUD and views complete; price change audit logging to be attached in Step 10).

---

### Step 05: Suppliers Directory
- **Goal**: Manage pharmaceutical vendors, distributors, contact details, and inward stock history.
- **Tasks**:
  1. Build `SupplierController` and views (`Index`, `Create`, `Edit`, `Delete`).
  2. Implement search filtering by supplier name and phone number.
  3. Implement soft-delete protection: suppliers with purchase invoices cannot be hard-deleted.
  4. Restrict controller access to `[Authorize(Roles = "Admin")]`.
- **Acceptance Criteria**:
  - Only administrators can view or edit suppliers.
  - Deleting a supplier with existing purchases toggles `IsActive = false` rather than throwing foreign key errors.
- **Status**: **Done** (CRUD and views complete; server-side authorization attribute to be attached).

---

### Step 06: Purchases & Inward Stock Management
- **Goal**: Record incoming supplier shipments, automatically generating new inventory batches and raising available stock.
- **Tasks**:
  1. Build `PurchaseController` and multi-item dynamic receiving form (`Views/Purchase/Create.cshtml`).
  2. Implement interactive Select2 search for suppliers and medicines.
  3. Validate future expiry date on each incoming batch ($\text{ExpiryDate} > \text{Today}$).
  4. Save purchase header, purchase items, and new batches within an atomic EF Core transaction.
  5. Associate transaction with the logged-in user rather than hardcoded ID 1.
  6. Extract purchase transaction logic into `StockService` / `PurchaseService`.
- **Acceptance Criteria**:
  - Saving a purchase atomically creates rows in `Purchases`, `PurchaseItems`, and `Batches`.
  - Available stock for the medicine immediately reflects the newly added batch quantity.
  - Transaction rolls back completely if any line fails validation.
- **Status**: **In Progress** (Multi-item form and transaction working; user context and service extraction pending).

---

### Step 07: POS Sales Counter & Thermal Receipts
- **Goal**: Build a keyboard-driven counter sales interface with barcode scanner auto-focus, FEFO multi-batch stock deduction, and thermal receipt printing.
- **Tasks**:
  1. Build `/Sale/New` screen with autofocus input `#barcodeInput`.
  2. Implement client-side cart logic in `sale.js` with instant quantity adjustment and modal search fallback.
  3. Implement `SaleService.ProcessSaleAsync` executing the 4 Core Invariants (ADR 0004: FEFO allocation, no overselling, no expired stock, atomic transaction).
  4. Generate sequential receipt numbers (`S-######`).
  5. Snapshot `UnitPrice` into `SaleItems` at sale moment.
  6. Build `Views/Sale/Receipt.cshtml` monospaced thermal slip with `window.print()` auto-trigger.
  7. Scope `SaleController.History`: Admin sees all sales; Staff sees only their own.
- **Acceptance Criteria**:
  - Scanning a barcode adds item to cart and retains cursor focus without mouse intervention.
  - Cashier cannot sell more units than are unexpired and in stock.
  - Sale automatically splits across batches when quantity exceeds earliest batch.
  - Receipt prints correctly on 80mm/58mm rolls and displays accurate cashier name.
- **Status**: **In Progress** (POS counter, FEFO deduction, and receipt view complete; user scoping and cashier ID resolution pending).

---

### Step 08: Stock Inventory & Expiry/Low Stock Reports
- **Goal**: Provide real-time inventory visibility, batch lot drill-down, and early warning reports for expiring and low-stock items.
- **Tasks**:
  1. Build `StockController` displaying consolidated unexpired stock per medicine.
  2. Build `StockController.Details` drill-down displaying all batches (including expired in red).
  3. Build `ReportController.Expiry` listing batches expiring within 90 days (orange) and expired (red).
  4. Build `ReportController.LowStock` listing medicines where unexpired stock $\le \text{LowStockLevel}$.
- **Acceptance Criteria**:
  - Consolidated stock correctly equals $\sum \text{QuantityInStock}$ of unexpired batches only.
  - Batches expiring within 90 days highlight in orange; expired batches highlight in red.
  - Staff and Admin can view inventory without altering stock directly.
- **Status**: **Done** (Implemented in `StockController` and `ReportController`).

---

### Step 09: Daily Sales Financial Report
- **Goal**: Provide pharmacy owners with an end-of-day financial reconciliation report breaking down revenue, transaction counts, and cashier contributions.
- **Tasks**:
  1. Add `DailySales` action to `ReportController` with `[Authorize(Roles = "Admin")]`.
  2. Build `DailySalesViewModel` holding date filters, gross revenue ($), total invoices, average ticket size, and a per-cashier summary table.
  3. Build `Views/Report/DailySales.cshtml` styled with summary cards and cashier performance tables.
  4. Update sidebar link in `_Layout.cshtml` to point to `/Report/DailySales`.
- **Acceptance Criteria**:
  - Admin can select a specific date or view today's sales.
  - Displays total revenue, invoice count, and per-staff revenue breakdown accurately.
  - Inaccessible to Staff role (redirects to AccessDenied).
- **Status**: **Not Started** (Tracked as Phase 5 Issue).

---

### Step 10: Database Backup & Activity Audit Log
- **Goal**: Enable one-click SQL Server database backups and provide a searchable forensic audit log of system events.
- **Tasks**:
  1. Build `BackupService` executing SQL Server `BACKUP DATABASE` to a configured local folder or USB path.
  2. Build `BackupController` and `Views/Backup/Index.cshtml` with backup trigger button and history table.
  3. Build `ActivityLogController` and `Views/ActivityLog/Index.cshtml` displaying chronological system events.
  4. Attach audit logging calls to: `AccountController.Login`, `SaleService.ProcessSaleAsync`, `PurchaseController.Create`, `MedicineController.Edit` (price modifications), and `BackupController`.
- **Acceptance Criteria**:
  - Clicking "Backup Database" produces a valid `.bak` file on disk with timestamped filename.
  - Activity log displays user, timestamp, action verb, and descriptive details.
  - Both screens are strictly restricted to Admin role.
- **Status**: **In Progress** (`ActivityLog` model and login/sale logging exist; UI screens and backup service pending).

---

### Step 11: UI/UX Polish & Counter Usability
- **Goal**: Refine typography, contrast, spacing, button dimensions, and keyboard shortcuts across all screens for optimal dispensing counter speed.
- **Tasks**:
  1. Verify all font sizes conform to 16px base and 28px-32px for numerical totals.
  2. Ensure color palette conforms to calm green (`#2e7d32`), danger red (`#dc3545`), and alert orange (`#fd7e14`).
  3. Add confirmation modals on critical destructive or irreversible actions.
  4. Audit mobile/tablet responsiveness for portable inventory tablets on LAN.
- **Acceptance Criteria**:
  - Consistent layout and visual hierarchy across all 22 screens.
  - Zero broken images, misaligned buttons, or missing icons.
- **Status**: **In Progress** (Core styles implemented; final accessibility and polish review pending).

---

### Step 12: Self-Contained Offline Publish & Installation
- **Goal**: Create a standalone, self-contained deployment package that installs and runs on a clean Windows PC without internet access.
- **Tasks**:
  1. Configure self-contained .NET 9 publish profile (`dotnet publish -c Release -r win-x64 --self-contained true`).
  2. Create automated setup script / documentation for SQL Server Express installation and initial database creation.
  3. Test full offline execution: verify all views, scripts, icons, and database operations function with network disabled.
  4. Package installation guide in `docs/installation-guide.md`.
- **Acceptance Criteria**:
  - Application launches and operates flawlessly on a Windows PC disconnected from the internet.
  - Default Admin account seeds automatically on first launch.
- **Status**: **Not Started** (Final deployment milestone).
