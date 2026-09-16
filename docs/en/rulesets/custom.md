# Custom Rulesets

Create custom rulesets in FunkArr's web UI to handle shows or movies not covered by the community catalog, or to override community rules with your own matching logic.

## Your First Ruleset

This walkthrough creates a ruleset for a TV show called "Abenteuer Wald" that airs on SWR.

### 1. Open the Builder

Navigate to **Rulesets → + New** in the FunkArr web UI. The builder opens with an empty form on the left and the live preview panel on the right.

### 2. Fill in Identity

| Field | Value | Why |
|---|---|---|
| **RuleSet ID** | `abenteuer-wald` | Unique kebab-case identifier. Cannot be changed after creation. |
| **Topic** | `Abenteuer Wald` | Exactly as it appears in the Mediathek topic field. |
| **Media Type** | Show | Tells Sonarr/Radarr how to handle it. |
| **Media Name** | `Abenteuer Wald` | Auto-filled from Topic. Change it only if the show has a different name in TVDB/TMDB. |
| **TVDB ID** | `12345` | Look this up on thetvdb.com. Optional but recommended — Sonarr uses it to match. |

Add aliases if the show appears under different topic names (e.g., "Abenteuer Wald - Spezial").

### 3. Set Default Confidence

Set confidence to `1.0` for a show where you expect reliable matches. Lower it (e.g., `0.8`) if the Mediathek data is inconsistent and you want Sonarr to double-check.

### 4. Add a Matching Rule

Click **+ Add Rule**. A new rule card opens with a generated ID.

| Field | Value |
|---|---|
| **Rule ID** | `airdate` |
| **Priority** | `0` (first rule to try) |
| **Strategy** | `Title Equals Airdate` |

For this example, the Mediathek titles look like "Abenteuer Wald vom 13. September 2026", so the airdate strategy extracts the broadcast date directly.

### 5. Add a Filter

Exclude short clips by adding a filter in the **all** section:

- Field: `duration`
- Operator: `greaterThan`
- Value: `20`

This ensures only full episodes (longer than 20 minutes) are matched.

### 6. Test with the Debugger

The live preview panel on the right auto-fetches Mediathek entries matching your topic. As you build rules, matches appear in real time with green indicators.

For more thorough testing, click **Full Test** to send your rules to the server. The full test shows a detailed pipeline trace for each candidate — which rules were tried, which filters passed or failed, and what was extracted.

### 7. Save

Click **Save**. FunkArr validates your ruleset against the JSON schema and registers it immediately. The new ruleset appears in the list and starts matching the next time Sonarr or Radarr trigger a search.

## Overrides

Local rulesets take priority over community rulesets for the same show. When both exist, FunkArr merges them:

- Rules with the **same ID** → local replaces community
- Rules with a **new ID** → appended to the list
- **Aliases** → unioned (both sets combined)
- **Media fields** → local wins per field (tvdbId, imdbId, etc.)
- **Confidence** → local value used if set

To edit a community ruleset, navigate to its detail page and click **Edit**. Your changes are saved as a local overlay — the community base stays intact and continues to receive updates.

## Standalone Mode

If you want to ignore the community base entirely, open your local ruleset JSON file and set `standalone: true`. This tells FunkArr to use your local ruleset as-is, with no merging.

Use this when a community ruleset's structure is too different from what you need, or when you want full control over every rule.

## Disabling Community Rules

To skip specific community rules without replacing them, add their IDs to the `disable` array:

```json
{
  "disable": ["title-includes", "title-includes-p1"]
}
```

Disabled rules are removed during merge. Your own rules still apply.

## Exporting for Community

If you've created a ruleset that others would find useful, click **Export for Community** on the detail page. This downloads a clean JSON file you can submit as a pull request to the community rulesets repository.

## Next Steps

- [Field Reference](./field-reference) — every field explained in detail
- [Strategy Guide](./strategies) — choosing the right identification strategy
- [Filter Cookbook](./filters) — filtering and narrowing candidates
