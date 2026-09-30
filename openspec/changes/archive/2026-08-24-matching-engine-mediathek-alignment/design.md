## Context

The Mediathek API (MediathekViewWeb, backed by OpenSearch) stores items with separate `topic` and `title` fields. When querying, FunkArr currently searches both fields with a single search term. When matching, rules apply regex patterns to individual fields. The mismatch: many community rulesets use lookbehind patterns like `(?<=Tatort:\s*)\S.*` on `field: "title"`, expecting a combined "Tatort: Die goldene Zeit" format that the Mediathek API never provides — topic and title are always separate.

The MediathekViewWeb API supports targeted field queries: `{ fields: ["topic"], query: "Tatort" }` returns only items where the topic matches. This is more efficient and semantically correct for TV search where we know the show name.

## Goals / Non-Goals

**Goals:**
- TV search by topic returns relevant items with minimal noise
- Rules can explicitly reference combined topic+title via `topicTitle` field
- BuildTitle output doesn't leak separator artifacts into TVDB comparison
- Auto-generated rules work correctly for shows where topic is not in title
- Existing rules that work (SeasonAndEpisodeNumber, Airdate, etc.) are unaffected

**Non-Goals:**
- Changing the MediathekViewWeb API
- Restructuring the matching strategy enum or adding new strategies
- Modifying the RuleSet JSON schema version (adding a field to GetFieldValue is backward-compatible — old rules using "title" still work)
- Changing how the Newznab API works

## Decisions

### Decision 1: FetchItems distinguishes topic-only vs full-text

Add an optional `SearchMode` to `FetchItems`: `Topic` (queries `fields: ["topic"]`) vs `FullText` (queries `fields: ["topic", "title"]`, current behavior). Default stays `FullText` for backward compatibility.

TvSearchActor uses `SearchMode.Topic` — it knows the show name and wants items from that show. TextSearchActor and BrowseActor use `FullText` — they do open-ended search.

**Why not always topic-only?** Text search from Prowlarr (`t=search&q=Tatort`) is intentionally broad — the user may be searching for items that mention "Tatort" in their title but belong to a different topic (e.g., a documentary about Tatort). Movie search also benefits from broad matching.

### Decision 2: `topicTitle` composite field in GetFieldValue

Add `"topicTitle"` to the field switch in `GetFieldValue`:
```
"topicTitle" => $"{item.Topic}: {item.Title}"
```

This is the only engine change needed. Rules choose their field explicitly:
- `field: "title"` — regex works on episode name only
- `field: "topic"` — regex works on show name only
- `field: "topicTitle"` — regex works on "ShowName: EpisodeTitle"

No magic, no implicit merging. The rule says what it means.

### Decision 3: BuildTitle trims separator artifacts

After concatenating all TitleRules, trim leading/trailing whitespace, dashes, and colons from the result before returning. This prevents static separator rules like `" - "` at the start from polluting the TVDB comparison.

`FormatTitle` already trims whitespace (line 577). Extend it to also trim `'-'` and handle the `" - "` prefix pattern. Only affects the string used for TVDB comparison — the original title construction logic is unchanged.

**Alternative considered:** Only trim when the result starts with a separator. Rejected — simpler to always trim consistently. A TVDB episode name will never start or end with ` - `.

### Decision 4: Auto-generator uses topicTitle for low TopicPrefixCount

In `RuleSetGeneratorActor.GenerateRegex`, when `DetectStrategy` falls back to `ItemTitleExact` or `ItemTitleIncludes` and `TopicPrefixCount` is low (topic not found in sample titles), generate TitleRules with `field: "topicTitle"` instead of `field: "title"`. The regex can then use the show name as an anchor.

When `TopicPrefixCount` is high (title already contains topic prefix), keep `field: "title"` — the regex works directly on the title.

## Risks / Trade-offs

**[Risk] Community ruleset changes are breaking for users with local overrides** → Mitigation: Local overrides use merge mode and reference rules by priority index. Changing the field inside a rule doesn't affect the merge mechanism. Users with full-replacement local overrides are unaffected (their local rules take precedence).

**[Risk] `topicTitle` format assumes ": " separator** → Mitigation: This matches the convention visible in MediathekView's UI and in how broadcasters format titles. If a show's topic contains a colon, the regex can still handle it. The separator is a display convention, not a semantic boundary.

**[Risk] Topic-only query may miss items where topic doesn't exactly match** → Mitigation: OpenSearch `multi_match` with `cross_fields` is still fuzzy enough to match partial topic names. And the MediathekGateway already gets 5000 items max — topic-only just reduces noise, doesn't risk missing real matches.
