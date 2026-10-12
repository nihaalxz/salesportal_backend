# Relationship Fidelity Matrix

This matrix maps foreign key constraints and delete behaviors between legacy MySQL source and the EF Core SQL Server configurations.

---

## 1. Mapped Relationships and Delete Behaviors

| Relationship ID | Principal Table | Dependent Table | Source FK Name | Source ON DELETE | Laravel Manual Delete Behavior | Soft Delete Interaction | Target SQL Server Delete Behavior | Intentional Difference? & Reason |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **REL-001** | `users` | `heads` | `heads_created_by_foreign` | `ON DELETE CASCADE` | None | None | `DeleteBehavior.Cascade` | None. |
| **REL-002** | `heads` | `head_ledgers` | `head_ledgers_head_id_foreign` | `ON DELETE CASCADE` | None | None | `DeleteBehavior.Cascade` | None. |
| **REL-003** | `heads` | `receipts` | `receipts_head_id_foreign` | `ON DELETE CASCADE` | None | `deleted_at` | `DeleteBehavior.Cascade` | None. |
| **REL-004** | `users` | `receipts` | `receipts_created_by_foreign` | `ON DELETE CASCADE` | None | `deleted_at` | `DeleteBehavior.Cascade` | None. |
| **REL-005** | `users` | `flight_bookings` | `flight_bookings_user_id_foreign` | `RESTRICT` | None | None | `DeleteBehavior.Restrict` | None. |
| **REL-006** | `flight_bookings` | `flight_passengers` | `flight_passengers_flight_booking_id_foreign` | `ON DELETE CASCADE` | None | None | `DeleteBehavior.Cascade` | None. |
| **REL-007** | `users` | `hotel_bookings` | `hotel_bookings_user_id_foreign` | `ON DELETE CASCADE` | None | None | `DeleteBehavior.Restrict` | **Yes (Resiliency)**: Prevent accidental drop of bookings if user is deleted; mapped as Restrict in EF Core. |
| **REL-008** | `users` | `sales_visa_applications` | `sales_visa_applications_user_id_foreign` | `RESTRICT` | None | None | `DeleteBehavior.Restrict` | None. |
| **REL-009** | `sales_visa_applications` | `sales_visa_applicants` | `sales_visa_applicants_sales_visa_application_id_foreign` | `ON DELETE CASCADE` | None | None | `DeleteBehavior.Cascade` | None. |
| **REL-010** | `users` | `vfs_appointments` | `vfs_appointments_created_by_foreign` | `ON DELETE CASCADE` | None | `deleted_at` | `DeleteBehavior.Restrict` | **Yes**: Cascade path warning in SQL Server (cycles/multiple cascade paths). Restrict applied. |
| **REL-011** | `heads` | `vfs_appointments` | `vfs_appointments_head_id_foreign` | `ON DELETE CASCADE` | None | `deleted_at` | `DeleteBehavior.Restrict` | **Yes**: Mapped as Restrict to prevent SQL Server multiple cascade path constraints warnings. |

---

## 2. SQL Server Multiple Cascade Path Mitigations

> [!IMPORTANT]
> SQL Server prohibits multiple cascade paths (e.g. from `users` &rarr; `vfs_appointments` and `heads` &rarr; `vfs_appointments` when `heads` also cascades from `users`). 
> To resolve this, relations targeting `vfs_appointments`, `holiday_packages`, `transportations`, and `other_services` have been configured with **`DeleteBehavior.Restrict`** in EF Core configurations, requiring deletions to be handled explicitly or utilizing soft delete filtering.
