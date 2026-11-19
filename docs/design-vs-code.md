# Design vs. Code Audit & Gap Analysis

This document provides a comprehensive, field-by-field and feature-by-feature comparison between the **Technical Design Specification** (`Technical_Design.pdf` - Source of Truth), the **Full Design Plan** (`PharmaDesk_-_Full_Design.pdf`), and the **actual current C# / ASP.NET Core codebase**.

---

## 1. Executive Implementation Summary

| Area | Status | Implementation Level | Notes |
|---|---|---|---|
| **Data Models** | Substantially Implemented | 90% | All 10 domain entities exist in `PharmaDesk/Models/`. Some column length minor discrepancies and stray legacy files. |
| **Database Context & Relationships** | Fully Implemented | 95% | `PharmaDeskContext.cs` implements all indexes, `DeleteBehavior.Restrict`, and `Cascade`. |
| **Migrations & DB State** | Not Started | 0% | No EF Core migrations have been created or committed (`Migrations/` folder does not exist). |
| **PIN Auth & Lockout** | Substantially Implemented | 85% | Working login with `PasswordHasher`, 5-attempt counter, and 5-min lockout. Needs UTC consistency. |
| **Role-Based Authorization** | Partly Implemented | 40% | Dynamic menu works; but server-side `[Authorize(Roles = "Admin")]` is missing on multiple admin controllers! |
| **Categories Module** | Fully Implemented | 100% | CUD actions, unique check, restrict check. |
| **Medicines Module** | Substantially Implemented | 90% | Index, Create, Edit, ToggleActive. Name length is 100 in model vs 150 in design. No audit log on price change. |
| **Suppliers Module** | Substantially Implemented | 85% | Index, Create, Edit, Delete, ToggleActive. Inconsistent soft vs hard delete logic. |
| **Purchases Module** | Partly Implemented | 75% | Multi-item purchase UI with Select2 and transaction-based batch creation. Hardcoded `TeamMemberId = 1`. No service layer. No Details view. |
| **Sales & Cart (FEFO)** | Substantially Implemented | 85% | Cart JS with barcode auto-focus, FEFO multi-batch allocation in `SaleService`. Hardcoded `TeamMemberId = 1`. No multi-user separation in history. |
| **Receipt Generation** | Fully Implemented | 95% | Dedicated thermal slip view (`window.print()`). Uses `S-000001` format. |
| **Stock & Batch Inspection** | Fully Implemented | 95% | Stock list with non-expired aggregation, batch drill-down view (`Details.cshtml`). |
| **Reports Module** | Partly Implemented | 50% | `Expiry` and `LowStock` implemented. `DailySales` report is missing entirely. |
| **Team Member Management** | Substantially Implemented | 90% | Admin-only controller for adding staff, resetting PINs, and deactivating accounts. |
| **Database Backup** | Not Started | 0% | `BackupController` and `BackupService` do not exist. |
| **Activity Audit Log** | Partly Implemented | 30% | `ActivityLog` table exists; login and sales write to it. No UI view exists (`ActivityLogController` missing), and purchases/price changes are not logged. |
| **Offline Static Assets** | Fully Implemented | 100% | Bootstrap 5, Bootstrap Icons, jQuery, and Select2 are stored in `wwwroot/lib/`. Zero CDN calls. |

---

## 2. Entity & Data Dictionary Audit

### 2.1 `TeamMembers`
- **Design**: `Id (int, PK)`, `FullName (string 100)`, `PinHash (string 200)`, `Role (string 10: 'Admin' | 'Staff')`, `IsActive (bool, default true)`, `FailedPinCount (int, default 0)`, `LockedUntil (DateTime?, null)`, `CreatedAt (DateTime)`.
- **Code (`PharmaDesk/Models/TeamMember.cs`)**: Matches 100%. Implements helper methods `IsLockedOut()`, `LockAccount()`, and `ResetFailedAttempts()`.
- **Finding**: Matches design.

### 2.2 `Categories`
- **Design**: `Id (int, PK)`, `Name (string 60, unique)`.
- **Code (`PharmaDesk/Models/Category.cs`)**: Matches 100%. `PharmaDeskContext` configures unique index on `Name`.
- **Finding**: Matches design.

### 2.3 `Medicines`
- **Design**: `Id (int, PK)`, `Name (string 150)`, `GenericName (string 150, optional)`, `Barcode (string 50, unique)`, `CategoryId (int, FK)`, `Unit (string 20)`, `SellingPrice (decimal 10,2)`, `LowStockLevel (int, default 10)`, `IsActive (bool, default true)`.
- **Code (`PharmaDesk/Models/Medicines.cs` / class `Medicine`)**:
  - `Name` has `[StringLength(100)]` in the model entity, whereas the Technical Design specifies `string(150)` (though `MedicineViewModel.cs` specifies 150).
  - All other fields (`GenericName`, `Barcode`, `CategoryId`, `Unit`, `SellingPrice`, `LowStockLevel`, `IsActive`) match 100%.
  - `[NotMapped] TotalStock` correctly computes sum of unexpired batches.
