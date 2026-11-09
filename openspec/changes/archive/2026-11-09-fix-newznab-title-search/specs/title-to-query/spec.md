## ADDED Requirements

### Requirement: Map Newznab title parameter to query

When `IndexerRequest.Title` is set and `IndexerRequest.Q` is empty, treat `title`
as `q` for tvsearch and movie search. The mapping happens before
`IsWithoutSearchCriteria` so the request triggers a MediathekViewWeb search instead
of the feed/validation fallback. When both `title` and `q` are set, `q` takes
precedence.
