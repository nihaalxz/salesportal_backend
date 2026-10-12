# Open Questions

This document lists open questions, architectural dependencies, and technical alignment requirements.

---

## 1. Idempotency Support on Accounts Portal (Critical)

- **Context**: The outbox worker will retry failed HTTP postings to the Accounts Portal (e.g. agent balance deductions) if network errors occur.
- **Problem**: If the remote Accounts Portal successfully processes a deduction but the connection drops before returning a response, the outbox worker will retry, leading to duplicate debits.
- **Requirement**: Will the Accounts Portal API be upgraded to support standard idempotency keys (e.g. `X-Idempotency-Key` or tracking by booking reference ID)?
- **Status**: **Unresolved (Requires User Review)**.

---

## 2. Legacy Hashing Resolution

- **Resolved**: Standardized on a custom `IPasswordHasher<User>` wrapper delegating checks to `BCrypt.Net-Next` to support legacy Laravel `$2y$` formats without breaking login co-existence. Mapped in ADR-006.

---

## 3. Financial Rounding

- **Resolved**: Standardized on `MidpointRounding.AwayFromZero` matching PHP's default rounding policy. Mapped in [financial-precision-and-rounding.md](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/docs/migration/financial-precision-and-rounding.md).

---

## 4. Frontend & Authentication Blockers (Phase 9)

- **JWT Authority & Audience Configuration**: The JWT validation authority and token generation endpoint are not yet implemented on the backend. This returns a `401 Unauthorized` block on all API endpoints. We mapped this blocker honestly and implemented SvelteKit diagnostics rendering for the trace/correlation IDs.
- **Default Business Currency**: We assumed BHD (Bahraini Dinar) with 3 decimal precision formatting as the default business currency. Please confirm if other currencies (like AED/USD) should be supported dynamically.
