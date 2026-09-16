# Filters

Filters decide which Mediathek entries enter a matching rule. They run **before** identification — if a filter fails, the rule is skipped and the next rule in priority order is tried.

## How Filters Work

Every rule can have a `filters` object with three sections that act as boolean logic gates:

| Section | Logic | Behavior |
|---|---|---|
| `all` | **AND** | Every condition must match |
| `any` | **OR** | At least one condition must match |
| `not` | **NOT** | None of the conditions may match |

All three sections are optional. If `filters` is omitted entirely, the rule accepts every candidate.

When multiple sections are present, they are evaluated in order: `all` → `any` → `not`. All sections must pass for the filter to succeed.

## Fields

Each condition checks one field from the Mediathek entry:

| Field | Type | Description | Example value |
|---|---|---|---|
| `title` | string | The full item title | "Tatort: Borowski und die Kinder" |
| `topic` | string | The topic/show name | "Tatort" |
| `channel` | string | The broadcaster | "ARD", "ZDF", "ORF", "SRF" |
| `description` | string | Item description text | "Kommissar Borowski ermittelt..." |
| `duration` | number | Duration in minutes | 90 |
| `timestamp` | number | Broadcast timestamp (Unix epoch) | 1726000000 |

## Operators

| Operator | Works on | Description |
|---|---|---|
| `eq` | string | Exact string match |
| `contains` | string | Substring match (case-sensitive) |
| `notContains` | string | Substring must not be present |
| `greaterThan` | number | Numeric greater-than comparison |
| `lessThan` | number | Numeric less-than comparison |
| `regex` | string | Regular expression match |

::: warning
Numeric values must be passed as strings in JSON: `"value": "60"`, not `"value": 60`.
:::

## Common Patterns

### Exclude short clips

Most Mediathek entries include trailers, teasers, and clip excerpts that are much shorter than full episodes. Filter them out with a duration check:

```json
"filters": {
  "all": [
    { "field": "duration", "op": "greaterThan", "value": "25" }
  ]
}
```

This is the most common filter — nearly every community ruleset uses it.

### Restrict to a specific channel

When a show airs on multiple channels with different title formats, filter by broadcaster:

```json
"filters": {
  "all": [
    { "field": "channel", "op": "eq", "value": "ZDF" }
  ]
}
```

### Exclude trailers and specials

Filter out entries with keywords that indicate non-episode content:

```json
"filters": {
  "not": [
    { "field": "title", "op": "contains", "value": "Trailer" },
    { "field": "title", "op": "contains", "value": "Vorschau" },
    { "field": "title", "op": "contains", "value": "(Audiodeskription)" }
  ]
}
```

The `not` section rejects any entry matching **any** of its conditions.

### Match by description keyword

Some shows share a topic name but have different content. Use the description to distinguish:

```json
"filters": {
  "all": [
    { "field": "description", "op": "contains", "value": "Krimi" }
  ]
}
```

### Match by title pattern

Use a regex filter when the title structure matters:

```json
"filters": {
  "all": [
    { "field": "title", "op": "regex", "value": "^Tatort:\\s" }
  ]
}
```

### Combine conditions

Combine duration and channel filters to be precise:

```json
"filters": {
  "all": [
    { "field": "duration", "op": "greaterThan", "value": "35" },
    { "field": "channel", "op": "eq", "value": "ARD" }
  ],
  "not": [
    { "field": "title", "op": "contains", "value": "Trailer" }
  ]
}
```

This matches only ARD entries longer than 35 minutes that aren't trailers.

## Nested Groups

For complex logic, filter nodes can be nested groups instead of simple conditions. Each nested group has its own `all`/`any`/`not` sections.

**Example:** Match entries from ARD or ZDF, but only if they're longer than 30 minutes:

```json
"filters": {
  "all": [
    { "field": "duration", "op": "greaterThan", "value": "30" },
    {
      "any": [
        { "field": "channel", "op": "eq", "value": "ARD" },
        { "field": "channel", "op": "eq", "value": "ZDF" }
      ]
    }
  ]
}
```

The outer `all` requires both conditions to pass: duration > 30 AND (channel = ARD OR channel = ZDF).

::: tip
Most rulesets only need flat conditions. Reach for nested groups when you need OR logic inside an AND block, or other combinations that flat sections can't express.
:::

## Short-Circuit Evaluation

Filters evaluate with short-circuit logic:

- **`all`**: stops at the first failing condition (remaining conditions show as "Skipped" in the debugger)
- **`any`**: stops at the first passing condition
- **`not`**: stops at the first matching condition (which means the filter fails)

This is visible in the debugger's pipeline trace — skipped conditions appear in gray.
