# ADR 0002: Service Layer for Inventory and Core Business Logic

## Status
Accepted

## Context
Retail pharmacy operations involve non-trivial business logic, specifically:
- FEFO (First-Expired, First-Out) stock allocation across multiple physical batches.
- Multi-batch split deductions when a single medicine quantity spans several deliveries.
- Database backup orchestration via SQL Server administrative commands.
- Aggregate reporting across sales, batches, margins, and stock thresholds.

Allowing MVC controllers to directly execute multi-step database mutations leads to duplicated business rules, transactional leakage, difficult automated testing, and bloated controllers.

## Decision
We enforce a **dedicated Service Layer** between MVC Controllers and the EF Core persistence layer:
1. **`StockService`**: Houses batch allocation logic, FEFO order calculation, stock deduction, batch stock restoration on void/return, and low-stock evaluation.
2. **`SaleService`**: Orchestrates sale transaction processing, invoking `StockService`, creating `Sale` and `SaleItem` records, recording immutable price snapshots, and writing the audit log.
3. **`BackupService`**: Encapsulates SQL Server `BACKUP DATABASE` execution to configured file paths and external drives.
4. **`ReportService`**: Computes daily sales summaries, expiry projections (red/orange statuses), and staff sales performance.

### Codebase Alignment & Discrepancies
- Currently, `SaleService` exists in `PharmaDesk/Services/SaleService.cs` and implements FEFO batch allocation directly within `ProcessSaleAsync`.
- `StockService`, `BackupService`, and `ReportService` are not yet created. Currently, `PurchaseController` performs batch creation and stock incrementation directly in its POST action, and `ReportController` executes inline LINQ queries against `PharmaDeskContext`.
- **Proposed Remedy**: Extract inventory logic into `StockService`, and create `ReportService` and `BackupService` to encapsulate respective domain logic, resolving controller fatness.

## Consequences
### Positive
- **Single Source of Truth**: Inventory allocation and FEFO calculation live in one place, preventing discrepancies between sales, stock viewing, and reporting.
- **Testability**: Services can be unit and integration tested without HTTP context or Razor view pipelines.
- **Lean Controllers**: Controllers focus strictly on HTTP validation, model binding, and view presentation.

### Negative / Risks
- Introduces an extra layer of abstraction and dependency injection registration in `Program.cs`.
