## REMOVED Requirements

### Requirement: ShowResolverWorker persistent actor
**Reason**: Split into two independent singleton persistent actors: `SeriesResolver` (TVDB) and `MovieResolver` (TMDB). Each has its own persistence stream, cache, and request coalescing — eliminating the mixed-concern design.
**Migration**: PersistenceId `show-resolver` is abandoned. New PersistenceIds `series-resolver` and `movie-resolver` start with empty journals. Caches rebuild organically within 24h from API calls. Acceptable at version 0.x (no data migration needed).
