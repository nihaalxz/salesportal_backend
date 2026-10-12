# Laravel to .NET Mapping Guide

This guide maps each Laravel component (controllers, models, services, middleware) to its modern C# ASP.NET Core equivalent.

---

## 1. Controller and Endpoint Mapping

| Laravel Controller | .NET Controller | Route & HTTP Method | Purpose |
| :--- | :--- | :--- | :--- |
| `DashboardController` | `DashboardController` | `GET /api/sales/dashboard/summary` | Dashboard summary metrics |
| | | `GET /api/sales/dashboard/revenue-trend` | 7-day revenue trend data |
| | | `GET /api/sales/dashboard/booking-statistics` | Service count metrics |
| | | `GET /api/sales/dashboard/service-distribution` | Distribution chart details |
| `HeadController`, `HeadApiController` | `HeadsController` | `GET /api/heads` | Search/list heads |
| | | `POST /api/heads` | Create new head |
| | | `GET /api/heads/{id}` | Head details & balance |
| `ReceiptController`, `ReceiptApiController` | `ReceiptsController` | `GET /api/receipts` | List all receipts |
| | | `POST /api/receipts` | Create payment receipt |
| | | `PUT /api/receipts/{id}/status` | Validate receipt status |
| `FlightBookingController`, `FlightBookingApiController` | `FlightBookingsController` | `GET /api/flight-bookings` | List flight bookings |
| | | `POST /api/flight-bookings` | Book flight & passengers |
| `InHouseCostSyncController` | `SyncController` | `POST /api/services/sync-actual-cost` | Operations actual cost sync |
| `StaffSyncController` | `SyncController` | `POST /api/sync-staff` | Operations staff profile sync |

---

## 2. Models to Entity Mapping

All .NET entity classes belong to `TexasPortal.Domain.Entities` and map to database tables via EF Core DbContext configurations in `TexasPortal.Infrastructure.Persistence.Configurations`.

| Laravel Model | .NET Entity | Db Table | Key Properties & Types |
| :--- | :--- | :--- | :--- |
| `User` | `User` | `users` | `Id` (long), `ProfitTarget` (decimal), `ProfitGenerated` (decimal) |
| `Head` | `Head` | `heads` | `Id` (long), `Balance` (decimal), `Wallet` (decimal), `UserId` (string) |
| `HeadLedger` | `HeadLedger` | `head_ledgers` | `Id` (long), `Debit` (decimal), `Credit` (decimal), `ServiceType` (string) |
| `Receipt` | `Receipt` | `receipts` | `Id` (long), `Amount` (decimal), `Status` (string), `PaymentMode` (string) |
| `FlightBooking` | `FlightBooking` | `flight_bookings` | `Id` (long), `PayableAmount` (decimal), `PaymentStatus` (string) |
| `FlightPassenger` | `FlightPassenger` | `flight_passengers` | `Id` (long), `PayableAmount` (decimal), `ExpectedSupplierCost` (decimal) |
| `HotelBooking` | `HotelBooking` | `hotel_bookings` | `Id` (long), `PayableAmount` (decimal), `ExpectedSupplierCost` (decimal) |
| `SalesVisaApplication`| `SalesVisaApplication`| `sales_visa_applications` | `Id` (long), `PayableAmount` (decimal), `VisaPackId` (int) |
| `SalesVisaApplicant` | `SalesVisaApplicant` | `sales_visa_applicants` | `Id` (long), `PayableAmount` (decimal), `FName` (string) |
| `VfsAppointment` | `VfsAppointment` | `vfs_appointments` | `Id` (long), `PayableAmount` (decimal), `VfsId` (string) |
| `HolidayPackage` | `HolidayPackage` | `holiday_packages` | `Id` (long), `PayableAmount` (decimal), `BookingId` (string) |
| `Transportation` | `Transportation` | `transportations` | `Id` (long), `PayableAmount` (decimal), `Pickup` (string) |
| `OtherService` | `OtherService` | `other_services` | `Id` (long), `PayableAmount` (decimal), `ServiceName` (string) |
| `CashInHand` | `CashInHand` | `cash_in_hands` | `Id` (long), `UserId` (long), `Amount` (decimal) |
| `Deposit` | `Deposit` | `deposits` | `Id` (long), `Amount` (decimal), `Status` (string), `DestinationId` (long) |
| `StaffMonthlyRecord` | `StaffMonthlyRecord` | `staff_monthly_records` | `Id` (long), `Month` (string), `Year` (int) |
| `Lead` | `Lead` | `leads` | `Id` (long), `MobileNo` (string), `Status` (string) |
| `Dispute` | `Dispute` | `disputes` | `Id` (long), `Subject` (string), `RefundAmount` (decimal) |
| `SalesVisaGovtFee` | `SalesVisaGovtFee` | `sales_visa_govt_fees` | `Id` (long), `Amount` (decimal), `Status` (string) |
| `VfsAppointmentGovtFee`| `VfsAppointmentGovtFee`| `vfs_appointment_govt_fees` | `Id` (long), `Amount` (decimal), `Status` (string) |
| `HolidayPackageGovtFee`| `HolidayPackageGovtFee`| `holiday_package_govt_fees` | `Id` (long), `Amount` (decimal), `Status` (string) |
| `HolidayPackageDocument`| `HolidayPackageDocument`| `holiday_package_documents` | `Id` (long), `DocumentPath` (string), `DocumentType` (string) |
| - | `Notification` | `notifications` | `Id` (string), `Type` (string), `NotifiableId` (long) |

---

## 3. Services Mapping

All business logic resides in `TexasPortal.Application`.

- **`BookingPaymentService.php`** → `TexasPortal.Application.Payments.Services.BookingPaymentService`
  - Handles verifying receipts, allocating funds to bookings, updating ledger balances, and syncing with Accounts Portal.
- **`HeadLedgerService.php`** → `TexasPortal.Application.Heads.Services.HeadLedgerService`
  - Handles ledger posting operations, debit/credit updates, and head balance recalculations.
- **`AccountsApiService.php`** → `TexasPortal.Infrastructure.Integrations.AccountsApiClient` (Typed `HttpClient`)
  - Outbound agent details, balance adjustments, and bank/account deposit transfers.
- **`CashInHandService.php`** → `TexasPortal.Application.Cash.Services.CashInHandService`
  - Fetching individual and team physical cash balances.

---

## 4. Middleware Mapping

- **`ValidateApiKey.php`** → `TexasPortal.Api.Middleware.ApiKeyAuthMiddleware`
  - Inspects request header `X-OPS-KEY` and validates against API Key options.
- **`auth:sanctum`** → `Microsoft.AspNetCore.Authentication.JwtBearer`
  - Standard token authentication for authenticated clients.
