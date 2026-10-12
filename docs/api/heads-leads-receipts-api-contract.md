# Heads, Leads, and Receipts API Contracts

All endpoints are authenticated with JWT and restricted to `Salesman` role. They inherit user context from `ICurrentUser` rather than relying on browser-posted user IDs.

---

## 1. Heads Endpoints

### GET /api/sales/heads
- **Role:** Salesman
- **Query Parameters:**
  - `page` (int, default=1)
  - `pageSize` (int, default=10)
  - `searchTerm` (string, optional)
  - `sortBy` (string, default="lastName")
  - `sortOrder` (string, default="asc")
- **Response (200 OK):** `PagedResponse<HeadSummaryResponse>`

### GET /api/sales/heads/{id}
- **Role:** Salesman
- **Response (200 OK):** `HeadDetailResponse`
- **Error (404 Not Found):** If Head does not exist or is not owned by the salesman.

### POST /api/sales/heads
- **Role:** Salesman
- **Request Body (`CreateHeadRequest`):**
  ```json
  {
    "firstName": "John",
    "lastName": "Doe",
    "type": "customer", // customer, agent
    "mobileNo": "12345678",
    "email": "john.doe@example.com",
    "nationality": "Bahraini",
    "address": "Manama, Bahrain"
  }
  ```
- **Validation:**
  - `firstName` & `lastName`: Required, Max Length 255.
  - `type`: Must be `"customer"` or `"agent"`.
  - `mobileNo`: Required, Max Length 20.
  - `email`: Required, Valid Email, Unique across `Heads`.
  - `nationality`: Required, Max Length 100.
  - `address`: Required.
- **Behavior:** Generates a random `user_id` starting with `TX` + random 3-digit number. If duplicate code found, loops until unique. Sets `balance_amount = 0`, `wallet = 0`.
- **Response (201 Created):** `HeadDetailResponse`
- **Error (400 Bad Request):** If validation fails (via ProblemDetails).

### PUT /api/sales/heads/{id}
- **Role:** Salesman
- **Request Body (`UpdateHeadRequest`):** (Same schema as `CreateHeadRequest`)
- **Behavior:** Updates Head details. Cannot edit financial balances or type.
- **Response (200 OK):** `HeadDetailResponse`
- **Error (404 Not Found):** If Head does not exist or is not owned by the salesman.

### GET /api/sales/heads/{id}/ledger
- **Role:** Salesman
- **Query Parameters:**
  - `page` (int, default=1)
  - `pageSize` (int, default=20)
- **Response (200 OK):** `PagedResponse<HeadLedgerEntryResponse>`

### GET /api/sales/heads/lookup
- **Role:** Salesman
- **Query Parameters:**
  - `search` (string, optional)
  - `limit` (int, default=20)
- **Response (200 OK):** `List<HeadLookupResponse>`

---

## 2. Leads Endpoints

### GET /api/sales/leads
- **Role:** Salesman
- **Query Parameters:**
  - `page` (int, default=1)
  - `pageSize` (int, default=10)
  - `searchTerm` (string, optional)
  - `status` (string, optional)
- **Response (200 OK):** `PagedResponse<LeadSummaryResponse>`

### GET /api/sales/leads/{id}
- **Role:** Salesman
- **Response (200 OK):** `LeadDetailResponse`

### POST /api/sales/leads
- **Role:** Salesman
- **Request Body (`CreateLeadRequest`):**
  ```json
  {
    "name": "Jane Prospect",
    "countryCode": "+973",
    "mobileNo": "33344455",
    "services": ["Flights", "Hotel Booking"],
    "leadSource": "Instagram",
    "description": "Interested in flight + hotel packages to Switzerland",
    "reminderDate": "2026-07-20T12:00:00Z"
  }
  ```
- **Validation:**
  - `name`: Required, Max Length 255.
  - `countryCode`: Required, Max Length 10.
  - `mobileNo`: Required, Max Length 20.
  - `services`: Required, minimum 1 service selection.
  - `leadSource`: Required.
- **Behavior:** Sets initial status to `"Pending"`.
- **Response (201 Created):** `LeadDetailResponse`

### PUT /api/sales/leads/{id}
- **Role:** Salesman
- **Request Body (`UpdateLeadRequest`):**
  ```json
  {
    "name": "Jane Prospect Updated",
    "status": "Followed Up", // Pending, Completed, Followed Up
    "countryCode": "+973",
    "mobileNo": "33344455",
    "services": ["Flights"],
    "leadSource": "Instagram",
    "description": "Followed up on package deals",
    "reminderDate": "2026-07-22T10:00:00Z"
  }
  ```
- **Validation:**
  - `status`: Must be `'Pending'`, `'Completed'`, or `'Followed Up'`.
- **Response (200 OK):** `LeadDetailResponse`

### PATCH /api/sales/leads/{id}/archive
- **Role:** Salesman
- **Behavior:** Sets `DeletedAt = DateTime.UtcNow` (soft delete).
- **Response (204 No Content)**

---

## 3. Receipts Endpoints

### GET /api/sales/receipts
- **Role:** Salesman
- **Query Parameters:**
  - `page` (int, default=1)
  - `pageSize` (int, default=10)
  - `searchTerm` (string, optional)
- **Response (200 OK):** `PagedResponse<ReceiptSummaryResponse>`

### GET /api/sales/receipts/{id}
- **Role:** Salesman
- **Response (200 OK):** `ReceiptDetailResponse`

### GET /api/sales/receipts/booking-options
- **Role:** Salesman
- **Query Parameters:**
  - `headId` (long, required)
  - `service` (string, required)
- **Response (200 OK):**
  ```json
  {
    "options": [
      {
        "id": 12,
        "label": "TXFLI1004 - PNR: G8H3W (2 Pax) - 10 Jul 2026",
        "amount": 250.00
      }
    ],
    "total": 250.0
  }
  ```

### POST /api/sales/receipts
- **Headers:** `Idempotency-Key` (string, required)
- **Request Body (`CreateReceiptRequest`):**
  ```json
  {
    "headId": 1,
    "clientType": "customer", // customer, agent
    "agentName": null,
    "narration": "Payment for flight booking TXFLI1004",
    "service": "flights", // flights, hotels, visas, vfs, holidays, transportations, others, wallet_deposit
    "bookingId": 12,
    "amount": 150.00,
    "paymentMode": "cash", // cash, online, wallet
    "bankId": null,
    "bankName": null
  }
  ```
- **Validation:**
  - `headId` or `agentName` must be provided according to `clientType`.
  - `amount`: Must be greater than 0.
  - `paymentMode`: Must be `"cash"`, `"online"`, or `"wallet"`.
  - `bankId` & `bankName`: Required if `paymentMode` is `"online"`.
- **Behavior:**
  - Creates a Receipt with prefix `INV` + 4-digit code.
  - Initial status: `success` for `wallet`, `collection` for `cash`, `pending` for `online`.
  - If `wallet` mode, deducts wallet balance immediately, records ledger transaction, and allocates booking.
  - Executes in transaction.
- **Response (201 Created):** `ReceiptDetailResponse`

### POST /api/sales/receipts/{id}/status
- **Role:** Salesman
- **Request Body:**
  ```json
  {
    "status": "success" // pending, collection, collected, success
  }
  ```
- **Behavior:** Transitions the receipt status. If transitioning to `success` from other statuses:
  - Executes receipt allocation FIFO or specific booking.
  - Adjusts cash-in-hand / ledgers / balances.
- **Response (200 OK):** `ReceiptDetailResponse`
