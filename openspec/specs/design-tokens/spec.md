## Purpose

Design token definitions for the FunkArr UI theme: surface colors, text hierarchy, brand accent, semantic status, borders, and font stacks, integrated via Tailwind v4.

## Requirements

### Requirement: Dark-first surface color tokens

The theme SHALL define surface color tokens for layered dark surfaces: `surface-base` (page background), `surface-raised` (cards, sidebar), `surface-elevated` (inputs, table headers, hover states), and `surface-overlay` (modals, dropdowns). Dark mode values SHALL be the default. Surface tones SHALL be neutral (no blue tint).

#### Scenario: Page background uses base surface
- **WHEN** any page renders
- **THEN** the page background uses the `surface-base` token (`#16181d`)

#### Scenario: Cards use raised surface
- **WHEN** a card component renders
- **THEN** the card background uses the `surface-raised` token (`#1e2028`)

#### Scenario: Interactive elements use elevated surface
- **WHEN** a table header, input field, or hovered row renders
- **THEN** the element background uses the `surface-elevated` token (`#272a33`)

#### Scenario: Overlay elements use overlay surface
- **WHEN** a toast, dropdown, or modal renders
- **THEN** the element background uses the `surface-overlay` token (`#31353f`)

### Requirement: Text color hierarchy

The theme SHALL define four text color tokens: `text-primary` for headings and emphasis, `text-body` for body content, `text-secondary` for labels and supporting text, and `text-muted` for placeholders and disabled states. Text colors SHALL be neutral warm (no blue tint).

#### Scenario: Heading text uses primary token
- **WHEN** a heading (`h1`, `h2`) renders
- **THEN** it uses the `text-primary` token (`#f0f0f0`)

#### Scenario: Body text uses body token
- **WHEN** paragraph or table cell text renders
- **THEN** it uses the `text-body` token (`#b8bcc6`)

#### Scenario: WCAG AA contrast on dark background
- **WHEN** `text-body` is rendered on `surface-base`
- **THEN** the contrast ratio SHALL be at least 4.5:1

### Requirement: Brand accent colors

The theme SHALL define brand color tokens in the amber family: `brand-400` (highlights, monospace accents), `brand-500` (active states, progress bars), `brand-600` (primary buttons), `brand-700` (pressed states), `brand-900` (subtle tint backgrounds).

#### Scenario: Primary button uses brand-600
- **WHEN** a primary action button renders
- **THEN** its background uses `brand-600` (`#d97706`)

#### Scenario: Active navigation uses brand-500
- **WHEN** a sidebar nav item is active
- **THEN** its left border uses `brand-500` (`#f59e0b`)

#### Scenario: Accent text uses brand-400
- **WHEN** a ruleset ID or monospace accent renders
- **THEN** it uses `brand-400` (`#fbbf24`)

#### Scenario: Subtle tint uses brand-900
- **WHEN** a brand-tinted background area renders (e.g., active nav item background)
- **THEN** it uses `brand-900` (`#451a03`)

### Requirement: Semantic status colors

The theme SHALL define status color tokens: `status-ok` (green), `status-warn` (amber), `status-fail` (red), and `status-info` (blue).

#### Scenario: Health check success indicator
- **WHEN** a health check result has status `ok`
- **THEN** the status dot uses `status-ok` (`#22C55E`)

#### Scenario: Health check failure indicator
- **WHEN** a health check result has status `fail`
- **THEN** the status dot uses `status-fail` (`#EF4444`)

### Requirement: Border tokens

The theme SHALL define `border-default` for card/divider borders, `border-subtle` for lighter separators (table rows), and `border-focus` for focus rings. Border colors SHALL use neutral white alpha values.

#### Scenario: Card border rendering
- **WHEN** a card renders
- **THEN** it uses a 1px solid `border-default` (`rgba(255, 255, 255, 0.07)`)

#### Scenario: Subtle separator rendering
- **WHEN** a table row separator renders
- **THEN** it uses `border-subtle` (`rgba(255, 255, 255, 0.035)`)

#### Scenario: Focus ring rendering
- **WHEN** an input receives focus
- **THEN** its border uses `border-focus` (`rgba(251, 191, 36, 0.4)`)

### Requirement: Font stack tokens

The theme SHALL define `font-sans` (system UI stack) and `font-mono` (monospace stack with Cascadia Code and JetBrains Mono fallbacks). No web fonts SHALL be loaded.

#### Scenario: Monospace rendering for IDs and patterns
- **WHEN** a ruleset ID, regex pattern, or API path renders
- **THEN** it uses the `font-mono` token

#### Scenario: No network requests for fonts
- **WHEN** the application loads
- **THEN** zero network requests SHALL be made for font files

### Requirement: Tailwind v4 integration

All tokens SHALL be defined via Tailwind v4 `@theme` directive in `style.css`, making them available as utility classes (e.g., `bg-surface-base`, `text-brand-500`).

#### Scenario: Token available as utility class
- **WHEN** a developer writes `bg-surface-raised` in a template
- **THEN** Tailwind generates the corresponding CSS with the token value
