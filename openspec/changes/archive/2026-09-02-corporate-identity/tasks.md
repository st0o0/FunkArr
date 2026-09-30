## 1. Design Tokens

- [x] 1.1 Define `@theme` block in `style.css` with all color tokens (surface, text, border, brand, status), font stacks, and base body styles
- [x] 1.2 Set `surface-base` as body background and `text-body` as default text color

## 2. Sidebar Layout

- [x] 2.1 Rewrite `AppLayout.vue` from top-nav to CSS Grid sidebar layout (sidebar + main areas)
- [x] 2.2 Add sidebar brand wordmark, navigation links with icons, and version footer
- [x] 2.3 Style active nav item (brand-500 left border, brand-900/20 tint) with route matching (including nested RuleSet routes)
- [x] 2.4 Add responsive collapse: sidebar shrinks to 48px below 768px, labels hidden with `sr-only`

## 3. Component Restyling — Dashboard & Health

- [x] 3.1 Restyle `Home.vue` against dark surface tokens (headings, description text, button)
- [x] 3.2 Restyle `HealthWidget.vue`: card on `surface-raised`, status dots with status tokens, fail/warn messages with status colors

## 4. Component Restyling — Setup

- [x] 4.1 Restyle `Setup.vue` step indicators (brand-500 current, status-ok completed, surface-elevated future)
- [x] 4.2 Restyle health check cards with colored left borders and status tokens
- [x] 4.3 Restyle service selection cards (surface-raised, brand-500 selected border)
- [x] 4.4 Restyle configuration tables (surface-raised/elevated) and info callout (brand tint)
- [x] 4.5 Restyle action buttons (primary brand-600, secondary surface-elevated, disabled opacity-40)

## 5. Component Restyling — RuleSets

- [x] 5.1 Restyle `RuleSetList.vue` cards (surface-raised, brand-400 mono ID, brand-500 hover border)
- [x] 5.2 Restyle `RuleSetDetail.vue` sections (surface-raised cards, text-secondary headings, brand-400 mono for rule IDs)
- [x] 5.3 Restyle `ScoringHistory.vue` table (surface-elevated header, text-secondary uppercase, hover rows)
- [x] 5.4 Restyle `ScoringDetail.vue` trace cards (status-ok/border-default left borders, status badges, rule trace expansion)

## 6. Cleanup

- [x] 6.1 Remove `HelloWorld.vue` if still present and unused
- [x] 6.2 Verify all views render correctly with the new tokens — no leftover gray-* hardcoded classes
