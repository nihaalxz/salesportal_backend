# Sales Read Performance Baselines

These baseline performance metrics are measured using test suites against the HTTP pipeline host.

---

| Endpoint | Median Duration (ms) | DB Command Count | Response Payload Size | Rows Returned |
| :--- | :--- | :--- | :--- | :--- |
| `/api/sales/dashboard/summary` | 12ms | 10 | 280 bytes | 1 DTO |
| `/api/sales/heads` | 6ms | 2 | 410 bytes | 1 row |
| `/api/sales/heads/{id}` | 4ms | 1 | 240 bytes | 1 row |
| `/api/sales/leads` | 5ms | 2 | 310 bytes | 1 row |
| `/api/sales/bookings/flights` | 8ms | 2 | 390 bytes | 1 row |
| `/api/sales/cash/in-hand` | 3ms | 1 | 110 bytes | 1 row |

*Note: Measured inside a localized integration environment. Network serialization latency is excluded.*
