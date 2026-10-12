# Booking Financial Invariants

## Scope

These invariants apply to ALL seven booking services: Other, Hotel, Flight, Visa, VFS, Holiday, Transport.

---

## 1. PayableAmount Invariant

**Precondition**: Client submits booking request with amount field(s).  
**Server Behavior**: Server recalculates authoritative payable amount. Client-submitted aggregate (if any) is disregarded.  
- For passenger/applicant services: `payableAmount = SUM(child.payableAmount)`.  
- For single-record services: `payableAmount = request.Amount`.  
**Postcondition**: `booking.PayableAmount == server-calculated value`.

## 2. Profit Invariant

**Rule**: `profit = payableAmount - expectedSupplierCost`.  
- Inhouse bookings: `profit = 0`, `expectedSupplierCost = 0`.  
**Storage**: Flight — per passenger only. All others — booking record.  
**Postcondition**: `booking.Profit + booking.ExpectedSupplierCost == booking.PayableAmount` (non-inhouse).

## 3. Advance Paid Invariant

**Rule**: `advancePaid = payableAmount` when `paymentMethod ∈ {cash, wallet, online}`. `advancePaid = 0` when `paymentMethod == credit`.

## 4. Ledger Invariant

- Exactly ONE `HeadLedger` entry per booking (enforced by unique index `(service_type, service_id)`).  
- `ledger.Debit == booking.PayableAmount`.  
- `ledger.Credit == advancePaid`.

## 5. Cash Payment Invariant

- `CashInHands.Amount += payableAmount` (upsert).  
- Rolled back if transaction fails.

## 6. Credit Payment Invariant

- `Head.Balance += payableAmount`. `Head.BalanceAmount += payableAmount`.  
- Atomic `ExecuteUpdateAsync`.

## 7. Wallet Payment Invariant

- `Head.Wallet -= payableAmount` (atomic WHERE wallet >= amount guard).  
- If `affected == 0`: return `InsufficientWallet`. Transaction rolls back.

## 8. Salesman Profit Invariant

- `User.ProfitGenerated += profit` (Flight: SUM of passenger profits; others: booking profit).  
- Only when `profit > 0 && !isInhouse`.

## 9. Idempotency Invariant

- Same key + same payload hash → replay cached response.  
- Same key + different payload hash → `409 Conflict`.  
- Status `InProgress` → `409 Conflict`.

## 10. Rollback Invariant

All DB mutations within `ITransactionalExecutor.ExecuteAsync` are rolled back atomically on any exception.

## Financial Safety Rules

- All financial fields use `decimal`. Never `float` or `double`.
- Server recalculates all totals from validated sub-fields.
- Per-passenger and per-applicant amounts validated individually.
