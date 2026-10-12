# Sales Read API Behavior

This document details the behavioral analysis of legacy Laravel read operations and mapping recommendations for ASP.NET Core endpoints.

---

## 1. Mapped Read Operations

### Dashboard Summary
- **Read ID**: `READ_DASHBOARD_SUMMARY`
- **Laravel Route**: `GET /dashboard/summary`
- **Laravel Controller**: `DashboardController@summary`
- **Tables Read**: `flight_bookings`, `hotel_bookings`, `sales_visa_applications`, `vfs_appointments`, `holiday_packages`, `transportations`, `other_services`, `receipts`.
- **Ownership Scope**: Strict query: `WHERE user_id = auth()->id()`.
- **Potential N+1**: Multi-service loop aggregation. Resolved in .NET by executing unified database projections.
- **Expected .NET Endpoint**: `GET /api/sales/dashboard/summary`

### Customer Heads list
- **Read ID**: `READ_HEADS_LIST`
- **Laravel Route**: `GET /heads`
- **Laravel Controller**: `HeadController@index`
- **Tables Read**: `heads`
- **Ownership Scope**: `WHERE created_by = auth()->id()`.
- **Search Fields**: `first_name`, `last_name`, `email`, `mobile_no`.
- **Expected .NET Endpoint**: `GET /api/sales/heads`

### Ledger History
- **Read ID**: `READ_HEAD_LEDGER`
- **Laravel Route**: `GET /heads/{id}/ledger`
- **Laravel Controller**: `HeadController@ledger`
- **Tables Read**: `head_ledgers`
- **Ownership Scope**: Linked to user-owned head.
- **Expected .NET Endpoint**: `GET /api/sales/heads/{id}/ledger`
