# 01. Product Overview

## 1. Business Context

**PharmaDesk** is a modern, lightweight, offline-first pharmacy management system specifically engineered for community retail pharmacies and clinic dispensaries. 

In emerging markets and suburban community centers, small pharmacies frequently operate on a single desktop computer located at the dispensing counter. These businesses encounter severe constraints:
- **Intermittent or Absent Internet**: Cloud-dependent SaaS solutions frequently experience outages, halting counter transactions and stranding customers.
- **Expiry Losses**: Without automated batch tracking, medicines close to expiration expire at the back of shelves, causing recurring stock loss.
- **Dispensing Hazards**: Without systematic inventory validation, expired medications risk being dispensed to patients.
- **Overcomplicated Legacy Software**: Existing retail POS systems are often bloated with complex accounting, high subscription fees, and difficult onboarding.

PharmaDesk solves these problems through an ultra-fast, barcode-driven interface with strict **FEFO (First-Expired, First-Out)** batch inventory control, running completely offline on standard desktop hardware.

---

## 2. Target Users & Personas

PharmaDesk serves two primary user personas:

### 1. Pharmacy Owner / Administrator (`Admin`)
- **Profile**: Licensed pharmacist or store proprietor responsible for regulatory compliance, pricing, purchasing, and business sustainability.
- **Primary Goals**:
  - Maintain healthy inventory levels and prevent drug expiration write-offs.
  - Review daily gross revenues, invoice totals, and staff sales volume.
  - Negotiate with suppliers and record incoming inventory deliveries.
  - Manage staff access, reset credentials, and monitor the activity audit log.
  - Secure database copies to external storage (USB drives).

### 2. Counter Staff / Cashier (`Staff`)
- **Profile**: Dispensary clerk or pharmacy technician working long shifts at the retail counter.
- **Primary Goals**:
  - Rapidly process customer orders using a handheld barcode scanner.
  - Dispense medications with zero mouse interaction required during peak counter hours.
  - Check medicine stock levels and active batch expiry dates in real time.
  - Review only their personal daily dispensing history.

---

## 3. Product Scope — Version 1.0

### Core Operational Capabilities
1. **Barcode-First Counter Sales**: Scan a barcode to immediately populate the cart; automated keyboard-driven quantity adjustments; single-press checkout.
2. **Batch-Level Inventory Tracking**: Every received quantity is stored as an independent batch with its manufacturer lot number, expiry date, and unit cost.
3. **Automated FEFO Allocation**: The system automatically pulls stock from the earliest expiring batch, seamlessly splitting across batches when an item quantity requires it.
4. **Expiry Warning Visuals**: Color-coded expiration monitoring:
   - **Red Alert**: Expired batches ($\text{ExpiryDate} \le \text{Today}$).
   - **Orange Warning**: Batches expiring within 90 days.
   - **Green Normal**: Batches with more than 90 days of shelf life.
5. **Low Stock Detection**: Automatic alerts whenever total non-expired stock drops to or below the medicine's configured alert threshold.
6. **Supplier & Purchase Management**: Record supplier invoices; saving a purchase automatically creates active inventory batches.
7. **Role-Based Authentication**: Secure 4-digit PIN authentication with a 5-consecutive-attempt lockout mechanism (5-minute cooldown) and encrypted session cookies.
8. **On-Screen Receipts**: Printable, monospaced thermal slip preview displayed immediately upon sale confirmation (`window.print()`).
9. **Daily Sales Analytics**: Real-time sales totals, transaction counts, and cashier-by-cashier revenue breakdown (Admin only).
10. **Activity Audit Trail**: Forensic audit log recording logins, sales, purchases, catalog price updates, and backups.
11. **One-Click Backup**: Instant SQL Server database backup to local disk or attached storage.

---

## 4. Out of Scope for Version 1.0

To maintain a laser focus on core dispensing reliability and rapid delivery, the following features are explicitly **deferred** to subsequent versions:

| Deferred Capability | Justification & Future Trajectory |
|---|---|
| **E-Commerce / Online Store** | PharmaDesk is an in-person, over-the-counter system. No public-facing shopping cart, online order booking, or payment gateway is included in v1. |
| **Delivery Tracking & Courier Integration** | Local retail only; no dispatch rider routing or shipping integrations. |
| **Direct Hardware Thermal Printer Drivers** | v1 renders clean HTML/CSS thermal receipts formatted for 80mm/58mm rolls triggered via standard browser `window.print()`. Direct ESC/POS raw socket printing is deferred. |
| **Returns & Customer Refunds** | Counter returns, batch re-stocking, and credit notes will be designed as a dedicated module in v2. |
| **Discounts & Loyalty Promotions** | Fixed pricing per unit in v1; promotional codes, item discounts, and customer loyalty points are out of scope. |
| **Customer CRM & Profiles** | Sales in v1 are anonymous retail counter transactions (`PaymentMethod = "Cash"`). Patient profiles and prescription histories belong to a clinical module. |
| **RTL / Arabic User Interface** | While all naming and data fields fully support Arabic Unicode strings, the primary interface in v1 is English. Full RTL stylesheet localization is planned for v1.2. |
| **Multi-Branch Synchronization** | Single-store deployment only; central multi-location cloud sync is outside the architectural scope of v1. |
