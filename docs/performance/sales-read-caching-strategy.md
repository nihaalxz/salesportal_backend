# Sales Read Caching Strategy

This document classifies Sales Read endpoints for caching opportunities.

---

| Endpoint | Cache Category | Rationale / Expiration |
| :--- | :--- | :--- |
| **GET `/api/sales/dashboard/summary`** | **Never Cache** | Contains live physical cash-in-hand figures. |
| **GET `/api/sales/heads`** | **Never Cache** | Contains customer balance amounts. |
| **GET `/api/sales/heads/{id}/ledger`** | **Never Cache** | Real-time financial ledger details. |
| **GET `/api/sales/leads`** | **Private Short-Lived** | 30-second output caching candidate (private). |
| **GET `/api/sales/bookings/*`** | **Never Cache** | Rapidly changing payment/processing statuses. |
| **GET `/api/sales/performance`** | **Private Cached** | 5-minute output caching candidate (data changes monthly). |
