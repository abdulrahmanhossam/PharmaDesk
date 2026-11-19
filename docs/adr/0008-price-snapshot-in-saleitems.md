# ADR 0008: Immutable Price Snapshots in SaleItems

## Status
Accepted

## Context
Medicine catalog selling prices change frequently due to pharmaceutical inflation, supplier revisions, and regulatory adjustments. If historical sales items dynamically reference `Medicine.SellingPrice`, any subsequent price edit retroactively mutates past financial reports, closed sales receipts, and tax statements.

## Decision
We enforce an **immutable price snapshot** on transaction line items:
1. `SaleItems` contains a dedicated column: `UnitPrice decimal(10,2)` (along with `Quantity int`, `MedicineId int`, and `BatchId int`).
2. At the instant a sale is executed, the current `Medicine.SellingPrice` is read and copied into `SaleItems.UnitPrice`.
3. Historical financial reporting and receipt re-printing calculate line totals strictly as:
   $$\text{LineTotal} = \text{SaleItem}.\text{Quantity} \times \text{SaleItem}.\text{UnitPrice}$$
4. Subsequent updates to `Medicine.SellingPrice` in the catalog affect future sales only. Past sales records remain permanently unchanged.

### Codebase Alignment & Discrepancies
- In `PharmaDesk/Models/SaleItem.cs`, `public decimal UnitPrice { get; set; }` is properly declared.
- In `PharmaDesk/Services/SaleService.cs` (line 77), `UnitPrice = medicine.SellingPrice` is correctly captured during sale processing.
- The conceptual ER diagram omitted `UnitPrice` on `SaleItems`, whereas the Technical Design data dictionary explicitly specified it. The codebase correctly conforms to the Technical Design.

## Consequences
### Positive
- Financial history is tamper-proof and immutable.
- Receipts reprinted months later match the original physical slip handed to the customer.
- Allows retrospective margin analysis comparing `SaleItem.UnitPrice` against `Batch.CostPrice`.

### Negative / Risks
- Requires maintaining the duplicate price field in `SaleItems` rather than normalizing away into `Medicines`.
