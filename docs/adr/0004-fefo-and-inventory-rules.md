# ADR 0004: FEFO Allocation and the Four Core Inventory Invariants

## Status
Accepted

## Context
Patient safety and pharmacy profitability depend on rigorous stock dispatching rules. If dispensing is left to cashier discretion, older stock is frequently neglected at the back of the shelf until expired. Furthermore, system errors or concurrent requests must never allow negative inventory or orphaned transaction records.

## Decision
We establish **FEFO (First-Expired, First-Out)** as the foundational dispatching algorithm and enforce **Four Non-Negotiable Business Rules** in application code:

1. **Rule 1 (No Overselling)**: A sale line cannot take more quantity than the physical stock available in valid batches. If requested quantity exceeds total active stock, the sale is rejected with an explicit error.
2. **Rule 2 (No Expired Stock)**: A sale never allocates units from an expired batch ($\text{ExpiryDate} \le \text{Today}$). Expired stock is sequestered and cannot be added to a cart or checked out.
3. **Rule 3 (FEFO Priority)**: Stock is strictly deducted from available batches ordered by `ExpiryDate ASC`. If a sale item quantity exceeds the earliest batch's available stock, the system automatically splits the deduction across subsequent batches, creating one `SaleItem` row per allocated batch.
4. **Rule 4 (Atomic Transactions)**: Saving a sale or a purchase occurs within a single ACID database transaction (`IDbContextTransaction`). If any item validation, batch update, or audit log write fails, the entire transaction rolls back completely. A partial or half-saved sale is impossible.

### Codebase Alignment & Discrepancies
- `SaleService.ProcessSaleAsync` in `PharmaDesk/Services/SaleService.cs` correctly implements all 4 rules:
  - Opens `_context.Database.BeginTransactionAsync()`.
  - Filters `b.ExpiryDate > today && b.QuantityInStock > 0` ordered by `b.ExpiryDate`.
  - Checks `totalAvailable < quantityNeeded` before mutating.
  - Splits across batches and generates individual `SaleItem` records.
  - Commits transaction only after all lines and `ActivityLog` are saved.
- **Gap Identified**: `PurchaseController.Create` opens a transaction and creates `Purchase`, `PurchaseItem`, and `Batch`, but this logic is located in the controller rather than a service. Additionally, `SaleService` currently hardcodes `TeamMemberId = 1` rather than resolving the active authenticated user from `HttpContext.User`.

## Consequences
### Positive
- Strict compliance with pharmaceutical dispensing standards.
- Automatic inventory rotation without manual cashier calculations.
- Guaranteed data consistency across database failures or power interruptions.

### Negative / Risks
- Single items in a cart may produce multiple `SaleItem` rows in receipts and database tables when stock spans multiple batches. The receipt layout and sales history view must format these clearly for the customer.
