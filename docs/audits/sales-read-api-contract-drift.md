# API Contract Drift Audit

This audit cross-references actual implemented REST endpoints against the design specification in `docs/api/sales-read-api-contract.md`.

---

## 1. Route Comparison

| Specification Route | Actual API Controller Route | Match? | Notes |
| :--- | :--- | :--- | :--- |
| `GET /api/sales/dashboard/summary` | `GET /api/sales/dashboard/summary` | **Yes** | Returns BHD aggregates and service booking counts. |
| `GET /api/sales/heads` | `GET /api/sales/heads` | **Yes** | Paginated heads. |
| `GET /api/sales/heads/{id}` | `GET /api/sales/heads/{id}` | **Yes** | Detailed head info. |
| `GET /api/sales/heads/{id}/ledger` | `GET /api/sales/heads/{id}/ledger` | **Yes** | Paginated ledger log entries. |
| `GET /api/sales/leads` | `GET /api/sales/leads` | **Yes** | Paginated leads. |
| `GET /api/sales/leads/{id}` | `GET /api/sales/leads/{id}` | **Yes** | Detailed lead info. |
| `GET /api/sales/bookings/flights` | `GET /api/sales/bookings/flights` | **Yes** | Paginated flights list. |
| `GET /api/sales/bookings/hotels` | `GET /api/sales/bookings/hotels` | **Yes** | Paginated hotels list. |
| `GET /api/sales/bookings/visas` | `GET /api/sales/bookings/visas` | **Yes** | Paginated visas list. |
| `GET /api/sales/bookings/vfs` | `GET /api/sales/bookings/vfs` | **Yes** | Paginated VFS appointments. |
| `GET /api/sales/bookings/holidays` | `GET /api/sales/bookings/holidays` | **Yes** | Paginated holiday packages. |
| `GET /api/sales/bookings/transport` | `GET /api/sales/bookings/transport` | **Yes** | Paginated transport. |
| `GET /api/sales/bookings/other` | `GET /api/sales/bookings/other` | **Yes** | Paginated other bookings. |
| `GET /api/sales/cash/in-hand` | `GET /api/sales/cash/in-hand` | **Yes** | Salesman physical cash. |
| `GET /api/sales/cash/deposits` | `GET /api/sales/cash/deposits` | **Yes** | Salesman bank deposit log. |
| `GET /api/sales/performance` | `GET /api/sales/performance` | **Yes** | Monthly targets and salaries. |

---

## 2. DTO Specifications & Types Verification

- **Financial Columns**: Audited all Contracts DTO classes (`DashboardSummaryResponse`, `HeadResponses`, `HeadLedgerEntryResponse`, `LeadResponses`, `BookingResponses`, `CashResponses`, `PerformanceSummaryResponse`). Every single currency/monetary value utilizes the `decimal` type. No `float` or `double` representations are exposed.
- **Dates & Times**: Sourced using the native `.NET` types (`DateTime`, `DateOnly`, `TimeSpan`) to preserve timezone-independent formatting during serialization.
- **Enums & Statuses**: All status outputs (e.g. `PaymentStatus`, `ProcessingStatus`, `OrderStatus`) are serialized as strings rather than numeric values to match the Laravel system status constraints.
