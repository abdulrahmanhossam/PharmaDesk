# ADR 0005: Soft Delete Strategy and Referential Integrity Behaviors

## Status
Accepted

## Context
In a pharmacy management system, historical sales receipts, financial summaries, and inventory audits must remain permanently accurate. If a catalog medicine, supplier, or staff member is hard-deleted (`DELETE FROM ...`), historical sales, purchase invoices, and audit trails lose referential integrity or cascade-delete critical financial history.

Conversely, line items belonging to a parent document (e.g., `SaleItems` within a `Sale`, or `PurchaseItems` within a `Purchase`) have no independent existence outside their header.

## Decision
We enforce a **dual referential integrity strategy**:

1. **Soft Delete (`IsActive = false`) for Master Entities**:
   - `Medicines`, `TeamMembers`, and `Suppliers` include a required `bool IsActive` column (default `true`).
   - "Deleting" a master entity sets `IsActive = false`. Inactive entities are hidden from active operational dropdowns, search auto-complete, and login selectors, but remain intact in the database so foreign keys on historical records remain valid.

2. **EF Core `DeleteBehavior.Restrict` on Protected Relationships**:
   - `Category` → `Medicines` (`DeleteBehavior.Restrict`): Cannot delete a category while medicines exist under it.
   - `Medicine` → `Batches` (`DeleteBehavior.Restrict`): Cannot delete a medicine with inventory batches.
   - `Medicine` → `SaleItems` / `PurchaseItems` (`DeleteBehavior.Restrict`): Historical lines are permanently protected.
   - `Supplier` → `Purchases` (`DeleteBehavior.Restrict`): Cannot delete a supplier who has purchase history.
   - `TeamMember` → `Sales` / `Purchases` / `ActivityLog` (`DeleteBehavior.Restrict`): Cannot delete staff who performed actions.

3. **EF Core `DeleteBehavior.Cascade` for Document Child Tables Only**:
   - `Sale` → `SaleItems` (`DeleteBehavior.Cascade`): Deleting a sale deletes its lines.
   - `Purchase` → `PurchaseItems` (`DeleteBehavior.Cascade`): Deleting a purchase header deletes its lines.

4. **EF Core `DeleteBehavior.SetNull` for Batch-Purchase Lineage**:
   - `PurchaseItem` → `Batches` (`DeleteBehavior.SetNull`): A batch references the `PurchaseItemId` that created it. If a purchase record is removed or adjusted, the batch remains preserved with a null reference to maintain stock accuracy.

### Codebase Alignment & Discrepancies
- `PharmaDesk/Data/PharmaDeskContext.cs` in `OnModelCreating` implements these exact behaviors.
- In `PharmaDesk/Controllers/SupplierController.cs` (lines 103-116), `DeleteConfirmed` contains custom branching: if purchases exist, it sets `IsActive = false`; if no purchases exist, it performs a hard delete (`_context.Suppliers.Remove`). For consistency across master entities, soft deletion via a uniform `ToggleActive` endpoint should be the primary pattern.

## Consequences
### Positive
- Absolute preservation of financial, dispensing, and tax audit records.
- Prevents database runtime exceptions caused by orphaned foreign key constraints.
- Clean user experience: inactive medicines or staff no longer clutter counter sales screens.

### Negative / Risks
- Queries for active items must include `.Where(x => x.IsActive)` filters.
- Unique constraints (such as `Medicines.Barcode`) must be handled carefully when inactive items retain their codes.
