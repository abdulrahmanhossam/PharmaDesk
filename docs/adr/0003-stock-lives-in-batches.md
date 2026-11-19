# ADR 0003: Batch-Centric Inventory Architecture

## Status
Accepted

## Context
Pharmacies cannot treat inventory as a flat scalar quantity on a product table. Medicines from different shipments arrive with distinct lot numbers, separate expiration dates, and varying supplier acquisition costs. Dispensing expired drugs is illegal and dangerous, while selling newer stock while older stock expires on the shelf results in financial loss.

## Decision
We maintain **physical stock strictly at the `Batch` level**, not as a mutable counter column on `Medicines`:
1. **Batch Entity**: Each row in `Batches` represents a distinct delivery of a medicine, holding `BatchNumber`, `ExpiryDate`, `QuantityInStock`, `CostPrice`, and an optional foreign key `PurchaseItemId`.
2. **Total Stock Derivation**: The total sellable inventory for a medicine is dynamically computed as:
   $$\text{TotalStock} = \sum_{\substack{b \in \text{Batches} \\ b.\text{ExpiryDate} > \text{Today}}} b.\text{QuantityInStock}$$
   Expired batches ($b.\text{ExpiryDate} \le \text{Today}$) are excluded from available sellable stock.
3. **Immutability of Historical Cost**: When stock is purchased, its purchase cost price is tied to that specific batch. Subsequent purchases at different cost prices create new batches without mutating existing inventory costs.

### Codebase Alignment & Discrepancies
- In `PharmaDesk/Models/Medicine.cs`, `TotalStock` is correctly implemented as an unmapped computed property (`[NotMapped]`) summing active non-expired batches.
- In `PharmaDesk/Models/Batch.cs`, `QuantityInStock` is maintained as an integer.
- In `PharmaDesk/Controllers/StockController.cs` and `ReportController.cs`, queries filter `b.ExpiryDate > today` when aggregating current stock.
- The conceptual ER diagram omitted `Batches.PurchaseItemId` and `Batches.CostPrice`. The technical design data dictionary correctly establishes this relationship.

## Consequences
### Positive
- **Accurate Expiry Auditing**: Every unit of stock has a traceable expiration date.
- **Accurate Costing**: FIFO/FEFO profit margins can be calculated based on real batch purchase prices rather than averaged approximations.
- **Zero Drift**: Eliminates synchronization bugs where a summary quantity column on `Medicines` drifts out of sync with actual batch quantities.

### Negative / Risks
- Aggregate inventory queries require joining and summing across `Batches`. Adding an index on `Batches(MedicineId, ExpiryDate)` and `Batches(ExpiryDate)` mitigates query performance overhead.
