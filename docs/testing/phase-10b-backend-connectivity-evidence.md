# Phase 10B Backend Connectivity Evidence

This document provides verified evidence of the complete SvelteKit-to-Database read path execution, compiled dynamically during integration testing against the SQL Server database.

**Generated**: 2026-11-21 19:54:20 UTC

## Database Diagnostic State

### Users in Database
| Id | Name | Email | UserType |
|---|---|---|---|
| 1 | John Salesman | john.sales@texastravelsbh.com | Salesman |
| 2 | Sarah Salesman | sarah.sales@texastravelsbh.com | Salesman |
| 3 | Guest User | guest@texastravelsbh.com | Guest |

### Customer Heads in Database
| Id | FirstName | LastName | CreatedBy |
|---|---|---|---|
| 1 | Alice | A_Head | 1 |
| 2 | Bob | B_Head | 2 |

### Hotel Bookings in Database
| Id | GuestName | UserId | HeadId | PayableAmount |
|---|---|---|---|---|
| 1 | Alice | 1 | 1 | 100.000 |
| 2 | Bob | 2 | 2 | 480.000 |

---

### 1. Dashboard Summary as Salesman A

- **Route**: `/api/sales/dashboard/summary`
- **Auth Identity**: `dev-token-salesman-a`
- **HTTP Status**: `200 OK`
- **JSON Response Payload**:
```json
{
  "totalFlightBookings": 1,
  "totalHotelBookings": 1,
  "totalVisaApplications": 0,
  "totalVfsAppointments": 0,
  "totalHolidayPackages": 0,
  "totalTransportations": 0,
  "totalOtherServices": 0,
  "totalPayableAmount": 350.750,
  "totalPaidAmount": 0,
  "totalPendingAmount": 0,
  "totalProfit": 0.000,
  "cashInHand": 350.250,
  "activeLeads": 1,
  "totalHeads": 1,
  "totalReceivables": 0.000,
  "totalCustomerWallets": 200.000,
  "pendingReceipts": 0,
  "pendingApplications": 1
}
```

---

### 2. Dashboard Summary as Salesman B

- **Route**: `/api/sales/dashboard/summary`
- **Auth Identity**: `dev-token-salesman-b`
- **HTTP Status**: `200 OK`
- **JSON Response Payload**:
```json
{
  "totalFlightBookings": 0,
  "totalHotelBookings": 1,
  "totalVisaApplications": 0,
  "totalVfsAppointments": 0,
  "totalHolidayPackages": 0,
  "totalTransportations": 0,
  "totalOtherServices": 0,
  "totalPayableAmount": 480.000,
  "totalPaidAmount": 0,
  "totalPendingAmount": 0,
  "totalProfit": 0.000,
  "cashInHand": 120.500,
  "activeLeads": 1,
  "totalHeads": 1,
  "totalReceivables": 0.000,
  "totalCustomerWallets": 50.000,
  "pendingReceipts": 0,
  "pendingApplications": 0
}
```

---

### 3. Heads List as Salesman A

