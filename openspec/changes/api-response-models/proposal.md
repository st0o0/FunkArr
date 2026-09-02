## Why

The internal API (`/api`) returns Message records directly as JSON responses. This couples the API contract to actor communication types — changing a Message shape breaks the API, and adding API-specific fields (pagination wrappers, computed properties) requires polluting Messages. The ArrApi layer already follows the correct pattern (own models + mapping), but `FunkArr.Api` does not.

## What Changes

- Introduce dedicated API response models in `FunkArr.Api` for all endpoints that currently return Message types directly
- Add mapping from Message response records to API models in each endpoint handler
- API models define the JSON contract; Messages remain pure actor communication

Affected endpoints:
- `GET /api/rulesets` — currently returns `RegisteredRuleSetEntry[]` directly
- `GET /api/rulesets/{id}` — currently returns `RuleSetDetailResult` directly
- `GET /api/rulesets/{id}/history` — currently returns `ScoringHistoryResult` directly
- `GET /api/rulesets/{id}/history/{requestId}` — currently returns `ScoringDetailResult` directly

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `ruleset-api`: Endpoints must map actor responses to dedicated API response models instead of returning Messages directly

## Impact

- `FunkArr.Api` — new response model records, mapping logic in endpoints
- `FunkArr.Api.Tests` — if any exist, response assertions may need updating for new model types
- No changes to Messages, actors, or any other project
