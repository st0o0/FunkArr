# Field Reference

Every field in the FunkArr ruleset JSON format, organized by section.

## Root Fields

| Field | Type | Required | Default | Description |
|---|---|---|---|---|
| `topic` | string | Yes | - | Display name of the show or movie. Must match the Mediathek topic field exactly. Used for topic-based lookup when Sonarr/Radarr trigger a search. |
| `aliases` | string[] | No | `[]` | Alternative topic names. FunkArr checks aliases when the primary topic doesn't match. Useful when a show appears under different names across broadcasters (e.g., "Tatort aus Österreich"). |
| `media` | object | Yes | - | External metadata IDs and media type. See [Media Fields](#media-fields). |
| `confidence` | number | No | `0` | Default confidence score (0.0–1.0) applied to rules that don't set their own. Higher values mean Sonarr/Radarr trust the match more. Use `1.0` for reliable matches, lower for fuzzy ones. |
| `rules` | array | Yes | - | Ordered list of matching rules. See [Rule Fields](#rule-fields). |
| `standalone` | boolean | No | `false` | **Local only.** When `true`, the local ruleset is used as-is - the community base is ignored entirely. No merging happens. |
| `disable` | string[] | No | `[]` | **Local only.** List of community rule IDs to skip during merge. The rules are removed before local rules are applied. Each value must match a rule `id` in the community base. |

## Media Fields

The `media` object links a ruleset to external metadata providers so Sonarr and Radarr can identify the show or movie.

| Field | Type | Required | Default | Description |
|---|---|---|---|---|
| `name` | string | Yes | - | Display name for Sonarr/Radarr. Usually the same as `topic`, but can differ (e.g., topic "Checker Reportagen" → media name "Checker Julian"). |
| `type` | string | Yes | `"show"` | Either `"show"` or `"movie"`. Determines whether FunkArr generates Newznab TV or movie categories. |
| `tvdbId` | integer | No | - | TheTVDB series or movie ID. Sonarr uses this for matching. Look it up at [thetvdb.com](https://thetvdb.com). |
| `imdbId` | string | No | - | IMDb ID in `tt` format (e.g., `"tt0806910"`). Used by both Sonarr and Radarr. |
| `tmdbId` | integer | No | - | TheMovieDB ID. Radarr uses this for movie matching. |

::: tip
At least one external ID (`tvdbId`, `imdbId`, or `tmdbId`) is strongly recommended. Without one, Sonarr/Radarr may not be able to match the download to the correct show or movie.
:::

## Rule Fields

Each entry in the `rules` array defines one matching attempt. Rules are evaluated in `priority` order - the first successful match wins.

| Field | Type | Required | Default | Description |
|---|---|---|---|---|
| `id` | string | Yes | - | Unique identifier for this rule within the ruleset. Must be kebab-case (lowercase letters, digits, hyphens, minimum 3 characters). Used for override targeting when local rules replace community rules. |
| `priority` | integer | No | `0` | Sort order for rule evaluation. Lower values are tried first. When two rules have the same priority, array order is used as tiebreaker. |
| `strategy` | string | Yes | - | Identification strategy. Determines how season/episode information is extracted. See [Strategies](./strategies) for details. |
| `confidence` | number | No | - | Override the ruleset's default confidence for this specific rule. Use a lower value for rules that are less certain (e.g., a fallback regex). |
| `filters` | object | No | - | Pre-filter conditions that must pass before identification is attempted. See [Filter Fields](#filter-fields). |
| `seasonRegex` | string | No | - | Regex pattern to extract a season number. Used with `seasonAndEpisodeNumber` strategy. Must contain at least one capture group. |
| `episodeRegex` | string | No | - | Regex pattern to extract an episode number. Used with `seasonAndEpisodeNumber` and `byAbsoluteEpisodeNumber` strategies. Must contain at least one capture group. |
| `captureGroup` | integer | No | `1` | Which regex capture group to use for extraction (0-indexed). Defaults to the first capture group. |
| `titleRules` | array | No | - | Title construction parts for `itemTitleExact`, `itemTitleIncludes`, and `itemTitleEqualsAirdate` strategies. See [Title Rule Fields](#title-rule-fields). |

## Filter Fields

The `filters` object uses boolean logic to narrow which Mediathek entries enter a rule. Filters run before identification - if a filter fails, the rule is skipped and the next rule is tried.

### Filter Group

| Field | Type | Description |
|---|---|---|
| `all` | array | **AND** - every condition must match. |
| `any` | array | **OR** - at least one condition must match. |
| `not` | array | **NOT** - none of the conditions may match. |

Each array contains filter nodes. A node is either a **condition** (leaf) or a nested **filter group** (for complex logic).

### Filter Condition

| Field | Type | Required | Description |
|---|---|---|---|
| `field` | string | Yes | The Mediathek field to check. One of: `title`, `topic`, `channel`, `description`, `duration`, `timestamp`. |
| `op` | string | Yes | Comparison operator. One of: `eq`, `contains`, `notContains`, `greaterThan`, `lessThan`, `regex`. |
| `value` | string | Yes | The value to compare against. For numeric operators (`greaterThan`, `lessThan`), pass the number as a string (e.g., `"60"`). |

### Filter Fields Reference

| Field | Type | Description | Example |
|---|---|---|---|
| `title` | string | The item title from the Mediathek (e.g., "Tatort: Borowski und die Kinder") | `"Tatort:"` |
| `topic` | string | The topic/show name (e.g., "Tatort") | `"Tatort"` |
| `channel` | string | The broadcaster (e.g., "ARD", "ZDF", "ORF") | `"ARD"` |
| `description` | string | Item description text | `"Krimi"` |
| `duration` | number | Duration in minutes | `"60"` |
| `timestamp` | number | Broadcast timestamp (Unix epoch) | `"1726000000"` |

### Filter Operators Reference

| Operator | Description | Example |
|---|---|---|
| `eq` | Exact string match | `channel eq "ARD"` |
| `contains` | Substring match (case-sensitive) | `title contains "Tatort"` |
| `notContains` | Substring must not be present | `title notContains "Trailer"` |
| `greaterThan` | Numeric greater-than | `duration greaterThan "25"` |
| `lessThan` | Numeric less-than | `duration lessThan "120"` |
| `regex` | Regular expression match | `title regex "S\\d{2}E\\d{2}"` |

See the [Filter Cookbook](./filters) for common patterns and examples.

## Title Rule Fields

Title rules construct or extract a title string piece by piece, left to right. Used with `itemTitleExact`, `itemTitleIncludes`, and `itemTitleEqualsAirdate` strategies.

| Field | Type | Required | Description |
|---|---|---|---|
| `type` | string | Yes | Either `"static"` (literal text) or `"regex"` (extract from a Mediathek field). |
| `value` | string | No | The literal text to append. Used when `type` is `"static"`. |
| `field` | string | No | The Mediathek field to run the regex against. Used when `type` is `"regex"`. One of: `title`, `topic`, `channel`, `description`. |
| `pattern` | string | No | Regex pattern to match. Used when `type` is `"regex"`. The matched text (or specified capture group) is appended to the constructed title. |
| `captureGroup` | integer | No | Which capture group to extract (0 = full match, 1 = first group, etc.). Defaults to `0` (full match) when omitted. |

### Example

This title rule set from the Tatort community ruleset:

```json
"titleRules": [
  { "type": "static", "value": " - " },
  { "type": "regex", "field": "title", "pattern": "(?<=Tatort:\\s*)\\S.*" }
]
```

Given the Mediathek title "Tatort: Borowski und die Kinder", this produces: `" - Borowski und die Kinder"`. With the `itemTitleIncludes` strategy, Sonarr searches for any episode title containing that string.
