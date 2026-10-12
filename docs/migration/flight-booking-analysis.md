# Flight Booking Analysis

## Mapped Laravel Source Details

### Laravel Route
- `GET /flights` -> index
- `GET /flights/create` -> create
- `POST /flights/store` -> store
- `GET /flights/{flightBooking}` -> show

### Controller Action
- `App\Http\Controllers\FlightBookingController@store`

### Service Method
- `App\Services\FlightBookingService@createBooking`

### Booking Table
- `flight_bookings`

### Passenger Table
- `flight_passengers`

### Required Fields
- `client_type` ("customer" | "agent")
- `head_id` (required if client_type is customer)
- `agent` (required if client_type is agent and no agentId is resolved)
- `is_inhouse` (boolean)
- `supplier_id` (required if non-inhouse)
- `from_location` (string)
- `to_location` (string)
- `payment_method` ("cash" | "wallet" | "credit" | "online")
- `bank_id` (required if payment_method is online)
- `passengers` (array, min 1)
- `passengers.*.passenger_name` (string)
- `passengers.*.expected_supplier_cost` (required if non-inhouse)
- `passengers.*.amount` (required, numeric, min 0.001)

### Optional Fields
- `care_of` (string)
- `bank_name` (string)
- `passengers.*.prefix` (string)
- `payment_proof` (file)

### Payment Methods
- `cash`, `credit`, `wallet`, `online`

### Ledger Debit Rule
- `Debit = totalPayable` (the sum of passenger amounts)

### Ledger Credit Rule
- `Credit = totalPayable` if payment method is `cash`, `wallet`, or `online`.
- `Credit = 0` if payment method is `credit`.

### CashInHand Rule
- Incremented by `totalPayable` if `payment_method` is `cash`.

### Head Balance Rule
- Incremented by `totalPayable - advancePaid` (i.e. `totalPayable` for `credit`, `0` for others) under `recordTransaction`.

### Head Wallet Rule
- Decremented by `totalPayable` if `payment_method` is `wallet`. Enforced via atomic DB mutation.

### Passenger Profit Rule
- `Profit = Passenger.Amount - Passenger.ExpectedSupplierCost` if non-inhouse.
- `Profit = 0` if inhouse.

### Aggregate Profit Rule
- `SUM(Passenger.Profit)` — derived at query time, not stored in `flight_bookings` table.

### Salesman Profit Rule
- Incremented by booking-level aggregate profit (if > 0 and non-inhouse).

### In-house Rule
- If `is_inhouse` is true:
  - `supplier_id = null`
  - `expected_supplier_cost = 0`
  - `profit = 0` (for passenger and salesman)

### Supplier Rule
- supplierId and supplierName stored at passenger level on `flight_passengers`.

### Status Lifecycle
- `payment_status`: `"success"` if payment method is `wallet` and wallet is sufficient; otherwise `"pending"`.
- `processing_status`: `"success"`.

### Validation Rules
- Enforce non-negative amounts, positive passenger amounts, valid HeadId, etc.

---

## Required .NET Improvements
- Move logic to Mediator/command handler from controller.
- Propagate cancellation tokens.
- DB-level concurrency safety using atomic `ExecuteUpdateAsync` updates.
- Standardized ProblemDetails returning validation errors.
