# 06. Screens & UI/UX Style Guide

## 1. Screen Inventory & Route Directory

The table below catalogs every screen in PharmaDesk, mapping its purpose, route pattern, underlying controller, view template, and role accessibility.

| # | Screen Name | Route Pattern | Controller Action | View Path | Role | Description |
|---|---|---|---|---|:---:|---|
| **01** | **Login** | `/Account/Login` | `AccountController.Login` | `Views/Account/Login.cshtml` | Public | User dropdown, 4-digit PIN input, 5-try lockout timer, clean numpad. |
| **02** | **Dashboard** | `/Home/Index` | `HomeController.Index` | `Views/Home/Index.cshtml` | Admin / Staff | Today's sales ($ - Admin only), sales volume, low stock count, top 10 expiring batches. |
| **03** | **New Sale (POS)** | `/Sale/New` | `SaleController.New` | `Views/Sale/New.cshtml` | Admin / Staff | Barcode scanner autofocus box, live cart table, quantity editing, total summary, instant checkout. |
| **04** | **Receipt Slip** | `/Sale/Receipt/{id}` | `SaleController.Receipt` | `Views/Sale/Receipt.cshtml` | Admin / Staff | Minimalist, monospaced thermal slip preview with auto `window.print()` trigger. |
| **05** | **Sales History** | `/Sale/History` | `SaleController.History` | `Views/Sale/History.cshtml` | Admin / Staff (Scoped)| Chronological sales register. Admin sees all transactions; Staff sees only their own. |
| **06** | **Medicines Catalog** | `/Medicine/Index` | `MedicineController.Index` | `Views/Medicine/Index.cshtml` | Admin / Staff (Read)| Searchable catalog by barcode, brand, or generic name. Add/Edit restricted to Admin. |
| **07** | **Add Medicine** | `/Medicine/Create` | `MedicineController.Create` | `Views/Medicine/Create.cshtml` | **Admin Only** | Form to register trade name, generic name, barcode, category, unit, price, and alert level. |
| **08** | **Edit Medicine** | `/Medicine/Edit/{id}` | `MedicineController.Edit` | `Views/Medicine/Edit.cshtml` | **Admin Only** | Form to update details and modify `SellingPrice` (triggers `ActivityLog.PriceChange`). |
| **09** | **Stock Inventory** | `/Stock/Index` | `StockController.Index` | `Views/Stock/Index.cshtml` | Admin / Staff | Consolidated unexpired stock totals by medicine, stock status badges (OK, Low, Expired). |
| **10** | **Batch Drill-Down** | `/Stock/Details/{id}` | `StockController.Details` | `Views/Stock/Details.cshtml` | Admin / Staff | Inspection of individual batch lots for a medicine, showing lot #, expiry, and quantity. |
| **11** | **Expiry Report** | `/Report/Expiry` | `ReportController.Expiry` | `Views/Report/Expiry.cshtml` | Admin / Staff | Filtered list of batches expiring within 90 days (orange) or already expired (red). |
| **12** | **Low Stock Report** | `/Report/LowStock` | `ReportController.LowStock` | `Views/Report/LowStock.cshtml` | Admin / Staff | Medicines where $\text{TotalStock} \le \text{LowStockLevel}$, sorted by highest depletion. |
| **13** | **Suppliers Directory**| `/Supplier/Index` | `SupplierController.Index` | `Views/Supplier/Index.cshtml` | **Admin Only** | Vendor listing, contact numbers, addresses, and active/hidden toggles. |
| **14** | **Add/Edit Supplier** | `/Supplier/Create`, `/Edit` | `SupplierController.Create`/`Edit` | `Views/Supplier/Create/Edit.cshtml`| **Admin Only** | Vendor registration and contact update forms. |
| **15** | **Purchases Register** | `/Purchase/Index` | `PurchaseController.Index` | `Views/Purchase/Index.cshtml` | **Admin Only** | Inward purchase invoice history with vendor name, date, invoice #, and total cost. |
| **16** | **New Purchase** | `/Purchase/Create` | `PurchaseController.Create` | `Views/Purchase/Create.cshtml` | **Admin Only** | Multi-item receiving form: Select2 supplier and medicine search, batch lot, expiry, qty, and unit cost. |
| **17** | **Daily Sales Report** | `/Report/DailySales` | `ReportController.DailySales` | `Views/Report/DailySales.cshtml` | **Admin Only** | End-of-day financial report: gross sales, invoice count, and per-cashier revenue breakdown. |
| **18** | **Team Members** | `/TeamMember/Index` | `TeamMemberController.Index` | `Views/TeamMember/Index.cshtml` | **Admin Only** | Staff roster, roles (`Admin`/`Staff`), active statuses, and account management. |
| **19** | **Add/Edit Staff** | `/TeamMember/Create`/`Edit`| `TeamMemberController.Create`/`Edit`| `Views/TeamMember/Create/Edit.cshtml`| **Admin Only** | Register staff member, assign role, and set or reset 4-digit numeric PIN. |
| **20** | **Database Backup** | `/Backup/Index` | `BackupController.Index` | `Views/Backup/Index.cshtml` | **Admin Only** | One-button manual database backup to local folder or external USB disk. |
| **21** | **Activity Audit Log** | `/ActivityLog/Index` | `ActivityLogController.Index` | `Views/ActivityLog/Index.cshtml` | **Admin Only** | Chronological audit trail of logins, sales, purchases, price modifications, and backups. |
| **22** | **Access Denied** | `/Account/AccessDenied` | `AccountController.AccessDenied`| `Views/Account/AccessDenied.cshtml` | Authenticated | Friendly HTTP 403 screen explaining role restriction when Staff tries to visit Admin pages. |

