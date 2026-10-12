# Correlation ID Observability Strategy

This document details request tracing and correlation patterns implemented for the sales portals API.

---

## 1. Trace Headers

- **Header Key**: `X-Correlation-ID`
- **Default Behavior**: If the incoming request contains a valid `X-Correlation-ID` header, it is mapped into the `HttpContext.TraceIdentifier`. If it is absent, the system generates a unique GUID string.
- **Trace Propagation**: The correlation ID is added to all logs via logging scopes, and returned in error ProblemDetails models to allow developers to reconcile exceptions in APM suites (like Application Insights or Seq).
