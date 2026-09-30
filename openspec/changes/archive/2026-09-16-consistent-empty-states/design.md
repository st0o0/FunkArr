## Context

`EmptyState.vue` already exists with a clean API: `icon` (SVG path), `title`, optional `description`, and a default slot for CTAs. Activity and RuleSet-List already use it. Three views don't.

## Goals / Non-Goals

**Goals:**
- Every empty state in the app uses `EmptyState.vue`
- Every empty state explains when/how content will appear

**Non-Goals:**
- Redesigning the EmptyState component itself (it's already good)
- Adding CTAs to every empty state (only where there's an actionable next step)

## Decisions

### 1. Reuse existing component as-is

No changes to `EmptyState.vue`. It already has icon, title, description, and slot. Just adopt it in the three views that don't use it yet.

### 2. Consistent icon vocabulary

- Scoring-History: clock/history icon (data appears after scoring runs)
- Home recent-activity: download icon (already used in Activity empty states)
- RuleSet-Detail no-rules: list/rules icon (no matching rules configured)

### 3. Every description explains the trigger

Not just "nothing here" but "X appears when Y happens." This is already the pattern in Activity and Home (noRecentActivityHint exists but isn't shown via EmptyState).
