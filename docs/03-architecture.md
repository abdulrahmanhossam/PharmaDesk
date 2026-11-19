# 03. Architecture & System Design

## 1. Architectural Style & Principles

PharmaDesk is designed as a **monolithic, layered ASP.NET Core MVC application** running on **.NET 9**. The architecture follows three primary tenets:

1. **Offline-First & Local Autonomous Execution**: The application, database, and presentation assets operate entirely within the local machine perimeter. No external cloud calls or CDN requests are permitted.
2. **ACID Financial and Inventory Transactions**: Business logic involving inventory balance shifts, batch allocations, and receipt creation occurs within explicit database transactions (`IDbContextTransaction`).
3. **Layered Separation of Concerns**: Controllers delegate domain calculations (such as FEFO allocation, report aggregation, and backup commands) to a dedicated Service Layer, keeping controllers thin and focused on HTTP request lifecycle, validation, and view presentation.

---

## 2. Layered Component Diagram

```mermaid
flowchart TD
    subgraph PresentationLayer["Presentation Layer (MVC & Razor)"]
        Browser[Client Web Browser / POS Terminal]
        StaticAssets["Local Static Assets (Bootstrap 5, Icons, jQuery, Select2)"]
        Views["Razor Views (.cshtml) + ViewModels"]
        Controllers["Controllers (Account, Home, Sale, Medicine, Stock, Purchase, Supplier, Report, TeamMember)"]
    end

    subgraph SecurityLayer["Security & Pipeline Middleware"]
        AuthMiddleware["Cookie Authentication Middleware"]
        AuthzMiddleware["Role Authorization Middleware ([Authorize(Roles='Admin')])"]
        SecurityFilter["Anti-Forgery Token Verification"]
    end

    subgraph ServiceLayer["Domain & Business Service Layer"]
        SaleService["SaleService (FEFO Allocation, Cart Processing)"]
        StockService["StockService (Batch Balance, Expiry & Low Stock Evaluation)"]
        ReportService["ReportService (Daily Sales Analytics & Financial Aggregation)"]
        BackupService["BackupService (SQL Server Backup Orchestration)"]
    end

    subgraph DataAccessLayer["Data Access & Persistence Layer (EF Core 9)"]
        DbContext["PharmaDeskContext (DbSets, Model Configurations, Indexes)"]
        DbSeeder["DbSeeder (Default Admin & Standard Categories Seed)"]
    end

    subgraph StorageLayer["Database Storage Engine"]
        SqlServer["Microsoft SQL Server Express / LocalDB (PharmaDeskDB)"]
        BackupStorage["Local / External USB Disk (Database .bak Backups)"]
    end

    Browser <--> Views
    Browser <--> StaticAssets
    Views <--> Controllers
    Controllers --> SecurityLayer
    SecurityLayer --> ServiceLayer
    ServiceLayer --> DataAccessLayer
    DataAccessLayer --> StorageLayer
    BackupService -.-> BackupStorage
```

---

## 3. Directory & Folder Layout

The repository is structured to maintain clean boundaries between models, viewmodels, data access, services, controllers, and static client assets:

```
PharmaDesk/
├── Controllers/                 # HTTP request handlers and endpoint routing
│   ├── AccountController.cs     # PIN authentication, cookie issuance, and lockout
│   ├── CategoryController.cs    # Medicine category administration
│   ├── HomeController.cs        # Main operational dashboard
│   ├── MedicineController.cs    # Product catalog management
│   ├── PurchaseController.cs    # Supplier invoice & inward batch reception
│   ├── ReportController.cs      # Expiry, low-stock, and sales reports
│   ├── SaleController.cs        # Counter POS cart and receipt printing
│   ├── StockController.cs       # Live stock and batch drill-down inspection
│   ├── SupplierController.cs    # Vendor directory management
│   └── TeamMemberController.cs  # Staff user administration and PIN resets
├── Data/                        # Persistence context and seed infrastructure
│   ├── DbSeeder.cs              # Initial administrator and category seeding
│   └── PharmaContext.cs         # EF Core DbContext, relationships, and index definitions
├── Models/                      # Core domain entities (one class per database table)
│   ├── ActivityLog.cs           # System audit trail entity
│   ├── Batch.cs                 # Physical batch inventory entity
│   ├── Category.cs              # Medicine category entity
│   ├── Medicine.cs              # Product catalog entity
│   ├── Purchase.cs              # Supplier purchase header entity
│   ├── PurchaseItem.cs          # Supplier purchase line item entity
│   ├── Sale.cs                  # Customer sales transaction header entity
│   ├── SaleItem.cs              # Customer sales transaction line item entity
│   ├── Supplier.cs              # Vendor entity
│   └── TeamMember.cs            # Cashier and administrator user entity
├── Services/                    # Domain logic & transactional workflows
│   ├── SaleService.cs           # Transactional FEFO checkout processing
│   ├── StockService.cs          # (Planned) Dedicated batch and stock calculations
│   ├── BackupService.cs         # (Planned) Database backup automation
│   └── ReportService.cs         # (Planned) Reporting aggregation service
├── ViewModels/                  # Strongly typed UI request/response models
│   ├── LoginViewModel.cs        # User selection and PIN input
│   ├── MedicineViewModel.cs     # Product creation and editing UI shape
│   ├── PurchaseViewModel.cs     # Multi-item purchase form shape
│   ├── SaleViewModel.cs         # POS cart and item payload
│   ├── StockViewModel.cs        # Consolidated stock display shape
│   └── TeamMemberViewModel.cs   # Staff account management shape
├── Views/                       # Razor view templates organized by controller
│   ├── Account/                 # Login and AccessDenied views
│   ├── Category/                # Category CRUD views
│   ├── Home/                    # Dashboard view
│   ├── Medicine/                # Catalog listing, creation, and edit views
│   ├── Purchase/                # Purchase invoice creation and index views
│   ├── Report/                  # Expiry, Low Stock, and Daily Sales report views
│   ├── Sale/                    # POS Counter, Sales History, and Receipt views
│   ├── Shared/                  # _Layout.cshtml, error pages, validation partials
│   ├── Stock/                   # Stock index and batch details drill-down
│   ├── Supplier/                # Vendor CRUD views
│   └── TeamMember/              # User management views
├── wwwroot/                     # Static web assets
│   ├── css/                     # Custom application styles
│   ├── js/                      # sale.js (POS cart logic), purchase.js
│   └── lib/                     # Local vendor packages (Bootstrap, Icons, jQuery, Select2)
├── appsettings.json             # Connection string and runtime logging configuration
├── GlobelUsing.cs               # Global namespace imports
└── Program.cs                   # Application bootstrapping, DI, pipeline configuration
```

