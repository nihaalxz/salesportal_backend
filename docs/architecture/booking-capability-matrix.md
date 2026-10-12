# Booking Capability Matrix

| Capability | Other | Flight | Hotel | Visa | VFS | Holiday | Transport |
|---|---|---|---|---|---|---|---|
| **Head** | Customer required; agent optional | Optional | Optional | Optional | Optional | Optional | Optional |
| **Supplier** | Optional (inhouse flag) | `supplier` text field (optional) | Optional (inhouse flag) | Optional (inhouse per-applicant) | Required (`supplier` field) | Required (`supplier` field) | Optional |
| **Child Records** | — | FlightPassenger[] | — | SalesVisaApplicant[] | VfsAppointmentGovtFee[] | HolidayPackageGovtFee[], Document[] | — |
| **Profit storage** | booking | per passenger | booking | application + per applicant | booking | booking | booking |
| **Inhouse mode** | ✅ | ✅ per-passenger | ✅ | ✅ per-applicant | ❌ always external | ❌ always external | ✅ |
| **Unique ID** | — | — | — | — | `vfs_id` (UNIQUE) | `booking_id` (UNIQUE) | — |
| **OrderStatus** | — | — | ✅ | — | — | — | — |
| **Appointment date** | — | — | CheckInDate | — | AppointmentDate/Time | — | DropDate/Time |
| **Country** | — | From/To location | Country | CountryId | Country | Country | — |
| **Days** | — | — | ✅ | — | — | ✅ | — |
| **Payment: cash** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Payment: credit** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Payment: wallet** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Payment: online** | ✅ (bankId req.) | ✅ | ✅ (bankId req.) | — | — | — | — |
| **CashInHand** | ✅ (cash only) | ✅ (cash only) | ✅ (cash only) | ✅ (cash only) | ✅ (cash only) | ✅ (cash only) | ✅ (cash only) |
| **HeadLedger** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Head Wallet** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Head Balance** | ✅ (credit) | ✅ (credit) | ✅ (credit) | ✅ (credit) | ✅ (credit) | ✅ (credit) | ✅ (credit) |
| **Salesman Profit** | ✅ | ✅ (passenger sum) | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Idempotency** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Concurrency protection** | Wallet: ExecuteUpdateAsync | Wallet: ExecuteUpdateAsync | Wallet: ExecuteUpdateAsync | Wallet: ExecuteUpdateAsync | Wallet: ExecuteUpdateAsync | Wallet: ExecuteUpdateAsync | Wallet: ExecuteUpdateAsync |
| **Payment field name** | `paymentMethod` | `paymentMethod` | `paymentMethod` | (app-level) | `paymentMode` | `paymentMode` | `paymentMethod` |

---

## Ownership Tracking Column

| Service | Column | Entity Interface |
|---|---|---|
| OtherService | `created_by` | `ICreatorOwned` |
| FlightBooking | `user_id` | `IUserOwned` |
| HotelBooking | `user_id` | `IUserOwned` |
| SalesVisaApplication | `user_id` | `IUserOwned` |
| VfsAppointment | `created_by` | `ICreatorOwned` |
| HolidayPackage | `created_by` | `ICreatorOwned` |
| Transportation | `created_by` | `ICreatorOwned` |

**Critical**: Write handlers must set the correct ownership column.

---

## Notes on Unique Constraints

- `vfs_appointments.vfs_id`: UNIQUE index. Handler must generate unique VFS ID (e.g., `VFS-{guid-short}`).
- `holiday_packages.booking_id`: UNIQUE index. Handler must generate unique booking ID (e.g., `HP-{guid-short}`).
- `head_ledgers.(service_type, service_id)`: UNIQUE index. One ledger entry per booking per service type — enforced by DB.
