# Shared Booking Write Infrastructure Design

## Principle

Extract shared infrastructure ONLY after comparing at least two concrete implementations (OtherService + Hotel). Do not introduce abstractions based on a single reference.

---

## What IS Shared (Stable Infrastructure, Already Exists)

| Component | What it does | Used by all services |
|---|---|---|
| `ITransactionalExecutor` | Executes a delegate inside a retryable EF Core execution strategy + explicit DB transaction with rollback | ✅ |
| `ISalesPortalDbContext` | Single unit-of-work for all entities | ✅ |
| `IdempotencyRecord` entity | Deduplication with InProgress/Completed/Failed states, SHA-256 hash verification | ✅ |
| `GetSha256Hash()` | Deterministic hash for idempotency payload | ✅ |

---

## What MIGHT Become Shared (After Hotel + OtherService Comparison)

These are candidate extractions — not premature abstractions:

### 1. Idempotency Check-Insert Pattern
The check-and-insert flow (check → register InProgress → execute → mark Completed/Failed) is identical in all write handlers. This is a candidate for extraction into a helper after ≥2 services confirm the pattern.

**Decision after Hotel**: If identical to OtherService, extract `IdempotencyService.RegisterAsync()` + `IdempotencyService.CompleteAsync()`.

### 2. Head Resolution (Customer vs Agent)
The "resolve target head from customer headId OR agent agentId" logic is common across all services that support both client types.

**Decision after Hotel**: Extract `HeadResolverService.ResolveAsync(clientType, headId, agentId, ct)` if Hotel confirms identical behavior.

### 3. Wallet Deduction
Identical `ExecuteUpdateAsync` atomic wallet guard pattern.

**Candidate**: `WalletService.DeductAsync(headId, amount, ct)` — but only if the WHERE clause and failure semantics are identical across services.

### 4. CashInHand Upsert
Identical upsert pattern for cash payments.

**Candidate**: `CashInHandService.UpsertAsync(userId, amount, ct)`.

### 5. Salesman Profit Increment
Identical for all single-record services. Flight has passenger-sum variant.

**Candidate**: `SalesmanProfitService.IncrementAsync(userId, profit, ct)`.

---

## What Must NOT Be Shared

| Anti-Pattern | Reason |
|---|---|
| `BaseBookingHandler<TRequest, TResult>` | Virtual method explosion, hidden coupling |
| Generic booking entity | Different schemas, different relationships |
| Reflection-based booking dispatch | Runtime errors, not type-safe |
| `BookingFactory.Create(type, request)` | Service-type switch throughout codebase |
| Shared validation base class | Each service has distinct required fields |

---

## Decision Gate

After OtherService and Hotel are both complete:
1. Compare idempotency code → if identical → extract.
2. Compare head resolution code → if identical → extract.
3. Compare wallet deduction code → if identical → extract.
4. Document which services USE each extracted component.
5. Do not extract unless at least 3 services will use it.
