# ADR 0009: Offline-First Local Static Asset Packaging

## Status
Accepted

## Context
Retail pharmacies in local clinics or neighborhood shops frequently experience unstable or absent internet connections. If frontend assets (Bootstrap CSS, Bootstrap Icons, jQuery, Select2) are loaded from Content Delivery Networks (CDNs) like `cdn.jsdelivr.net` or `cdnjs.cloudflare.com`, the application UI will break, lose styling, or fail to render during internet outages.

## Decision
We mandate that **100% of static client assets are stored locally within the application repository**:
1. All stylesheets, JavaScript libraries, and icon web fonts reside in `PharmaDesk/wwwroot/lib/`:
   - Bootstrap 5 (`wwwroot/lib/bootstrap/`)
   - Bootstrap Icons font files (`wwwroot/lib/bootstrap-icons/`)
   - jQuery and Validation (`wwwroot/lib/jquery/`, `wwwroot/lib/jquery-validation/`)
   - Select2 searchable dropdown plugin and themes (`wwwroot/lib/select2/`)
2. Views and layouts reference assets strictly through virtual path tag helpers (e.g. `~/lib/bootstrap/bootstrap.min.css`), never external `https://` URLs.
3. The application must achieve full visual fidelity, interactivity, and functionality with zero network connectivity.

### Codebase Alignment & Discrepancies
- The existing codebase has already downloaded and placed all necessary libraries into `PharmaDesk/wwwroot/lib/`.
- `_Layout.cshtml` references these local assets directly.
- The application verified complete offline capability.

## Consequences
### Positive
- Zero external network dependencies: counter operations never stall due to DNS failures or internet outages.
- Predictable page render times unaffected by third-party CDN latency.
- Enhanced privacy: customer and dispensing activity does not leak referrer data to third-party CDNs.

### Negative / Risks
- Repository clone size includes library files (~2MB of vendor assets).
- Vendor library updates must be manually checked in rather than bumping CDN URL tags.
