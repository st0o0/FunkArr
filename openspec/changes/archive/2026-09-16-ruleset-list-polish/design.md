## Context
List page badges and filters should surface differences, not repeat defaults.

## Decisions

### 1. Source filter hidden when uniform
When all rulesets have the same source type (e.g. all community), the source filter tabs don't help — hide them entirely.

### 2. Badge suppression logic
- Media type badge: only show "movie" badge. Shows are the default, so no badge needed.
- Source badge: only show when there's a mix of sources (local/merged exist alongside community). When all are community, suppress.

### 3. Alias truncation
Long alias lists (3+ aliases) truncate to first 2 with "+N more" suffix.
