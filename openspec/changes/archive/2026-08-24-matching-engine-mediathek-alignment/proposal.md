## Why

The RuleSetMatchingEngine produces 0 matches from 1000 Mediathek items because rules were written for a combined "Tatort: Die goldene Zeit" string that doesn't exist. The Mediathek API separates data into `topic` (show name) and `title` (episode name), but the engine has no way to provide the combined form. Additionally, the MediathekGateway queries both topic and title fields indiscriminately, returning noisy results when a topic-specific query would be more efficient and semantically correct.

## What Changes

- MediathekGateway gains topic-specific querying for TV search — queries `fields: ["topic"]` instead of `["topic", "title"]`. Text search remains broad. This is efficient querying (like `WHERE show_id=X`), not matching logic.
- MatchingEngine adds a `topicTitle` composite field in `GetFieldValue` returning `"{topic}: {title}"`. Rules explicitly choose which field they need: `"title"` for episode-only, `"topic"` for show-only, `"topicTitle"` for the combined form.
- MatchingEngine trims leading/trailing separators from `BuildTitle` output before TVDB comparison, preventing static `" - "` prefixes from causing false negatives.
- RuleSetGeneratorActor uses `topicTitle` when it detects the topic prefix is not present in item titles (low `TopicPrefixCount`).
- **BREAKING** (community rulesets only): 19 community ruleset JSON files updated to use `field: "topicTitle"` where lookbehind patterns expect the show name prefix.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `ruleset-matching-engine`: New `topicTitle` composite field in `GetFieldValue`; `BuildTitle` trims separator artifacts before TVDB comparison
- `mediathek-gateway-worker`: TV search uses topic-specific querying; `FetchItems` message distinguishes topic-only vs full-text search
- `ruleset-auto-generation`: Auto-generated rules use `topicTitle` field when topic prefix is absent from sample titles
- `community-dataset`: 19 rulesets with lookbehind patterns updated to reference `topicTitle` field

## Impact

- **Engine**: `RuleSetMatchingEngine.GetFieldValue` (new field), `BuildTitle` (trim logic)
- **Gateway**: `MediathekGatewayActor` query construction, `FetchItems` message shape
- **Generator**: `RuleSetGeneratorActor.GenerateRegex` field selection
- **Data**: 19 community ruleset JSON files
- **API surface**: No external API changes
- **Tests**: MatchingEngine tests for new field, Gateway tests for topic-specific query, existing contract tests unaffected
