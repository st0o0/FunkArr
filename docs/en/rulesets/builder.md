# Ruleset Builder

Visually construct a FunkArr ruleset and export valid JSON - no running FunkArr instance required. The builder runs entirely in your browser; no data is sent to a server.

<RulesetBuilder />

## How the Builder Works

The form is on the left, the live preview of the generated JSON on the right. Every change is reflected in the preview immediately.

### Identity and Media

| Field | Description |
|---|---|
| Topic | Required. The show's topic; must match the Mediathek topic field. |
| Aliases | Alternative topic names. Add with **+ Add Alias**, remove with &times;. |
| Name | Required. Display name for Sonarr/Radarr. Follows the topic automatically until you edit it manually. |
| Type | `show` or `movie`. |
| Confidence | Default confidence of the ruleset (0-1, default `1.0`). |
| TVDB ID / IMDB ID / TMDB ID | External IDs. At least one is recommended. |

### Rules

Click **+ Add Rule** to add a rule. Each rule is a collapsible card with:

- **Rule ID**, **Priority**, and optional **Confidence** (overrides the ruleset confidence)
- **Strategy** - one of the five [strategies](./strategies). Matching fields appear depending on the selection:
  - `seasonAndEpisodeNumber`: Season Regex, Episode Regex, Capture Group
  - `byAbsoluteEpisodeNumber`: Episode Regex, Capture Group
  - `itemTitleExact` and `itemTitleIncludes`: Title Rules with **+ Static** (literal text) and **+ Regex** (field, pattern, capture group)
  - `itemTitleEqualsAirdate`: no additional fields, the date is detected automatically
- **Filters** - conditions in the **ALL** (AND), **ANY** (OR), and **NOT** sections. ANY and NOT are collapsed and open on click. Each condition consists of a field, an operator, and a value. See [Filters](./filters).

### Validation

The builder validates as you type and shows messages below the JSON preview and next to the affected fields:

- **Errors:** missing topic or media name, missing or duplicate rule ID, invalid rule ID (kebab-case, at least 3 characters), missing strategy, invalid regex patterns
- **Warnings:** no rules defined, no external ID (`tvdbId`, `imdbId`, `tmdbId`) set

### Export and Import

- **Copy** copies the JSON to the clipboard, **Download** saves it as `<topic>.json`.
- **Import JSON** loads an existing ruleset into the builder for editing.

::: warning Builder limitations
The builder covers the core fields (`topic`, `aliases`, `media`, `confidence`, `rules`). `standalone`, `disable`, `enrichment`, and nested filter groups are not supported and are lost on import. Add such fields manually to the exported JSON if needed (see [Field Reference](./field-reference)).
:::

You can use the exported ruleset as a local ruleset - see [Custom Rulesets](./custom).