---

## 2. Global Layout & Shell Structure

All authenticated pages share a standard responsive desktop shell defined in `PharmaDesk/Views/Shared/_Layout.cshtml`:

```
┌─────────────────┬───────────────────────────────────────────────────────────┐
│  PharmaDesk 💊  │ Good morning, Ahmed (Admin)                        Logout │ Top Bar
├─────────────────┼───────────────────────────────────────────────────────────┤
│ 📊 Dashboard    │                                                           │
│ 🛒 New Sale     │                                                           │
│ 🧾 Sales History│                                                           │
│ 💊 Medicines    │                    PAGE WORK AREA                         │
│ 📦 Stock        │                                                           │
│ ⚠️ Expiry Report│              (RenderBody() / Dynamic Content)             │
│ 📉 Low Stock    │                                                           │
│ ────────────────│                                                           │
│ 🚚 Suppliers    │                                                           │
│ 📥 Purchases    │                                                           │
│ 📈 Daily Sales  │                                                           │
│ 👥 Team Members │                                                           │
│ 💾 Backup       │                                                           │
│ 📋 Activity Log │                                                           │
└─────────────────┴───────────────────────────────────────────────────────────┘
```

1. **Fixed Sidebar (Left, 240px)**:
   - Header with pharmacy brand logo and calm green branding (`#2e7d32`).
   - Clean vertical navigation links with Bootstrap Icons.
   - Dynamic role filtering: administrative links (`Suppliers`, `Purchases`, `Daily Sales`, `Team Members`, `Backup`, `Activity Log`) are rendered conditionally with `@if (User.IsInRole("Admin"))`.
2. **Top Bar (Header)**:
   - User greeting (e.g., `Good morning, Ahmed`) dynamically reading `User.Identity.Name`.
   - Current user status and one-click Logout form (`POST /Account/Logout`).
3. **Main Content Container (Right, margin-left 240px)**:
   - Houses the dynamic page views wrapped inside Bootstrap cards, structured tables, and metric summary tiles.

---

## 3. UI/UX Style Guide & Design System

The visual design is specifically tailored for busy pharmacy dispensing environments where clarity, speed, high contrast, and touchscreen accessibility are paramount.

### 3.1 Color Palette

```
  Calm Green (Primary)       Red Alert (Expired)       Orange Alert (Warning)
       #2e7d32                     #dc3545                    #fd7e14
   [Brand & Normal OK]      [Expired Batches/Errors]   [Low Stock & Close Expiry]

     Card Background             Body Background             Text Muted
         #ffffff                     #f8f9fa                   #6c757d
```

- **Primary Green (`#2e7d32` / `#1f8a5b`)**: Represents clinical trust, health, and calm. Used for the brand bar, primary action buttons ("Save and show receipt"), and active navigation highlights.
- **Red Alert (`#dc3545` / `.text-danger`)**: Indicates non-negotiable danger. Reserved strictly for **expired medicines** ($\text{ExpiryDate} \le \text{Today}$) and severe validation rejections.
- **Orange Warning (`#fd7e14` / `.text-warning-custom`)**: Indicates proactive attention required. Used for medicines **expiring within 90 days** and items with **stock $\le$ alert level**.
- **Neutral Greys (`#f8f9fa`, `#ffffff`, `#dee2e6`)**: Clean, clinical contrast that reduces glare and eye fatigue during 12-hour counter shifts.

### 3.2 Typography & Sizing Rules
- **Base Body Font**: System UI font stack (`system-ui, -apple-system, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif`), set to **16px** for high legibility from standing counter distances.
- **Header Numbers & Financial Totals**: Large, bold numerical displays (**28px - 32px**, e.g., `.fs-2`, `.fs-3.fw-bold`) to prevent misreading transaction amounts or remaining unit counts.
- **Receipt Slip**: Monospaced font stack (`'Courier New', Courier, ui-monospace, monospace`) styled specifically for 80mm/58mm thermal paper rolls.

### 3.3 Counter Usability Rules
1. **Autofocus Barcode Box**: On `/Sale/New`, the cursor automatically focuses inside `#barcodeInput` on page load and immediately refocuses after every line item added or sale saved. The cashier can scan consecutive items without ever touching the mouse.
2. **Big Touch-Friendly Buttons**: Primary action buttons on the POS counter screen (`Save and show receipt`, `Clear cart`) have large padding (`.btn-lg`, min height 48px) for effortless touchscreen tapping or mouse clicks.
3. **Modal Autocomplete**: When a typed medicine query matches multiple products, an interactive modal appears allowing immediate single-key or click selection.
