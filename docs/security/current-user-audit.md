# Current User Context Security Audit

This document audits the extraction and security boundaries of the current authenticated salesman context.

---

## 1. Context Resolution Path

```
HttpContext.User (ClaimsPrincipal)
  ↓
ClaimTypes.NameIdentifier
  ↓
Parsed via long.TryParse
  ↓
ICurrentUser.UserId
```

## 2. Security Assessment

1. **Source of Truth**: The user ID is sourced exclusively from the security claims of the authenticated HTTP request. No client-supplied headers (e.g. `X-User-Id`), query strings, or request body parameters are trusted.
2. **Fallback Check**: There is no hardcoded fallback or fake user context in the production `CurrentUser` implementation. In testing environments, a custom test auth handler maps mock headers directly into the security context.
3. **Database Performance**: `CurrentUser` reads values from in-memory claims already decoded during the authentication middleware execution phase. It does NOT query the database to retrieve the user ID, avoiding N+1 I/O overhead.
4. **Invalid Identity Handling**: If the token is missing or if the claim is invalid/non-numeric, `ICurrentUser.UserId` returns `null` and the API controllers return `401 Unauthorized` immediately.
