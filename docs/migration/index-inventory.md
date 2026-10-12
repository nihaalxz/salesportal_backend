# Index Inventory

This document inventory details all indexes, classified by purpose and constraint rules.

---

## 1. Index Classifications

### SOURCE-EXISTING
- **`users_email_unique`**:
  - Columns: `email` (Unique)
  - Purpose: Enforce user email uniqueness in authentication.
- **`heads_user_id_unique`**:
  - Columns: `user_id` (Unique)
  - Purpose: Unique agent identifier validation.
- **`heads_email_unique`**:
  - Columns: `email` (Unique)
  - Purpose: Unique customer/head email.
- **`receipts_receipt_id_unique`**:
  - Columns: `receipt_id` (Unique)
  - Purpose: Prevent double-receipt record insertions.
- **`vfs_appointments_vfs_id_unique`**:
  - Columns: `vfs_id` (Unique)
- **`holiday_packages_booking_id_unique`**:
  - Columns: `booking_id` (Unique)
- **`head_ledgers_head_id_index`**:
  - Columns: `head_id` (Non-unique)
  - Purpose: Speeds up customer ledger item list lookups.

### TARGET-REQUIRED-FOR-CONSTRAINT
- **`unique_service_ledger_entry`**:
  - Columns: `service_type, service_id` (Unique)
  - Purpose: Enforce that each booking service has at most one ledger posting.

### TARGET-REQUIRED-FOR-CONCURRENCY
- **`cash_in_hands_user_id_unique`**:
  - Columns: `user_id` (Unique)
  - Purpose: Prevents allocating multiple cash drawers to the same salesman.

### TARGET-REQUIRED-FOR-IDEMPOTENCY
- None in Phase 7. Outbox table indexes will be added in future phases.

### TARGET-PROPOSED-FOR-PERFORMANCE (Not Mapped in DbContext yet)
- **`heads_status_index`** [PROPOSED]:
  - Columns: `status`
  - Purpose: Fast lookups of active customers/agents.
- **`receipts_status_index`** [PROPOSED]:
  - Columns: `status`
  - Purpose: Fast checks of pending/collection receipt queues.
