# feed-title-filter Specification

## Purpose
TBD - created by archiving change fix-newznab-title-search. Update Purpose after archive.
## Requirements
### Requirement: Filter feed entries by topic when query is set

When the search falls through to the feed/validation path and `q` (or mapped
`title`) is set, filter feed entries by topic (case-insensitive Contains on
`entry.Topic`) before returning. When no filter is applied (q is empty), behavior
is unchanged — all feed entries returned.

