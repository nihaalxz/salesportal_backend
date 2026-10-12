# Remaining Booking Services Analysis

## Source Evidence

**Laravel source**: Not available locally.  
**Primary sources**: `schema_deploy.sql`, domain entities, `SalesReadService.cs`, `DevSeeder.cs`.

---

## Hotel Booking

**Route**: `POST /api/sales/bookings/hotels`  
**Ownership**: `user_id` (IUserOwned)  

### Fields
| Field | Required | Notes |
|---|---|---|
| GuestName | ✅ | |
| Country | ✅ | |
| CheckInDate | ✅ | |
| Days | ✅ | > 0 |
| PaymentMethod | ✅ | cash/credit/wallet/online |
| Amount (PayableAmount) | ✅ | >= 0 |
| ClientType | ✅ | customer/agent |
| HeadId | conditional | required when clientType=customer |
| Agent | conditional | required when clientType=agent |
| AgentId | conditional | required when clientType=agent |
| IsInhouse | ✅ | determines supplier requirement |
| SupplierId | conditional | required when !isInhouse |
| ExpectedSupplierCost | conditional | required when !isInhouse |
| Prefix | ❌ | |
| BankId | conditional | required when paymentMethod=online |
| BankName | ❌ | |
| PaymentProof | ❌ | file path |

### Financial Pattern
Identical to OtherService. `profit = payableAmount - expectedSupplierCost`. Stored at booking level.

### OrderStatus
Created with `"pending"`. Managed separately in booking workflow updates.

---

## Flight Booking

**Route**: `POST /api/sales/bookings/flights`  
**Ownership**: `user_id` (IUserOwned)  

### Fields (Booking Level)
| Field | Required | Notes |
|---|---|---|
| FromLocation | ✅ | |
| ToLocation | ✅ | |
| PaymentMethod | ✅ | cash/credit/wallet/online |
| ClientType | ✅ | customer/agent |
| HeadId | conditional | |
| AgentId | conditional | |
| Passengers | ✅ | >= 1 passenger |

### Fields (Passenger Level)
| Field | Required | Notes |
|---|---|---|
| PassengerName | ✅ | |
| PayableAmount | ✅ | |
| ExpectedSupplierCost | ✅ (non-inhouse) | |
| IsInhouse | ✅ | |
| SupplierId | conditional | required when !isInhouse |
| Prefix | ❌ | |

### Financial Pattern
No booking-level profit. Per-passenger profit. See `flight-financial-model.md`.

---

## Visa Application

**Route**: `POST /api/sales/bookings/visas`  
**Ownership**: `user_id` (IUserOwned)  

### Fields (Application Level)
| Field | Required | Notes |
|---|---|---|
| CountryId | ✅ | integer |
| VisaPackId | ✅ | integer |
| VisaPackName | ❌ | |
| ClientType | ✅ | |
| PaymentMethod (application-level) | ✅ | |
| HeadId | conditional | |
| AgentId | conditional | |
| Applicants | ✅ | >= 1 |

### Fields (Applicant Level)
| Field | Required | Notes |
|---|---|---|
| FName | ✅ | |
| LName | ✅ | |
| Nationality | ✅ | |
| ContactNo | ✅ | |
| EmailId | ✅ | |
| PassportNo | ✅ | |
| PayableAmount | ✅ | |
| ExpectedSupplierCost | conditional | non-inhouse |
| IsInhouse | ✅ | |
| SupplierId | conditional | |
| Prefix | ❌ | |
| Dob | ❌ | |
| IqamaHotelNo | ❌ | |

### Financial Pattern
See `visa-financial-model.md`. Application aggregate computed from applicants.

---

## VFS Appointment

**Route**: `POST /api/sales/bookings/vfs`  
**Ownership**: `created_by` (ICreatorOwned)  

### Fields
| Field | Required | Notes |
|---|---|---|
| VfsId | server-generated | UNIQUE index — auto-generated |
| Prefix | ✅ | |
| Name | ✅ | |
| Supplier | ✅ | name string |
| SupplierId | ❌ | |
| Country | ✅ | |
| VisaType | ✅ | |
| PayableAmount | ✅ | |
| ExpectedSupplierCost | ✅ | |
| PaymentMode | ✅ | cash/credit/wallet |
| AppointmentDate | ❌ | |
| AppointmentTime | ❌ | |
| ClientType | ✅ | |
| HeadId | conditional | |
| AgentId | conditional | |

### Financial Pattern
Identical to OtherService (booking-level profit). Always external (no inhouse).

### VfsId Generation
`VFS-{random alphanumeric}` — unique, server-generated, verified against DB index.

---

## Holiday Package

**Route**: `POST /api/sales/bookings/holidays`  
**Ownership**: `created_by` (ICreatorOwned)  

### Fields
| Field | Required | Notes |
|---|---|---|
| BookingId | server-generated | UNIQUE index |
| Prefix | ✅ | |
| PassengerName | ✅ | |
| Supplier | ✅ | |
| SupplierId | ❌ | |
| Country | ✅ | |
| Days | ✅ | |
| PayableAmount | ✅ | |
| ExpectedSupplierCost | ✅ | |
| PaymentMode | ✅ | cash/credit/wallet |
| ClientType | ✅ | |
| HeadId | conditional | |
| AgentId | conditional | |

### Financial Pattern
Booking-level profit. Always external supplier (no inhouse).

### BookingId Generation
`HP-{random alphanumeric}` — unique, server-generated.

---

## Transport Booking

**Route**: `POST /api/sales/bookings/transport`  
**Ownership**: `created_by` (ICreatorOwned)  

### Fields
| Field | Required | Notes |
|---|---|---|
| Name | ✅ | passenger name |
| Pickup | ✅ | |
| Drop | ✅ | |
| DropDate | ❌ | |
| DropTime | ❌ | |
| Vehicle | ❌ | |
| PaymentMethod | ✅ | cash/credit/wallet |
| PayableAmount | ✅ | |
| ExpectedSupplierCost | ✅ (non-inhouse) | |
| IsInhouse (inferred from SupplierId) | ❌ | |
| SupplierId | ❌ | |
| Prefix | ❌ | |
| ClientType | ✅ | |
| HeadId | conditional | |
| AgentId | conditional | |

### Financial Pattern
Identical to OtherService. Booking-level profit.
