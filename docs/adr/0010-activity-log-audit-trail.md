# ADR 0010: Comprehensive Activity Audit Logging

## Status
Accepted

## Context
Community pharmacies manage controlled inventory, cash intake, and confidential pricing. Pharmacy owners require complete accountability over operational events to detect shrinkage, investigate discrepancy in drawer cash, identify who authorized price changes, and verify when database backups were performed.

## Decision
We implement a persistent, append-only **Activity Log**:
1. **`ActivityLog` Entity**:
   - `Id int, PK`
   - `TeamMemberId int, FK` (references `TeamMembers`, `DeleteBehavior.Restrict`)
   - `Action string(50)` (standardized action verbs: `Login`, `Sale`, `Purchase`, `PriceChange`, `Backup`, `DeactivateUser`)
   - `Details string(300)` (human-readable metadata, e.g., "Receipt S-000125, Total: $45.00" or "Medicine Panadol price changed from 20.00 to 25.00")
   - `CreatedAt DateTime` (UTC timestamp)
2. **Mandatory Logging Points**:
   - Every user login (successful).
   - Every sale transaction.
   - Every supplier purchase recorded.
   - Every medicine price modification.
   - Every database backup initiated.
   - Every team member creation or deactivation.
3. **Access Control**:
   - The Activity Log screen (`/ActivityLog`) is strictly accessible to **Admin only**.
   - Activity log records are append-only; no UI or controller action shall support editing or deleting audit rows.

### Codebase Alignment & Discrepancies
- `PharmaDesk/Models/ActivityLog.cs` is implemented with the required columns.
- `PharmaDeskContext.cs` maps `DbSet<ActivityLog> ActivityLogs`.
- `AccountController.cs` logs `Login`.
- `SaleService.cs` logs `Sale`.
- **Gaps Identified**:
  - `ActivityLogController` does not exist yet.
  - `MedicineController.Edit` does not log price changes to `ActivityLog`.
  - `PurchaseController.Create` does not write an `ActivityLog` record.
  - `TeamMemberController` does not log user creation or status toggles.
- **Proposed Remedy**: Create `ActivityLogController` and View for Admin viewing, and inject audit logging into all master data mutations.

## Consequences
### Positive
- Comprehensive forensic audit trail for the pharmacy owner.
- Strong deterrent against employee theft, unauthorized price manipulations, and inventory discrepancies.

### Negative / Risks
- The `ActivityLogs` table grows continuously over time. In future versions, an archival or purge policy for records older than 2 years may be introduced.
