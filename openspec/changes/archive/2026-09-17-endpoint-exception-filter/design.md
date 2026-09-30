## Context

Every API endpoint wraps its handler in try/catch that creates a logger and logs the exception. This was added as a batch change and is pure boilerplate — a single IEndpointFilter can replace all of them.

## Goals / Non-Goals

**Goals:**
- One filter handles all unhandled exception logging
- Endpoints contain only business logic

**Non-Goals:**
- Changing error response formats
- Adding structured error responses beyond what exists
- Modifying SSE streaming endpoints (different catch semantics)

## Decisions

### IEndpointFilter, not middleware

Endpoint filters run per-endpoint and have access to endpoint metadata (route name). Middleware runs for all requests including static files. Filter is more precise.

### Keep business-logic logging in handlers

The filter only replaces generic catch-and-log. Handlers that log specific business events (e.g. "RuleSet {id} not found" at Warning level) keep that logging.
