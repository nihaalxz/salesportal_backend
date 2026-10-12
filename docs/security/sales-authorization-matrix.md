# Sales Authorization Matrix

This document maps authorization requirements, ownership constraints, and expected responses for every Sales Read endpoint.

---

| Endpoint Route | HTTP Method | Auth Required? | Ownership Filter? | Expected 401 | Expected 403 / 404 |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `/api/sales/dashboard/summary` | GET | Yes | Scoped to User | Unauthorized | N/A |
| `/api/sales/heads` | GET | Yes | Scoped to Creator | Unauthorized | N/A |
| `/api/sales/heads/{id}` | GET | Yes | Scoped to Creator | Unauthorized | 404 Not Found |
| `/api/sales/heads/{id}/ledger` | GET | Yes | Head Creator Check | Unauthorized | 404 Not Found |
| `/api/sales/leads` | GET | Yes | Scoped to Creator | Unauthorized | N/A |
| `/api/sales/leads/{id}` | GET | Yes | Scoped to Creator | Unauthorized | 404 Not Found |
| `/api/sales/bookings/flights` | GET | Yes | Scoped to User | Unauthorized | N/A |
| `/api/sales/bookings/hotels` | GET | Yes | Scoped to User | Unauthorized | N/A |
| `/api/sales/bookings/visas` | GET | Yes | Scoped to User | Unauthorized | N/A |
| `/api/sales/bookings/vfs` | GET | Yes | Scoped to Creator | Unauthorized | N/A |
| `/api/sales/bookings/holidays` | GET | Yes | Scoped to Creator | Unauthorized | N/A |
| `/api/sales/bookings/transport` | GET | Yes | Scoped to Creator | Unauthorized | N/A |
| `/api/sales/bookings/other` | GET | Yes | Scoped to Creator | Unauthorized | N/A |
| `/api/sales/cash/in-hand` | GET | Yes | Scoped to User | Unauthorized | 404 Not Found |
| `/api/sales/cash/deposits` | GET | Yes | Scoped to User | Unauthorized | N/A |
| `/api/sales/performance` | GET | Yes | Scoped to User | Unauthorized | N/A |
