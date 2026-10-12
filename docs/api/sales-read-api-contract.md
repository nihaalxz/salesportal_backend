# Sales Read API Contract

This document specifies the concrete API surface, HTTP routes, request parameters, and response models for the modernized Sales Read APIs.

---

## 1. Dashboard Operations

### GET `/api/sales/dashboard/summary`
- **HTTP Method**: GET
- **Permission**: `sales.dashboard.view`
- **Response**:
  ```json
  {
    "totalBookings": 15,
    "totalCollected": 1250.000,
    "totalPending": 300.000,
    "activeLeads": 5
  }
  ```

---

## 2. Customer Heads Operations

### GET `/api/sales/heads`
- **Query Parameters**:
  - `page` (int, default 1)
  - `pageSize` (int, default 10, max 100)
  - `searchTerm` (string, max 50 chars)
  - `sortBy` (string: `firstName`, `lastName`, `balance`, default `lastName`)
  - `sortOrder` (string: `asc`, `desc`, default `asc`)
- **Response**:
  ```json
  {
    "items": [
      {
        "id": 1,
        "firstName": "John",
        "lastName": "Doe",
        "email": "john.doe@gmail.com",
        "balance": 100.000,
        "wallet": 0.000
      }
    ],
    "page": 1,
    "pageSize": 10,
    "totalCount": 1,
    "totalPages": 1,
    "hasNextPage": false,
    "hasPreviousPage": false
  }
  ```
