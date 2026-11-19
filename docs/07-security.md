# 07. Security Architecture & Threat Mitigation

## 1. Threat Profile for Local Pharmacy POS

Unlike distributed SaaS applications, PharmaDesk operates as a local web application running within a physical pharmacy. Its primary security threats consist of:
1. **Unauthorized Staff Escalation**: A cashier attempting to modify catalog prices, delete historical records, view gross revenue margins, or create fictitious supplier purchase invoices.
2. **PIN Brute-Forcing**: An unauthorized employee or visitor guessing short numeric PINs on unattended counter terminals.
3. **Audit Trail Tampering**: Erasing or altering transaction logs to conceal inventory shrinkage or cash drawer shortages.
4. **Credential Exposure**: Exposing plain text PINs or administrative passwords in databases, configuration files, or version control.

---

## 2. Authentication & Credential Storage

```
                           AUTHENTICATION WORKFLOW
┌──────────────┐     Select User & Enter PIN     ┌────────────────────────┐
│ Cashier/User │ ──────────────────────────────► │ AccountController.POST │
└──────────────┘                                 └───────────┬────────────┘
                                                             │
                                        Is LockedOut? (DateTime < LockedUntil)
                                             ┌───────────────┴───────────────┐
                                      YES    │                               │ NO
                                      ┌──────▼──────┐                 ┌──────▼──────┐
                                      │ Reject 400  │                 │ Verify PIN  │
                                      │ "Try in X m"│                 │ via Hasher  │
                                      └─────────────┘                 └──────┬──────┘
                                                                             │
                                                                   Verification Result
                                                      ┌──────────────────────┴──────────────────────┐
                                                FAILED│                                             │ SUCCESS
                                       ┌──────────────▼──────────────┐               ┌──────────────▼──────────────┐
                                       │ FailedPinCount++            │               │ FailedPinCount = 0          │
                                       │ If >= 5: Lock for 5 minutes │               │ LockedUntil = null          │
                                       │ Write DB Changes            │               │ Log Activity: "Login"       │
                                       └─────────────────────────────┘               │ Issue Session Cookie        │
                                                                                     └─────────────────────────────┘
```

### 2.1 Cryptographic PIN Hashing
- **Zero Plain-Text Storage**: PINs are never stored in plain text, logged to console, or rendered back to HTML views.
- **Algorithm**: Hashing is executed via ASP.NET Core Identity's `PasswordHasher<TeamMember>`, implementing PBKDF2 with HMAC-SHA256, 128-bit salt generation per user, and 100,000 hash iterations. The output hash string (storing version, salt, and hash) is preserved in `TeamMembers.PinHash NVARCHAR(200)`.
- **PIN Length**: Staff PINs require a minimum length of 4 digits and up to 10 characters (`[StringLength(10, MinimumLength = 4)]`).

### 2.2 Rate Limiting & 5-Attempt Lockout Mechanism
To prevent automated or manual brute-force enumeration of 4-digit PINs:
1. `TeamMembers.FailedPinCount` (int, default 0) tracks consecutive invalid login attempts.
2. If `FailedPinCount < 5`: Each incorrect attempt increments the counter and returns a friendly warning stating remaining attempts (`"Wrong PIN. {5 - FailedPinCount} attempts remaining."`).
3. If `FailedPinCount >= 5`:
   - `LockedUntil` is stamped with `DateTime.UtcNow.AddMinutes(5)`.
   - `FailedPinCount` is reset to 0.
   - The user receives an error: `"Too many wrong attempts. Account locked for 5 minutes."`.
4. While locked (`DateTime.UtcNow < LockedUntil`): Any authentication attempt is rejected before hashing, displaying remaining lockout minutes.
5. Upon successful PIN validation:
   - `FailedPinCount` is cleared to `0`.
   - `LockedUntil` is cleared to `null`.
   - A success event is recorded in `ActivityLog`.
   - A persistent, encrypted session cookie (`CookieAuthenticationDefaults.AuthenticationScheme`) is issued.

---

## 3. Server-Side Role-Based Access Control (RBAC)

Presentation filtering (omitting links in `_Layout.cshtml`) is only visual. Full security requires **strict server-side authorization enforcement**.

