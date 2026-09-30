# message-response-interfaces

## REMOVED Requirements

### Requirement: Domain-level response marker interfaces
**Reason**: All domain-wide response interfaces (ISearchResponse, IScoringResponse, IMediathekResponse, IDownloadResponse) replaced by per-command/query abstract record response types.
**Migration**: Replace `Ask<IDomainResponse>` with `Ask<SpecificCommandResponse>` for each call site.

### Requirement: Ask calls use domain response interfaces
**Reason**: Ask calls now use per-command/query response types for exhaustive matching.
**Migration**: `Ask<ISearchResponse>` → `Ask<SearchSeriesResponse>`, `Ask<IScoringResponse>` → `Ask<ScoreItemsResponse>` or `Ask<ScoringDetailResponse>`, etc.