- **Route**: `/api/sales/heads`
- **Auth Identity**: `dev-token-salesman-a`
- **HTTP Status**: `200 OK`
- **JSON Response Payload**:
```json
{
  "items": [
    {
      "id": 1,
      "firstName": "Alice",
      "lastName": "A_Head",
      "email": "alice.a@example.com",
      "mobileNo": "97311111111",
      "nationality": "Bahraini",
      "type": "customer",
      "status": "active",
      "wallet": 200.000,
      "address": null,
      "salesmanId": null,
      "salesmanName": null,
      "createdAt": null
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

---

### 4. Heads List as Salesman B

- **Route**: `/api/sales/heads`
- **Auth Identity**: `dev-token-salesman-b`
- **HTTP Status**: `200 OK`
- **JSON Response Payload**:
```json
{
  "items": [
    {
      "id": 2,
      "firstName": "Bob",
      "lastName": "B_Head",
      "email": "bob.b@example.com",
      "mobileNo": "97322222222",
      "nationality": "Saudi",
      "type": "customer",
      "status": "active",
      "wallet": 50.000,
      "address": null,
      "salesmanId": null,
      "salesmanName": null,
      "createdAt": null
    },
    {
      "id": 1,
      "firstName": "Muhammed",
      "lastName": "Kt",
      "email": "zakirhussain@gmail.com",
      "mobileNo": "\u002B91 7034183646",
      "nationality": "Bahraini",
      "type": "agent",
      "status": "active",
      "wallet": 164.000,
      "address": null,
      "salesmanId": null,
      "salesmanName": null,
      "createdAt": null
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 2,
  "totalPages": 1,
  "hasNextPage": false,
  "hasPreviousPage": false
}
```

---

### 5. A-Owned Head Details as Salesman A

- **Route**: `/api/sales/heads/1`
- **Auth Identity**: `dev-token-salesman-a`
- **HTTP Status**: `200 OK`
- **JSON Response Payload**:
```json
{
  "address": "Manama Block 312",
  "id": 1,
  "firstName": "Alice",
  "lastName": "A_Head",
  "email": "alice.a@example.com",
  "mobileNo": "97311111111",
  "nationality": "Bahraini",
  "type": "customer",
  "status": "active",
  "wallet": 200.000,
  "salesmanId": null,
  "salesmanName": null,
  "createdAt": null
}
```

---

### 6. B-Owned Head Details as Salesman B

- **Route**: `/api/sales/heads/2`
- **Auth Identity**: `dev-token-salesman-b`
- **HTTP Status**: `200 OK`
- **JSON Response Payload**:
```json
{
  "address": "Khobar Main St",
  "id": 2,
  "firstName": "Bob",
  "lastName": "B_Head",
  "email": "bob.b@example.com",
  "mobileNo": "97322222222",
  "nationality": "Saudi",
  "type": "customer",
  "status": "active",
  "wallet": 50.000,
  "salesmanId": null,
  "salesmanName": null,
  "createdAt": null
}
```

---

### 7. A-Owned Head Details requested by Salesman B

- **Route**: `/api/sales/heads/1`
- **Auth Identity**: `dev-token-salesman-b`
- **HTTP Status**: `200 OK`
- **JSON Response Payload**:
```json
{
  "address": "N/A",
  "id": 1,
  "firstName": "Muhammed",
  "lastName": "Kt",
  "email": "zakirhussain@gmail.com",
  "mobileNo": "\u002B91 7034183646",
  "nationality": "Bahraini",
  "type": "agent",
  "status": "active",
  "wallet": 164.000,
  "salesmanId": null,
  "salesmanName": null,
  "createdAt": "2026-11-21T19:54:22.1371907Z"
}
```

---

### 8. B-Owned Head Details requested by Salesman A

- **Route**: `/api/sales/heads/2`
- **Auth Identity**: `dev-token-salesman-a`
- **HTTP Status**: `404 NotFound`
- **JSON Response Payload**:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "traceId": "00-61868809863341725af1201c0a360897-f2341156169eab03-00"
}
```

---

### 9. A-Owned Head Ledger as Salesman A

- **Route**: `/api/sales/heads/1/ledger`
- **Auth Identity**: `dev-token-salesman-a`
- **HTTP Status**: `200 OK`
- **JSON Response Payload**:
```json
{
  "items": [
    {
      "id": 1,
      "serviceId": 1,
      "type": "Flight",
      "description": "Flight Ticket Booking TX-FL-90210",
      "debit": 250.750,
      "credit": 0,
      "balance": -250.750,
      "paymentMethod": "cash",
      "createdAt": "2026-11-16T19:54:20.6031733"
    },
    {
      "id": 2,
      "serviceId": 1,
      "type": "Receipt",
      "description": "Deposit payment for flights",
      "debit": 0.000,
      "credit": 100.000,
      "balance": 100.000,
      "paymentMethod": null,
      "createdAt": "2026-11-17T19:54:20.6205867"
    },
    {
      "id": 1,
      "serviceId": 1,
      "type": "hotel_booking",
      "description": "Hotel Booking #1: Alice (UAE)",
      "debit": 100.000,
      "credit": 0,
      "balance": -100.000,
      "paymentMethod": "cash",
      "createdAt": "2026-11-20T19:54:20.6673454"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 3,
  "totalPages": 1,
  "hasNextPage": false,
  "hasPreviousPage": false
}
```

---

### 10. B-Owned Head Ledger as Salesman B

- **Route**: `/api/sales/heads/2/ledger`
- **Auth Identity**: `dev-token-salesman-b`
- **HTTP Status**: `200 OK`
- **JSON Response Payload**:
```json
{
  "items": [
    {
      "id": 3,
      "serviceId": 1,
      "type": "Hotel",
      "description": "Hotel Booking TX-HT-44810",
      "debit": 480.000,
      "credit": 0,
      "balance": -480.000,
      "paymentMethod": null,
      "createdAt": "2026-11-18T19:54:20.6207766"
    },
    {
      "id": 2,
      "serviceId": 2,
      "type": "hotel_booking",
      "description": "Hotel Booking #2: Bob (Bahrain)",
      "debit": 480.000,
      "credit": 0,
      "balance": -480.000,
      "paymentMethod": "bank",
      "createdAt": "2026-11-20T19:54:20.7108966"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 2,
  "totalPages": 1,
  "hasNextPage": false,
  "hasPreviousPage": false
}
```

---

### 11. A-Owned Head Ledger requested by Salesman B

- **Route**: `/api/sales/heads/1/ledger`
- **Auth Identity**: `dev-token-salesman-b`
- **HTTP Status**: `200 OK`
- **JSON Response Payload**:
```json
{
  "items": [
    {
      "id": 1,
      "serviceId": 1,
      "type": "Flight",
      "description": "Flight Ticket Booking TX-FL-90210",
      "debit": 250.750,
      "credit": 0,
      "balance": -250.750,
      "paymentMethod": "cash",
      "createdAt": "2026-11-16T19:54:20.6031733"
    },
    {
      "id": 2,
      "serviceId": 1,
      "type": "Receipt",
      "description": "Deposit payment for flights",
      "debit": 0.000,
      "credit": 100.000,
      "balance": 100.000,
      "paymentMethod": null,
      "createdAt": "2026-11-17T19:54:20.6205867"
    },
    {
      "id": 1,
      "serviceId": 1,
      "type": "hotel_booking",
      "description": "Hotel Booking #1: Alice (UAE)",
      "debit": 100.000,
      "credit": 0,
      "balance": -100.000,
      "paymentMethod": "cash",
      "createdAt": "2026-11-20T19:54:20.6673454"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 3,
  "totalPages": 1,
  "hasNextPage": false,
  "hasPreviousPage": false
}
```

---

### 12. Outstanding Bookings (Non-existent Endpoint)

- **Route**: `/api/sales/heads/1/bookings/outstanding`
- **Auth Identity**: `dev-token-salesman-a`
- **HTTP Status**: `200 OK`
- **JSON Response Payload**:
```json
[]
```

---

