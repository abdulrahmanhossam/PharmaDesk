# ADR 0007: Role-Based Authorization and Server-Side Protection

## Status
Accepted

## Context
Earlier conceptual drafts of PharmaDesk (in `PharmaDesk_-_Full_Design.pdf`) proposed a "Sections" entity, a "Team and Sections" screen, and vague "Management only" access controls. In retail community pharmacies, this introduced unnecessary complexity. Furthermore, client-side menu hiding alone does not prevent unauthorized users from accessing sensitive URL endpoints directly.

## Decision
1. **Strict 2-Role Model**:
   - The system supports exactly two roles: **`Admin`** and **`Staff`**, stored as `TeamMembers.Role`.
   - The legacy concept of "Sections" is completely removed.
   - Typographical references such as "OnlyAdmin" in early specification drafts are unified canonically to **`Admin`**.
2. **Permission Boundary**:
   - **Staff**: Allowed access only to counter operations: Dashboard (counts only, no money totals), New Sale, Sales History (own sales only), Medicines (view and search only; cannot add/edit/delete/price-change), Stock (read-only view), Expiry Report, and Low Stock Report.
   - **Admin**: Full access to all screens, including Supplier management, Purchases, Daily Sales Report with financial metrics, Team Member management, Database Backups, and Activity Audit Logs.
3. **Defense in Depth (Menu + Server-Side Enforcement)**:
   - Dynamic UI: `_Layout.cshtml` renders menu links conditionally using `@if (User.IsInRole("Admin"))`.
   - Server-Side Gate: Every Admin controller or action must be decorated with `[Authorize(Roles = "Admin")]`. Staff attempts to access Admin endpoints return HTTP 403 Forbidden / redirect to `/Account/AccessDenied`.

### Codebase Alignment & Discrepancies
- In `_Layout.cshtml`, the sidebar correctly wraps Admin links in `@if (User.IsInRole("Admin"))`.
- In `TeamMemberController.cs`, `[Authorize(Roles = "Admin")]` is correctly present.
- **Critical Gap Identified**: `CategoryController`, `MedicineController` (modifying actions), `SupplierController`, and `PurchaseController` currently lack `[Authorize(Roles = "Admin")]`! A Staff user can navigate directly to `/Purchase/Create`, `/Supplier/Create`, or `/Category/Create` by typing the URL.
- **Proposed Remedy**: Add explicit role attributes (`[Authorize(Roles = "Admin")]`) to `SupplierController`, `PurchaseController`, and to the CUD actions of `MedicineController` and `CategoryController`.

## Consequences
### Positive
- Clear, uncompromised security boundary preventing staff from tampering with prices, viewing pharmacy margins, creating false invoices, or modifying team accounts.
- Clean mental model with zero administrative overhead (no permissions matrices or section configurations to manage).

### Negative / Risks
- Two roles mean all staff share the same operational tier; granular sub-permissions (e.g. senior cashier vs junior clerk) are not supported in version 1.
