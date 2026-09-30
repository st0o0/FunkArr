## Context

The codebase carries explicit `IActorRef ReplyTo` fields in 4 places where Akka's built-in sender propagation makes them redundant. Two mechanisms cover all cases:

1. **`PipeTo(Self, Sender)`** — preserves the original sender through Ask/PipeTo chains (used by SearchWorkers).
2. **`Tell(msg, Sender)`** — forwards sender identity when an intermediary creates a new message (used by MatchMagicManager routing to pool).

The one place that genuinely needs stored sender is `SearchGatewayManager` (scatter-gather aggregation with concurrent dictionary lookups).

## Goals / Non-Goals

**Goals:**
- Remove redundant `IActorRef ReplyTo` from `ExecuteScoring`, `SearchContext`, `HttpCompleted`, `HttpFailed`
- Use idiomatic Akka sender propagation everywhere possible

**Non-Goals:**
- Changing `SearchGatewayManager.PendingSearch.OriginalSender` — scatter-gather requires stored sender
- Changing the public message contracts in `FunkArr.Messages` (e.g. `ScoreItems`, `SearchCompleted`)
- Refactoring actor topology or routing strategy

## Decisions

### D1: SearchWorkers — drop `ReplyTo` from `SearchContext`, use `Sender` directly

The workers already do `Ask(...).PipeTo(Self, Sender)` at every pipeline hop, which preserves the original `Sender` (SearchGatewayManager) through the entire chain:

```
TvSearchCommand (Sender=Gateway)
  → Ask mediathek → PipeTo(Self, Sender=Gateway)
  → Ask ruleSetResolver → PipeTo(Self, Sender=Gateway)
  → Ask matchMagic → PipeTo(Self, Sender=Gateway)
  → Sender.Tell(SearchCompleted)  ← reaches Gateway
```

All reply points (`SearchCompleted`, `SearchFailed`, `ReplyWithUnscored`, `Status.Failure`) can use `Sender` directly.

**Alternative considered**: Forward pattern — rejected because workers need to stay in the pipeline to transform intermediate results.

### D2: MediathekViewWebManager — add sender arg to `PipeTo`

Current: `Task.Run(...).PipeTo(self)` wraps `replyTo` inside internal messages.
Change: `Task.Run(...).PipeTo(self, replyTo)` and use `Sender` in handlers.

The `HttpCompleted`/`HttpFailed` records shrink to just the payload (no `IActorRef`). The handlers use `Sender.Tell(...)`.

### D3: MatchMagicManager → MatchMagicActor — use `Tell(msg, Sender)`

Current: `_router.Tell(new ExecuteScoring(config, items, Sender))` — sender wrapped in message.
Change: `_router.Tell(new ExecuteScoring(config, items), Sender)` — sender propagated by Akka.

`ExecuteScoring` loses its `IActorRef ReplyTo` field. `MatchMagicActor.Handle` replies with `Sender.Tell(...)`.

**Alternative considered**: `_router.Forward(...)` — rejected because the manager wraps the message in a new type (`ScoreItems` → `ExecuteScoring`), so Forward can't be used. `Tell(msg, Sender)` achieves the same sender propagation with a different message type.

## Risks / Trade-offs

- **[Sender lost in async gaps]** → Mitigated: all async hops use `PipeTo` with explicit sender parameter, which is the documented Akka pattern for sender preservation. No raw `await` or `ContinueWith` where Sender could be lost.
- **[Test refactoring]** → Low risk: tests use `TestKit` which sets Sender automatically via `ExpectMsg` / `Tell`. Removing `ReplyTo` from test message construction simplifies tests.