- **Finding**: **Discrepancy in `Name` string length**: Model specifies max 100 chars, design specifies 150 chars.

### 2.4 `Batches`
- **Design**: `Id (int, PK)`, `MedicineId (int, FK)`, `BatchNumber (string 50)`, `ExpiryDate (DateOnly)`, `QuantityInStock (int)`, `CostPrice (decimal 10,2)`, `PurchaseItemId (int, FK?, optional)`.
- **Code (`PharmaDesk/Models/Batch.cs`)**: Matches 100%.
- **Finding**: Matches design. Note: ER diagram image omitted `PurchaseItemId` and `CostPrice`, but code correctly follows the Technical Design.

### 2.5 `Suppliers`
- **Design**: `Id (int, PK)`, `Name (string 100)`, `Phone (string 30, optional)`, `Address (string 200, optional)`, `Notes (string 300, optional)`, `IsActive (bool, default true)`.
- **Code (`PharmaDesk/Models/Supplier.cs`)**: Matches 100%.
- **Finding**: Matches design. Note: ER diagram image omitted `IsActive` and `Address`, but code correctly includes them.

### 2.6 `Purchases`
- **Design**: `Id (int, PK)`, `SupplierId (int, FK)`, `InvoiceNumber (string 50, optional)`, `PurchaseDate (DateTime)`, `TotalCost (decimal 12,2)`, `TeamMemberId (int, FK)`.
- **Code (`PharmaDesk/Models/Purchase.cs`)**: Matches 100%.
- **Finding**: Matches design. Note: ER diagram image omitted `InvoiceNumber`, but code includes it.

### 2.7 `PurchaseItems`
- **Design**: `Id (int, PK)`, `PurchaseId (int, FK)`, `MedicineId (int, FK)`, `BatchNumber (string 50)`, `ExpiryDate (DateOnly)`, `Quantity (int)`, `CostPrice (decimal 10,2 - price per ONE unit)`.
- **Code (`PharmaDesk/Models/PurchaseItem.cs`)**: Matches 100%.
- **Finding**: Matches design. Clarified: `CostPrice` is unit cost, not line total.

### 2.8 `Sales`
- **Design**: `Id (int, PK)`, `ReceiptNumber (string 20, unique)`, `SaleDate (DateTime)`, `TotalAmount (decimal 12,2)`, `PaymentMethod (string 20, "Cash")`, `TeamMemberId (int, FK)`.
- **Code (`PharmaDesk/Models/Sale.cs`)**: Matches 100%. Unique index configured on `ReceiptNumber`.
- **Finding**: Matches design. Note: ER diagram image omitted `ReceiptNumber` and `PaymentMethod`, but code includes them.

### 2.9 `SaleItems`
- **Design**: `Id (int, PK)`, `SaleId (int, FK)`, `MedicineId (int, FK)`, `BatchId (int, FK)`, `Quantity (int)`, `UnitPrice (decimal 10,2)`.
- **Code (`PharmaDesk/Models/SaleItem.cs`)**: Matches 100%.
- **Finding**: Matches design. Note: ER diagram image omitted `UnitPrice`, but code includes it as an immutable price snapshot.

### 2.10 `ActivityLog`
- **Design**: `Id (int, PK)`, `TeamMemberId (int, FK)`, `Action (string 50)`, `Details (string 300, optional)`, `CreatedAt (DateTime)`.
- **Code (`PharmaDesk/Models/ActivityLog.cs`)**: Matches 100%.
- **Finding**: Matches design. Note: The legacy plan named this column `Time`; the Technical Design and code use `CreatedAt`. The ER diagram image omitted this table entirely.

---

## 3. Detailed Architectural & Code Gaps

### GAP 1: Missing Server-Side Authorization on Sensitive Modules (SECURITY RISK)
- **Design Requirement**: Staff users must NEVER access Admin pages, and the server must block direct URL navigation to Admin actions.
- **Current Code**:
  - `TeamMemberController` has `[Authorize(Roles = "Admin")]`.
  - `HomeController` has `[Authorize]`.
  - `Program.cs` only has authentication middleware redirecting unauthenticated users to `/Account/Login`.
  - **Vulnerability**: `CategoryController`, `SupplierController`, `PurchaseController`, and `MedicineController` (Create, Edit, ToggleActive) **HAVE NO ROLE RESTRICTION**. Any logged-in Staff member can manipulate suppliers, purchase orders, categories, and medicine prices by navigating directly to those URLs.

### GAP 2: Hardcoded `TeamMemberId = 1` in Sales and Purchases
- **Design Requirement**: Transactions must record the actual logged-in user who processed them (`TeamMemberId` FK).
- **Current Code**:
  - `SaleService.cs` line 29: `TeamMemberId = 1, // مؤقتاً (لاحقاً: المستخدم الحالي)`
  - `PurchaseController.cs` line 105: `TeamMemberId = 1 // مؤقتاً (لاحقاً: المستخدم الحالي)`
