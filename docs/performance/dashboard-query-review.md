# Dashboard Query Performance Review

This document evaluates the query execution design of the main dashboard endpoint.

---

## 1. Query Execution Profile

* **Database Round Trips**: Currently, the dashboard aggregates values by querying count/sum operations sequentially against each service-type table (Flights, Hotels, Visas, etc.). This executes 10 database round-trips.
* **Database Views Alternative**: For high-volume multi-service reporting, sequential tables aggregation can be refactored later into a materialized read model or a database view combining booking statistics.
* **Latency Profile**: Sourced against target indexed foreign keys, execution is sub-10ms on normal business datasets.
* **Caching**: Sharing cached copies is not allowed due to direct financial cash-in-hand properties inside the dashboard payload.
