# ADR 0001: Monolithic Architecture with ASP.NET Core MVC, EF Core, and SQL Server Express

## Status
Accepted

## Context
PharmaDesk is designed as a local, offline-first pharmacy management system for community retail pharmacies. Most pharmacies in target deployment environments operate on a single desktop computer located at the dispensing counter with intermittent or non-existent internet access. In the future, the system may need to support multiple counter terminals within the same pharmacy over a Local Area Network (LAN).

The architecture must support:
1. Zero internet dependency for daily operations (selling, inventory lookups, receipts).
2. Straightforward deployment and minimal operational overhead for non-technical users.
3. High transactional reliability with ACID guarantees (preventing partial sales, overselling, or inconsistent stock levels).
4. Low latency on counter operations (sub-100ms barcode scanning and cart updates).
5. A smooth upgrade path to multi-terminal LAN usage without redesigning the core application.

## Decision
We adopt a **monolithic ASP.NET Core MVC application** running on **.NET 9**, paired with **Entity Framework Core (EF Core)** and **Microsoft SQL Server Express** (or LocalDB for local development):
- **Server and UI in One Process**: ASP.NET Core MVC delivers server-rendered Razor views along with localized client scripts (vanilla JavaScript / jQuery). There is no separate detached SPA build or cloud microservice layer.
- **Data Access**: Entity Framework Core 9.0 handles database interaction using Code-First entity definitions, migrations, and strongly typed queries.
- **Database Engine**: Microsoft SQL Server Express running as a local Windows service on the primary pharmacy PC. For LAN expansion, other counter PCs simply open the web application in a browser directed to `http://<host-pc-ip>:<port>`.

### Codebase Alignment & Discrepancies
- In the existing codebase, `Program.cs` and `appsettings.json` configure `PharmaDeskContext` against SQL Server (`Server=.;Database=PharmaDeskDB;Trusted_Connection=True;TrustServerCertificate=True;`).
- While `PharmaDesk/README.md` mentions potential future SQLite support for portable single-file deployment, SQL Server Express remains the primary target due to concurrent write safety, robust file-level backups, and native LAN accessibility across multiple local PCs.

## Consequences
### Positive
- **Simplicity**: Single deployable executable/folder; zero cross-service network partition risks; unified debugging and logging.
- **Offline Reliability**: The entire stack runs on `localhost` without external telemetry, CDNs, or API dependencies.
- **LAN-Ready**: By running Kestrel bound to local network interfaces (`http://0.0.0.0:5000`), additional counter terminals can access the system via web browser without installing .NET or SQL Server on secondary machines.
- **ACID Transactions**: Relational transactions guarantee that multi-table mutations (Sale + SaleItems + Batch deductions + ActivityLog) commit or rollback atomically.

### Negative / Risks
- **Host PC Dependency**: If the primary PC fails or shuts down, the entire pharmacy system is unavailable until restored.
- **Local Database Administration**: Backups must be actively scheduled or triggered to external USB drives (addressed in ADR 0010 and Roadmap Step 10).
