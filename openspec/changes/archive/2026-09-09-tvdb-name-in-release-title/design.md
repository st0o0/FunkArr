## Context

Ruleset JSON files have `media.name` (the TVDB series name) but `RawMedia` didn't map it. The release title builder receives the Mediathek topic via `raw.Topic`, which can be a longer alias (e.g., "Löwenzahn mit Peter Lustig") that doesn't match Sonarr's TVDB-sourced series name ("Löwenzahn").

## Goals / Non-Goals

**Goals:**
- Release titles use the TVDB series name when available
- Fallback to Mediathek topic when no media name is configured

**Non-Goals:**
- Changing how the Mediathek is queried (still uses topic)
- Adding media.name to existing rulesets that don't have it

## Decisions

### Decision: Propagate media.name through the full chain

Add `MediaName` as an optional field to: `RawMedia` -> `ExtractIdentity` tuple -> `RegisterRuleSet` message -> `RuleSetResolverState` -> `RuleSetResolved` message -> search worker state -> release title builder input.

This keeps the data flow explicit and type-safe through the actor system.

## Risks / Trade-offs

**[Risk] Rulesets without media.name** -> Falls back to topic, same behavior as before. No regression.
