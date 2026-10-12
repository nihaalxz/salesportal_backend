# Other Service Write API Contract

This document specifies the HTTP endpoint and DTO models for creating Other Service bookings.

---

## 1. POST `/api/sales/bookings/other`

- **HTTP Method**: `POST`
- **Security**: Authentication Required (`Salesman` role context)
- **Headers**:
  - `Idempotency-Key` (string: 36-char GUID required, prevents duplicate creations).
- **Request Body (JSON)**:
  ```json
  {
    "clientType": "customer",
    "headId": 1,
    "prefix": "Mr.",
    "name": "Jane Doe",
    "isInhouse": false,
    "supplierId": 4,
    "expectedSupplierCost": 50.000,
    "amount": 75.000,
    "serviceName": "Translation Services",
    "narration": "Urgent visa document translation",
    "paymentMethod": "cash",
    "status": "pending",
    "paymentProof": null
  }
  ```

- **Success Response (201 Created)**:
  * **Headers**: `Location: /api/sales/bookings/other/{id}`
  * **Body**:
    ```json
    {
      "id": 12,
      "payableAmount": 75.000,
      "expectedSupplierCost": 50.000,
      "profit": 25.000,
      "paymentStatus": "pending",
      "processingStatus": "pending"
    }
    ```

- **Error Responses**:
  - **400 Bad Request**: Validation failure (empty names, negative amounts, invalid enum values) returning RFC 9457 `ValidationProblemDetails`.
  - **409 Conflict**: Idempotency Key collision (returning detailed error context).
  - **422 Unprocessable Entity**: Insufficient wallet balance for wallet payment method.
