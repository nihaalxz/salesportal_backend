# Visa Financial Model

## Source Evidence

**Laravel source**: Not available locally.  
**Primary sources**: `schema_deploy.sql`, `SalesVisaApplication.cs`, `SalesVisaApplicant.cs`, `SalesReadService.cs`.

---

## Financial Authority Level

Both the **application** and **applicant** levels store financial fields:

### Application Level (`sales_visa_applications`)
```sql
[payable_amount]          decimal(15,3) NOT NULL  -- aggregate
[expected_supplier_cost]  decimal(15,3) NOT NULL  -- aggregate
[profit]                  decimal(15,3) NOT NULL  -- aggregate
```

### Applicant Level (`sales_visa_applicants`)
```sql
[payable_amount]          decimal(15,3) NOT NULL  -- per applicant
[supplier_cost]           decimal(15,3) NOT NULL  -- actual cost (post-booking)
[expected_supplier_cost]  decimal(15,3) NOT NULL  -- per applicant
[profit]                  decimal(15,3) NOT NULL  -- per applicant
[is_inhouse]              bit NOT NULL
[payment_status]          nvarchar(255) NOT NULL  -- per applicant
[payment_method]          nvarchar(255) NULL       -- per applicant
[govt_fee_amount]         decimal(15,3) NULL
[govt_fee_status]         nvarchar(255) NOT NULL
```

---

## Payment Authority for Phase 11 Creation

On initial booking creation, payment deduction operates at the **application level**:

- `paymentMethod` and `paymentStatus` at application level govern the ledger and financial mutations.
- Applicant-level `payment_status` defaults to `"pending"` on creation.
- Applicant-level `payment_method` is `null` on creation (assigned per individual payment workflow later).

This avoids complexity of per-applicant payment during creation while preserving the schema structure for future individual applicant payment reconciliation.

---

## Application Aggregate Calculations (Server-Authoritative)

```
Application.PayableAmount         = SUM(applicant.payableAmount)
Application.ExpectedSupplierCost  = SUM(applicant.expectedSupplierCost)  -- non-inhouse only
Application.Profit                = Application.PayableAmount - Application.ExpectedSupplierCost
```

Client-submitted aggregate amounts are ignored. Server recalculates from validated applicant inputs.

---

## Government Fee Behavior

- `sales_visa_govt_fees` is linked per applicant (`sales_visa_applicant_id`).
- On creation in Phase 11, govt fees are NOT created during booking write.
- Govt fees are managed in a separate workflow (payment tracking, updates).
- `applicant.govt_fee_status` defaults to `"pending"` on creation.

---

## Ledger Behavior

Single ledger entry per application:
```
HeadLedger.ServiceType = "Visa"
HeadLedger.ServiceId   = application.Id
HeadLedger.Debit       = Application.PayableAmount (aggregate)
HeadLedger.Credit      = advance paid (0 for credit, aggregate for cash/wallet/online)
```

---

## Head Wallet Deduction

Application-level payable amount deducted atomically when `paymentMethod == "wallet"`.

---

## Salesman Profit Update

```
totalProfit = Application.Profit (non-inhouse case)
if totalProfit > 0: Users.ProfitGenerated += totalProfit
```

For inhouse applications: `expectedSupplierCost = 0`, `profit = 0`.

---

## Tests Required

- [ ] Application aggregate = SUM(applicant amounts)
- [ ] Server recalculates, does not trust client aggregate
- [ ] Applicants created with `payment_status = "pending"`
- [ ] Ledger entry uses application-level payable amount
- [ ] Wallet deducted by application payable amount
- [ ] Head balance for credit
- [ ] Salesman profit = application profit
