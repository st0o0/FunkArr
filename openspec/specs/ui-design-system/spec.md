# ui-design-system Specification

## Purpose

Cross-cutting UI design system defining typography, button tiers, content width strategy, and standardized spacing for consistent visual language across all FunkArr views.

## Requirements

### Requirement: Typography system with Inter font

The UI SHALL load the Inter typeface via Google Fonts and define it as the primary `font-sans` value with system fallbacks. The `@theme` block SHALL define `--font-sans: 'Inter', -apple-system, 'Segoe UI', system-ui, sans-serif`. The monospace font stack SHALL remain unchanged.

#### Scenario: Inter font loaded
- **WHEN** the application loads
- **THEN** `index.html` SHALL include a Google Fonts `<link>` element loading Inter with weights 400, 500, and 600

#### Scenario: Font fallback
- **WHEN** Inter fails to load (e.g., offline)
- **THEN** the UI SHALL render using the next available system font in the fallback stack

### Requirement: Type scale

The UI SHALL use a 5-step type scale applied consistently across all views:

| Step | Size | Weight | Usage |
|---|---|---|---|
| page-title | 18px | 600 (semibold) | Page headings (h1) |
| section | 14px | 600 (semibold) | Section headings (h2), card titles |
| body | 13px | 400 (normal) | Body text, nav items, table cells |
| label | 12px | 500 (medium) | Form labels, metadata, secondary info |
| micro | 11px | 400 (normal) | Timestamps, tertiary data |

No text in the UI SHALL be smaller than 11px. The previous `text-[10px]` and `text-[9px]` sizes SHALL NOT be used.

#### Scenario: Page heading rendering
- **WHEN** a page heading renders (e.g., "Overview", "Activity", "RuleSets")
- **THEN** it SHALL use 18px font-size with font-weight 600 and `tracking-tight` (-0.01em)

#### Scenario: Section heading rendering
- **WHEN** a section heading renders within a view (e.g., "Identity", "Matching Rules")
- **THEN** it SHALL use 14px font-size with font-weight 600 in normal case (not uppercase)

#### Scenario: No uppercase tracking-widest labels
- **WHEN** any section label or category header renders
- **THEN** it SHALL NOT use `uppercase tracking-widest` styling
- **AND** SHALL instead use normal case with `font-semibold text-text-secondary`

#### Scenario: Minimum text size
- **WHEN** any text element renders anywhere in the UI
- **THEN** the computed font-size SHALL be at least 11px

### Requirement: Button tier system

The UI SHALL define three button tiers used consistently across all views:

| Tier | Appearance | Usage |
|---|---|---|
| Primary | `bg-accent text-black font-medium rounded-md` | Main action: Save, Confirm, Next |
| Secondary | `bg-surface-elevated border border-border-default text-text-body rounded-md` | Alternate action: Cancel, Back, Re-check |
| Ghost | `text-text-secondary hover:text-text-body` (no background, no border) | Inline actions, links, collapse toggles |

#### Scenario: Primary button rendering
- **WHEN** a primary action button renders (e.g., "Save" in RuleSetBuilder)
- **THEN** it SHALL use amber accent background with black text and font-medium weight

#### Scenario: Secondary button rendering
- **WHEN** an alternate action button renders (e.g., "Cancel" in RuleSetBuilder)
- **THEN** it SHALL use `surface-elevated` background with `border-default` border and `text-body` color

#### Scenario: Ghost button rendering
- **WHEN** an inline action renders (e.g., "Collapse", "View All")
- **THEN** it SHALL use text-only styling with `text-secondary` color and hover to `text-body`

#### Scenario: Disabled button state
- **WHEN** any button is disabled
- **THEN** it SHALL have `opacity-50` and `cursor-not-allowed` regardless of tier

### Requirement: Consistent content widths

All single-column views SHALL use `max-w-3xl mx-auto`. Two-column views (RuleSetBuilder) SHALL use `max-w-5xl mx-auto`. No view SHALL render without a max-width constraint.

#### Scenario: Single-column view width
- **WHEN** Overview, Activity, RuleSetList, RuleSetDetail, ScoringHistory, ScoringDetail, or Setup renders
- **THEN** the view root element SHALL have `max-w-3xl mx-auto`

#### Scenario: Two-column view width
- **WHEN** the RuleSetBuilder renders
- **THEN** the view root element SHALL have `max-w-5xl mx-auto`

### Requirement: Standardized page padding

The AppLayout main content area SHALL use `px-6 py-5` padding consistently.

#### Scenario: Main content padding
- **WHEN** any view renders within the AppLayout
- **THEN** the wrapping container SHALL have `px-6 py-5` padding (replacing the current `px-8 py-6`)
