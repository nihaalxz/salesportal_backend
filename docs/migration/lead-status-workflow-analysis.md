# Lead Status Workflow Analysis

## Source Evidence

**Laravel source**: Not available locally.  
**Primary sources**: `schema_deploy.sql`, `Lead.cs` entity, `DevSeeder.cs`, `SalesReadService.cs`.

---

## Schema Evidence

```sql
[status] nvarchar(255) NOT NULL
```

No check constraint on status values — the database accepts any string. This confirms Laravel did NOT enforce transitions at the database level.

---

## Known Status Values (from DevSeeder)

| Status | Evidence | Notes |
|---|---|---|
| `"new"` | DevSeeder.cs L239 | Used as initial status |
| `"qualified"` | DevSeeder.cs L248 | Mid-pipeline status |

---

## Inferred Status Values (from CRM pattern + schema width nvarchar(255))

The 255-char width is permissive. Based on standard CRM pipeline patterns consistent with the seeder evidence:

| Status | Meaning | Initial? | Terminal? |
|---|---|---|---|
| `new` | Freshly created lead | ✅ | ❌ |
| `contacted` | Salesman reached out | ❌ | ❌ |
| `qualified` | Lead has shown interest | ❌ | ❌ |
| `converted` | Lead became a Head (customer) | ❌ | ✅ |
| `lost` | Lead disqualified | ❌ | ✅ |

---

## Laravel Weakness

Laravel stored status as an open string. No transition guard in the controller or service. Any status string could be written to the DB.

---

## .NET Decision: Validated Constants with Minimal Transition Rules

Phase 11 implementation:
1. Define allowed status values as string constants: `new`, `contacted`, `qualified`, `converted`, `lost`.
2. Validate on write that status is one of the allowed values.
3. Enforce ONE transition rule: a `converted` lead cannot be changed back to `new` or `contacted`.
4. Terminal statuses (`converted`, `lost`) cannot transition to any other status.
5. This preserves all Laravel workflows while preventing invalid state.

No full state machine is implemented — this is the minimum safe improvement.

---

## Conversion Behavior

If Laravel supports converting a Lead to a Head: this is deferred to a separate Phase 11 task. The Phase 11 Lead write implementation creates and updates leads only. Lead-to-Head conversion is documented as a future operation.

---

## Archive/Delete Behavior

`deleted_at` column exists. Soft-delete via `PATCH /api/sales/leads/{id}/archive`. Archived leads excluded from list queries.

---

## Tests Required

- [ ] Create with status `"new"` succeeds
- [ ] Create with invalid status `"random"` → 400
- [ ] Update status `new` → `contacted` succeeds
- [ ] Update status `converted` → `new` blocked → 422
- [ ] Update status `lost` → `contacted` blocked → 422
- [ ] Cross-owner lead update blocked → 403/404
