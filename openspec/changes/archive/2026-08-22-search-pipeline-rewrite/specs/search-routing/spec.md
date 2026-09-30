## REMOVED Requirements

### Requirement: Request routing to dedicated child actors
**Reason**: Replaced by SearchCoordinator pipeline orchestration. The coordinator no longer routes to type-specific child actors (TvSearchActor, MovieSearchActor, TextSearchActor). Instead, it orchestrates five specialized workers through a PipeTo pipeline.
**Migration**: Controllers use the same external message types (TvSearchRequest, MovieSearchRequest, TextSearchRequest, SearchResponse) but resolve SearchCoordinator instead of SearchActor.

### Requirement: Cache checked before routing, populated from child results
**Reason**: Cache behavior moves to SearchCoordinator with topic-level keys instead of episode-level keys. See search-coordinator spec.
**Migration**: Cache is in-memory; no data migration. New topic-level keys enable coalescing.

### Requirement: Dependency resolution stays in the parent
**Reason**: Dependency resolution moves to SearchCoordinator with the same behavior. See search-coordinator spec.
**Migration**: Same stashing/re-resolution pattern, new actor name.

### Requirement: Rules pre-fetched by parent for TV search
**Reason**: Rules are now fetched as a pipeline step (parallel Ask to RuleSetCoordinator) by SearchCoordinator. See search-coordinator spec.
**Migration**: Same behavior, different orchestration.

### Requirement: Match records forwarded from child to ledger
**Reason**: Match record forwarding moves to SearchCoordinator. See search-coordinator spec.
**Migration**: Same fire & forget Tell to MatchLedgerActor.

### Requirement: Child actor lifecycle
**Reason**: Three type-specific children replaced by five specialized workers. See search-coordinator spec.
**Migration**: Workers are permanent children with Restart supervision.

### Requirement: External API surface unchanged
**Reason**: External API surface is preserved in SearchCoordinator. Message types move from SearchActor to SearchCoordinator.
**Migration**: Controllers resolve SearchCoordinator instead of SearchActor. Message types renamed (SearchActor.TvSearchRequest → SearchCoordinator.TvSearchRequest).

### Requirement: MovieSearchActor resolves movie metadata via TMDB before searching
**Reason**: TMDB resolution moves to ShowResolverWorker. See show-resolver-worker spec.
**Migration**: Same logic, different actor.

### Requirement: MovieSearchActor populates duration context from TMDB runtime
**Reason**: Duration context is set by SearchCoordinator from ShowResolverWorker results before asking MatchWorker.
**Migration**: Same behavior, different orchestration.

### Requirement: MovieSearchActor fallback search with original title
**Reason**: Fallback search logic moves to SearchCoordinator pipeline (second Ask to MediathekGatewayWorker if first returns empty and original title differs).
**Migration**: Same behavior, different orchestration.
