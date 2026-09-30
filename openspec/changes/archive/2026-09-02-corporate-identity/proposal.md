## Why

The FunkArr UI uses default Tailwind gray utility classes with a light background and top-nav header. It reads as a scaffold rather than a product. A defined visual identity with a dark-first palette, sidebar navigation, and consistent component patterns turns it into something users take seriously and enjoy using alongside their *arr stack.

## What Changes

- Replace the top-nav `AppLayout` with a sidebar layout (dark sidebar, dark page background)
- Introduce a Mediathek Teal (`#14B8A6` family) brand accent color system
- Define dark-first color tokens for surfaces, text, borders, and semantic states
- Restyle all existing views and components against the new design tokens
- Add responsive sidebar collapse (icons-only on narrow screens)
- Prepare light-mode token overrides (sidebar stays dark in both themes)

## Capabilities

### New Capabilities

- `design-tokens`: Tailwind CSS custom theme tokens (colors, typography, spacing) that define the FunkArr visual identity. Dark-first surface scale, teal accent, semantic status colors, system font stack.
- `sidebar-layout`: Sidebar navigation component replacing the top-nav header. Collapsible to icon-only on narrow viewports. Dark in both light and dark themes.

### Modified Capabilities

- `ruleset-ui`: Restyled to use dark surfaces, teal accents, updated card/table patterns
- `setup-guide-ui`: Restyled wizard steps, health checks, and service config against new design tokens
- `static-file-serving`: No spec-level change (serving mechanism unchanged)

## Impact

- `FunkArr.UI/src/style.css` — design token definitions
- `FunkArr.UI/src/components/AppLayout.vue` — replaced with sidebar layout
- All views (`Home`, `Setup`, `RuleSetList`, `RuleSetDetail`, `ScoringHistory`, `ScoringDetail`) — restyled
- `FunkArr.UI/src/components/HealthWidget.vue` — restyled
- No backend changes. No API changes. No dependency additions (pure Tailwind v4).