### 3.1 Defense-in-Depth Enforcement
- **Authentication Middleware**: In `Program.cs`, all requests outside `/Account/Login`, `/Account/AccessDenied`, and static assets (`/css`, `/js`, `/lib`) require an authenticated cookie identity.
- **Controller Attribute Gates**: Every controller and action reserved for administrators must be explicitly decorated:
  ```csharp
  [Authorize(Roles = "Admin")]
  public class TeamMemberController : Controller { ... }
  ```
- **Unauthorized Handling**: When an authenticated user with `Role = "Staff"` navigates directly to an Admin route (e.g., `/Purchase/Create` or `/Supplier/Index`), ASP.NET Core intercepts the pipeline and redirects to `/Account/AccessDenied`, displaying an HTTP 403 Forbidden notification.

### 3.2 Granular Action-Level Permissions Matrix
| Controller | Action / Route | Permitted Role | Protection Mechanism |
|---|---|:---:|---|
| `AccountController` | `Login`, `Logout` | Public / All | Standard Cookie Pipeline |
| `AccountController` | `AccessDenied` | Authenticated | Informational HTTP 403 View |
| `HomeController` | `Index` | Admin / Staff | Dynamic financial card suppression |
| `SaleController` | `New`, `Save`, `Receipt` | Admin / Staff | POS counter dispensing |
| `SaleController` | `History` | Admin / Staff | Data scoped: Admin sees all; Staff sees own |
| `MedicineController` | `Index` | Admin / Staff | Read-only catalog search for Staff |
| `MedicineController` | `Create`, `Edit`, `ToggleActive`| **Admin Only** | `[Authorize(Roles = "Admin")]` required |
| `CategoryController` | All Actions | **Admin Only** | `[Authorize(Roles = "Admin")]` required |
| `StockController` | `Index`, `Details` | Admin / Staff | Inventory balances and batch drill-down |
| `ReportController` | `Expiry`, `LowStock` | Admin / Staff | Stock warning reports |
| `ReportController` | `DailySales` | **Admin Only** | Financial revenue report |
| `SupplierController`| All Actions | **Admin Only** | `[Authorize(Roles = "Admin")]` required |
| `PurchaseController`| All Actions | **Admin Only** | `[Authorize(Roles = "Admin")]` required |
| `TeamMemberController`| All Actions | **Admin Only** | `[Authorize(Roles = "Admin")]` required |
| `BackupController` | All Actions | **Admin Only** | `[Authorize(Roles = "Admin")]` required |
| `ActivityLogController`| All Actions | **Admin Only** | `[Authorize(Roles = "Admin")]` required |

---

## 4. Immutable Activity Audit Logging

The `ActivityLogs` table provides an append-only forensic audit trail for sensitive administrative, financial, and inventory actions.

### 4.1 Mandatory Audit Events
The system requires an immutable log entry for:
1. **`Login`**: Successful cashier or administrator sign-in (`"Login successful"`).
2. **`Sale`**: Checkout confirmation (`"Receipt S-000125, Total: $45.00"`).
3. **`Purchase`**: Stock invoice receipt (`"Invoice INV-8890 from Nile Pharma, Total: $1,250.00"`).
4. **`PriceChange`**: Medicine catalog selling price modification (`"Panadol 500mg price changed from 20.00 to 25.00"`).
5. **`Backup`**: Database backup execution (`"Database backup saved to C:\PharmaDesk\Backups\PharmaDesk_20261120.bak"`).
6. **`StaffUpdate`**: Staff creation, role change, or deactivation.

### 4.2 Non-Repudiation & Protection
- `ActivityLogs` records are linked by foreign key to `TeamMembers(Id)` with `DeleteBehavior.Restrict`.
- There is **no controller endpoint or UI button** that permits updating or deleting audit records.
- Records can only be viewed by administrators on the `/ActivityLog/Index` screen.

---

## 5. Secret Management & Database Connection Security

1. **Zero Hardcoded Secrets**: Connection strings with plaintext passwords or sensitive API keys must **never** be committed to source code or documentation.
2. **Local Windows Authentication**: Development and single-PC deployments use Windows Integrated Security (`Trusted_Connection=True;TrustServerCertificate=True;`), eliminating database password storage from `appsettings.json`.
3. **CSRF Protection**: All POST endpoints enforce Anti-Forgery Token validation via `[ValidateAntiForgeryToken]` and client-side `@Html.AntiForgeryToken()` forms or JSON header injection (`RequestVerificationToken`).
