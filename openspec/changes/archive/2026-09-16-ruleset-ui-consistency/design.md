## Context

After the `unify-ruleset-schema` change, the API returns structured typed data — filter ops as enums (`eq`, `contains`, `greaterThan`...), fields as enums (`title`, `topic`, `duration`...), groups as structured `all`/`any`/`not` arrays, and title rules as typed `static`/`regex` parts. But every view renders these raw. The Builder has hardcoded `<option value="greaterThan">greaterThan</option>`, the Detail-View shows `duration greaterThan 35`, the Trace-View shows `duration greaterThan 35 → actual`. No i18n, no consistency.

## Goals / Non-Goals

**Goals:**
- Single source of truth for op/field/group/titlePart display across all views
- Localized labels (DE+EN) for all rule vocabulary
- Visually improved Detail-View with collapsible rules and structured filter/title-rule rendering
- Builder dropdowns show localized labels while keeping enum values for serialization

**Non-Goals:**
- Changing filter/title-rule editing UX in the Builder (only dropdown labels change)
- Adding new filter capabilities or ops
- Changing any backend code or API responses
- Preview/simulation of how rules would match (that's a separate feature)

## Decisions

### 1. Utility over component for vocabulary

The vocabulary functions (`opSymbol`, `opLabel`, `groupLabel`, `fieldLabel`, `titlePartLabel`) live in `utils/ruleVocabulary.ts` as pure functions taking `(value, t)`. They're not a composable because they have no reactive state — just string mapping with i18n.

**Alternative considered**: A composable (`useRuleVocabulary()`) that captures `t` once. Rejected because it forces a setup-context dependency — utilities are simpler and the `t` parameter keeps them testable.

### 2. Op symbols for compact display, op labels for dropdowns

Two representations per op:
- `opSymbol(op)` → short symbol for read-only display: `=`, `∋`, `∌`, `>`, `<`, `≈`
- `opLabel(op, t)` → full localized name for dropdown options: "equals", "contains", "greater than"

The symbol form keeps filter condition lines compact; the label form makes dropdowns understandable.

### 3. Collapsible rules in Detail-View reuse the Builder's expand/collapse pattern

The Builder already has click-to-toggle rule cards with a header showing ID + strategy + prio. The Detail-View adopts the same interaction pattern but with read-only content. First rule expanded by default, rest collapsed.

### 4. FilterConditionDisplay.vue upgrade (not replacement)

The existing `FilterConditionDisplay.vue` (created in the schema unification change) is upgraded in place — it already receives `FilterGroupOutput` props. It gets visual grouping containers and uses the shared vocabulary.

### 5. TitleRuleDisplay.vue as new component

Title rules need their own display component because their rendering is structurally different from filters — ordered parts that concatenate to form a title, with type-specific layouts (static shows value, regex shows field→pattern).

### 6. i18n keys in `rule` namespace, not `builder`

Strategy labels currently live under `builder.*` keys. New vocabulary keys go under a dedicated `rule` namespace. The existing strategy keys stay in `builder` for now (moving them is churn with no user impact).

## Risks / Trade-offs

**[Risk] Symbol ambiguity** → `≈` for regex and `∋` for contains may not be universally understood. Mitigation: symbols appear alongside the field name which provides context; tooltips can be added later if needed.

**[Risk] Builder dropdown label length** → Localized labels like "enthält nicht" are longer than "notContains". Mitigation: dropdown widths are already flexible; the ops are few enough that this won't overflow.
