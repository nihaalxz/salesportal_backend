# Sales Read Database Command Footprints

This document details database command execution budgets allowed per endpoint request.

---

| Endpoint | Data Queries | Count/Aggregate Queries | Total Budget |
| :--- | :--- | :--- | :--- |
| **GET `/api/sales/dashboard/summary`** | 1 (Cash) | 9 (Booking Counts & Financial sums) | **10** |
| **GET `/api/sales/heads`** | 1 | 1 | **2** |
| **GET `/api/sales/heads/{id}`** | 1 | 0 | **1** |
| **GET `/api/sales/heads/{id}/ledger`** | 1 | 1 | **2** |
| **GET `/api/sales/leads`** | 1 | 1 | **2** |
| **GET `/api/sales/leads/{id}`** | 1 | 0 | **1** |
| **GET `/api/sales/bookings/flights`** | 1 | 1 | **2** |
| **GET `/api/sales/bookings/hotels`** | 1 | 1 | **2** |
| **GET `/api/sales/bookings/visas`** | 1 | 1 | **2** |
| **GET `/api/sales/bookings/vfs`** | 1 | 1 | **2** |
| **GET `/api/sales/bookings/holidays`** | 1 | 1 | **2** |
| **GET `/api/sales/bookings/transport`** | 1 | 1 | **2** |
| **GET `/api/sales/bookings/other`** | 1 | 1 | **2** |
| **GET `/api/sales/cash/in-hand`** | 1 | 0 | **1** |
| **GET `/api/sales/cash/deposits`** | 1 | 1 | **2** |
| **GET `/api/sales/performance`** | 1 | 1 | **2** |