- **Impact**: All sales and purchases in the database are permanently attributed to user ID 1 (Admin), breaking cashier accountability, daily sales breakdowns, and forensic audit logs.

### GAP 3: Sales History Does Not Filter for Staff Role
- **Design Requirement**: Staff can only view their own sales in Sales History; Admin can view all sales.
- **Current Code**: `SaleController.History` (lines 77-86) fetches all sales unconditionally:
  ```csharp
  var sales = await _context.Sales
      .Include(s => s.SaleItems).ThenInclude(si => si.Medicine)
      .OrderByDescending(s => s.SaleDate).ToListAsync();
  ```
- **Impact**: Staff can see pharmacy-wide revenue and sales executed by other team members.

### GAP 4: Missing Daily Sales Report Screen
- **Design Requirement**: Section 1 of Technical Design specifies a "Daily Sales Report" screen showing money totals, sales volume, and cashier breakdown (Admin only).
- **Current Code**: `ReportController.cs` only has `Expiry()` and `LowStock()`. The `DailySales` action and view do not exist. In `_Layout.cshtml`, the link points to `/Report/Index` which returns HTTP 404.

### GAP 5: Missing Backup & Activity Log Controllers/Views
- **Design Requirement**: Screen 13 (Backup) and Screen 14 (Activity Log) for Admin only.
- **Current Code**: `_Layout.cshtml` links to `asp-controller="Backup"` and `asp-controller="ActivityLog"`, but neither controller nor their corresponding views exist in the codebase. Clicking them produces HTTP 404.

### GAP 6: Incomplete Service Layer
- **Design Requirement**: Architecture specifies `StockService` (FEFO logic), `BackupService`, and `ReportService`.
- **Current Code**: Only `SaleService` exists. `PurchaseController` performs its own direct batch updates and transactions, and `ReportController` performs inline aggregation.

### GAP 7: Incomplete Activity Logging Triggers
- **Design Requirement**: Every sale, purchase, price change, login, and backup must be recorded in `ActivityLog`.
- **Current Code**:
  - `AccountController.Login`: Logged.
  - `SaleService.ProcessSaleAsync`: Logged.
  - `PurchaseController.Create`: **NOT LOGGED**.
  - `MedicineController.Edit` (Price Change): **NOT LOGGED**.
  - `TeamMemberController` (Create/Deactivate): **NOT LOGGED**.

### GAP 8: Missing Database Migrations
- **Design Requirement**: Entity Framework Core Code-First migrations tracking schema changes.
- **Current Code**: The project contains no `Migrations/` directory. If run against an empty SQL Server database, the application will crash with table missing exceptions unless `EnsureCreated()` or migrations are added.

### GAP 9: Dead, Duplicate, and Misnamed Files in Codebase
- **Dead 0-byte files in `PharmaDesk/Models/`**:
  - `Batches.cs` (0 bytes) vs `Batch.cs`
  - `Purchases.cs` (0 bytes) vs `Purchase.cs`
  - `PurchasItem .cs` (0 bytes, contains trailing space in filename!) vs `PurchaseItem.cs`
  - `Suppliers.cs` (0 bytes) vs `Supplier.cs`
- **Misnamed file in `PharmaDesk/Controllers/`**:
  - `'CategoryController .cs'` has a trailing space before the `.cs` extension.
- **Temporary Backup**:
  - `PharmaDesk/PharmaDesk.csproj.Backup.tmp` left in project root.

### GAP 10: Inconsistent DateTime Timezones
- **Design Requirement**: Explicit, uniform timestamp handling.
- **Current Code**: `TeamMember.CreatedAt` uses `DateTime.UtcNow`; `SaleService` uses `DateTime.Now`; `AccountController` lockout checks compare `DateTime.Now` against `LockedUntil`. Mixing UTC and local server time risks lockout calculation bugs and inaccurate audit logs during daylight savings transitions.

---

## 4. Resolution Plan & Action Items

All identified gaps will be tracked as explicit engineering issues in Phase 5 and resolved cleanly without modifying existing code during the documentation phase:
1. **Issue: Server-Side Authorization Hardening** (Add `[Authorize(Roles = "Admin")]` to controllers).
2. **Issue: Current User Context Resolution** (Replace hardcoded `TeamMemberId = 1`).
3. **Issue: Staff Sales History Filtering** (Filter sales by cashier claim for non-admins).
4. **Issue: Daily Sales Report Implementation** (`DailySales` action, ViewModel, and View).
5. **Issue: Backup & Activity Log UI Implementation** (Controllers, Services, Views).
6. **Issue: Architectural Refactoring to `StockService` and `ReportService`**.
7. **Issue: Audit Log Interception** (Log purchases, price updates, user updates).
8. **Issue: Initial Database Migration Creation** (`dotnet ef migrations add InitialCreate`).
9. **Issue: Cleanup of Duplicate/Malformed Source Files** (Safe file cleanup under refactoring guidelines).
10. **Issue: Timezone Standardization** (Standardize on UTC for timestamps and local date conversions).
