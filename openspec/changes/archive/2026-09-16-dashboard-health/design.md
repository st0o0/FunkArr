## Context
Dashboard stat row has 3 cards. Adding a 4th for rulesets.

## Decisions
Fetch ruleset count from the existing `GET /api/rulesets` endpoint (already returns `rulesets` array and `communityVersion`). Show count and version as subtitle.
