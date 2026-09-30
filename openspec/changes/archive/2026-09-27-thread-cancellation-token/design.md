## Context

ASP.NET Core provides `HttpContext.RequestAborted` - a CancellationToken that fires when the client disconnects. In Minimal APIs, adding `CancellationToken` as a lambda parameter auto-binds it. In controllers, adding it as an action parameter does the same. Akka.NET's `Ask<T>` supports a 3-arg overload: `Ask<T>(message, TimeSpan? timeout, CancellationToken ct)`.

Currently, only the SSE streaming endpoint (`/queue/stream`) passes the token. All other ~43 Ask calls use timeout-only, meaning the server continues processing even after the client disconnects.

## Goals / Non-Goals

**Goals:**
- Every HTTP-facing Ask call receives the request's CancellationToken
- Service classes in ArrApi forward CT from controller to Ask
- Tests compile and pass with the new CT parameters

**Non-Goals:**
- Adding CT to actor-to-actor Ask calls (those are internal coordination, not tied to HTTP lifecycle)
- Adding CT to non-Ask async operations (e.g., database calls, file I/O) - future improvement
- Changing timeout values
- Adding CT-aware logic inside actors (actors don't see the token - Ask handles cancellation externally)

## Decisions

### 1. Minimal API: CT as lambda parameter

ASP.NET auto-injects `CancellationToken` when it appears in the lambda signature. No attribute or manual extraction needed.

```csharp
// Before
group.MapGet("/queue", async (IActorRegistry registry) =>
{
    var result = await manager.Ask<QueueResponse>(new QueryQueue(), _askTimeout);
});

// After
group.MapGet("/queue", async (IActorRegistry registry, CancellationToken ct) =>
{
    var result = await manager.Ask<QueueResponse>(new QueryQueue(), _askTimeout, ct);
});
```

### 2. Controllers: CT as action parameter

Same auto-binding behavior. The CT parameter goes last by convention.

```csharp
public async Task<IActionResult> HandleGet(..., CancellationToken cancellationToken)
```

### 3. Service classes: CT on public methods

Service methods that call Ask need CT threaded through. Add it as the last parameter.

```csharp
// Before
public async Task<SearchResult> Search(SearchQuery query)
    => await _actor.Ask<SearchResult>(query, _timeout);

// After
public async Task<SearchResult> Search(SearchQuery query, CancellationToken ct)
    => await _actor.Ask<SearchResult>(query, _timeout, ct);
```

### 4. Keep timeout + CT together

Use the 3-arg overload `Ask<T>(message, timeout, ct)`, not the CT-only overload. The timeout is the actor-level deadline (how long the actor has to respond). The CT is the client-level signal (client disconnected). Both are needed.

### 5. Tests: pass CancellationToken.None

Test methods calling services need to pass `CancellationToken.None` (or `default`). This is the standard pattern for tests.

## Risks / Trade-offs

- **Large but mechanical diff** - Every API endpoint and service method signature changes. Risk of missed spots. Mitigation: grep for remaining `Ask<` calls without CT after implementation.
- **OperationCanceledException in logs** - When clients disconnect, Ask throws `OperationCanceledException` (or `TaskCanceledException`). If not handled, these show as errors in logs. Mitigation: ASP.NET already handles this gracefully for controller/endpoint code - cancelled requests don't log as 500s by default.
