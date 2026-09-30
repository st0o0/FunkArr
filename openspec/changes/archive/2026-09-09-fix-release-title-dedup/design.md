## Context

`ReleaseTitleBuilder.Build(topic, title, ...)` concatenates sanitized topic + identifier + sanitized title. The Mediathek's `title` field frequently includes the topic in various patterns:
- Prefix with colon: "Tatort: Virus"
- Prefix with dash: "extra 3 - Spezial"
- Suffix with dash: "Die dunkle Stunde - Donna Leon"
- Suffix with en-dash: "Die dunkle Stunde – Donna Leon"

## Goals / Non-Goals

**Goals:**
- No duplicate series name in release titles
- Series name portion preserves comma for Sonarr matching
- Incorrect TVDB mappings in community rulesets fixed

**Non-Goals:**
- Changing how Mediathek data is fetched or stored
- Handling every possible title format edge case

## Decisions

### Decision: Strip topic from title using case-insensitive prefix/suffix removal

`StripTopicFromTitle(string topic, string title)`:
1. Check if `title` starts with `topic` followed by `: `, `: `, or ` - ` (case-insensitive) -> strip prefix
2. Check if `title` ends with ` - topic`, ` – topic` (case-insensitive) -> strip suffix
3. Trim and return. If stripping would leave empty string, return original title.

This runs BEFORE sanitization so it works on the raw Unicode strings.

### Decision: Keep comma in series name sanitization

Remove `,` from `_invalidChars`. Comma is valid in scene release names and needed for Sonarr matching ("Elefant, Tiger & Co."). The `&` stays stripped because Sonarr normalizes it away and it causes issues in URLs/filenames.

Actually - looking at how Sonarr normalizes: it strips ALL non-alphanumeric. So "Elefant, Tiger & Co." and "Elefant Tiger und Co" both normalize differently. The real issue is that FunkArr uses `media.name: "Elefant, Tiger und Co."` but TVDB has "Elefant, Tiger & Co." - the "und" vs "&" mismatch. Fix the ruleset data to use the exact TVDB name.

### Decision: Fix ruleset data directly

- `unser-sandmaennchen.json`: remove media block entirely. This topic matches multiple sub-shows.
- `fernsehfilme-und-serien-serien.json`: remove media block. This is a catch-all for misc series.
- `elefant-tiger-und-co.json`: change media.name to "Elefant, Tiger & Co." to match TVDB exactly.

## Risks / Trade-offs

**[Risk] Aggressive prefix stripping** -> "Tatort: Tatort Spezial" would strip to "Tatort Spezial" which is correct. But "Tatort aus Wien" without a colon separator wouldn't match because we only strip on known delimiters (`:`, ` - `, ` – `).