---

## 4. End-to-End Request Flow

### 4.1 POS Sale Request Flow

```mermaid
sequenceDiagram
    autonumber
    actor Cashier as Cashier (Counter)
    participant Browser as Browser (sale.js)
    participant SaleCtrl as SaleController
    participant SaleSvc as SaleService
    participant Db as SQL Server (EF Core)

    Cashier->>Browser: Scan Barcode / Press Enter
    Browser->>Browser: Find Medicine & Increment Cart Qty
    Cashier->>Browser: Click "Save and show receipt"
    Browser->>SaleCtrl: POST /Sale/Save (JSON Cart + AntiForgeryToken)
    SaleCtrl->>SaleSvc: ProcessSaleAsync(SaleViewModel)
    
    activate SaleSvc
    SaleSvc->>Db: BeginTransactionAsync()
    SaleSvc->>Db: Generate Receipt Number (e.g. S-000125)
    SaleSvc->>Db: Query Unexpired Batches (b.ExpiryDate > Today ORDER BY ExpiryDate ASC)
    
    alt Insufficient Stock across all batches
        SaleSvc->>Db: RollbackAsync()
        SaleSvc-->>SaleCtrl: Return (false, "Not enough stock")
        SaleCtrl-->>Browser: JSON { success: false, message: ... }
    else Stock Available
        loop For each cart item
            SaleSvc->>SaleSvc: Deduct quantity from earliest batch (FEFO)
            SaleSvc->>Db: Insert SaleItem (UnitPrice = Medicine.SellingPrice)
            SaleSvc->>Db: Update Batch.QuantityInStock
        end
        SaleSvc->>Db: Insert Sale Header (TotalAmount, ReceiptNumber, TeamMemberId)
        SaleSvc->>Db: Insert ActivityLog ("Sale", "Receipt S-000125...")
        SaleSvc->>Db: SaveChangesAsync()
        SaleSvc->>Db: CommitAsync()
        deactivate SaleSvc
        SaleSvc-->>SaleCtrl: Return (true, "Sale completed", saleId)
        SaleCtrl-->>Browser: JSON { success: true, saleId: 125 }
        Browser->>Cashier: Open /Sale/Receipt/125 in print modal
    end
```

---

## 5. Network Topology & Future Expansion

PharmaDesk is deployed initially as a **Single-PC Workstation**:
- ASP.NET Core Kestrel and SQL Server Express run on the same Windows host machine.
- The browser opens `http://localhost:5000`.

### Local Area Network (LAN) Readiness
Because the system is built with web technologies, scaling to multiple counter terminals within the same pharmacy requires **zero architectural refactoring**:

```mermaid
flowchart LR
    subgraph ServerPC["Main Pharmacy PC (Dispensing & Server)"]
        AppServer["PharmaDesk Kestrel (:5000)"]
        DbServer["SQL Server Express (PharmaDeskDB)"]
        MainBrowser["Browser: Counter 1 (Admin/Cashier)"]
        AppServer <--> DbServer
        MainBrowser <--> AppServer
    end

    subgraph ClientPC1["Counter Terminal 2 (LAN)"]
        Browser2["Web Browser: Counter 2 (Staff)"]
    end

    subgraph ClientPC2["Office / Inventory Laptop (LAN)"]
        Browser3["Web Browser: Office (Admin)"]
    end

    Browser2 <-->|LAN HTTP| AppServer
    Browser3 <-->|LAN HTTP| AppServer
```

To enable multi-terminal LAN usage:
1. Configure Kestrel in `Program.cs` or `appsettings.json` to bind to `http://0.0.0.0:5000`.
2. Allow incoming port 5000 on the Windows Defender Firewall of the main PC.
3. Secondary counter computers open `http://<main-pc-ip>:5000` in their standard browser.
