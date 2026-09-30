## Why

The FunkArr UI is functionally complete but feels lifeless to navigate. The color palette is cold and monotone (deep navy surfaces + blue-only brand), every card and section uses the same visual treatment, there are no transitions between routes, loading states show raw "Loading..." text, and user actions (save, delete, copy) give no visible feedback. The result is an app that works but doesn't feel good to use.

## What Changes

- **Color palette overhaul**: Replace the cold navy surface tones (#0c0f1a base) with neutral warm darks. Replace the blue brand color with warm amber to give FunkArr a distinct identity in the *arr* family. Warm up all text colors (remove blue tint).
- **Collapsible sidebar**: Sidebar collapses to icon-only (56px) with smooth transition, expands on hover or toggle. State persisted in localStorage. Active nav item uses amber accent.
- **Per-view content widths**: Remove the global `max-w-6xl` constraint. Tables get full width, forms get focused narrow width, dashboard fills available space.
- **Card hierarchy**: Introduce three visual levels (section card, list item, inline row) instead of one uniform card style. Add proper button variants (primary, secondary, ghost, danger).
- **Route transitions**: Fade/slide transitions on `<router-view>` for smooth navigation feel.
- **Loading skeletons**: Replace all "Loading..." text with skeleton shimmer components that match the shape of real content.
- **Toast notification system**: `useToast()` composable with auto-dismissing toasts for all user actions (save, delete, copy, cancel, error).
- **Empty states**: Replace bare text ("No active downloads.") with icon + descriptive text that explains what would appear and how.
- **Micro-interactions**: Hover lifts on cards, press feedback on buttons, smoother progress bar transitions, consistent hover states on table rows.
- **Breadcrumb component**: Extract repeated breadcrumb pattern into a shared `AppBreadcrumb.vue` component.

## Capabilities

### New Capabilities

- `color-palette`: Warm neutral dark theme with amber brand color, replacing the current cold navy/blue palette. Defines all surface, text, border, brand, and status color tokens.
- `collapsible-sidebar`: Sidebar that toggles between icon-only (56px) and expanded (200px) modes with smooth CSS transition. State persisted in localStorage.
- `route-transitions`: Vue transition wrapper around router-view providing fade+slide-up animation on route changes.
- `loading-skeletons`: Reusable skeleton shimmer components (SkeletonBlock, SkeletonTable, SkeletonCard) replacing text-based loading indicators across all views.
- `toast-notifications`: Composable-based toast system with auto-dismiss, positioned bottom-right, supporting success/error/info variants.
- `empty-states`: Consistent empty state pattern (icon + title + description) for Queue, History, RuleSets, and Dashboard widgets.
- `micro-interactions`: Hover lifts, press feedback, smooth progress transitions, and consistent interactive states across all clickable elements.
- `breadcrumb-component`: Shared AppBreadcrumb.vue replacing duplicated breadcrumb markup in RuleSetDetail, RuleSetBuilder, ScoringHistory, and ScoringDetail.
- `card-hierarchy`: Three-level visual system (section card, list item, inline row) with four button variants (primary, secondary, ghost, danger).
- `per-view-content-width`: View-specific content width constraints replacing the single global max-w-6xl.

### Modified Capabilities

- `design-tokens`: All color tokens replaced (surfaces, brand, text, border, focus colors).
- `sidebar-layout`: Sidebar becomes collapsible with icon-only mode; grid changes from fixed 200px to dynamic.
- `download-queue-ui`: Queue view gets loading skeleton, empty state, toast on cancel, card hover effects.
- `download-history-ui`: History view gets loading skeleton, sticky table header, empty state, toast on delete/retry, row animations.
- `ruleset-ui`: RuleSet list gets loading skeleton, empty state, card hover effects.
- `ruleset-builder-ui`: Builder gets asymmetric layout (form wider than debugger), toast on save.
- `ruleset-debugger-ui`: No behavior change, only visual alignment with new card hierarchy.
- `setup-guide-ui`: Setup wizard gets loading skeleton, adjusted button variants.

## Impact

- **FunkArr.UI only** — no backend changes, no API changes, no .NET code touched.
- **Files affected**: `style.css` (palette), `AppLayout.vue` (sidebar + router-view transition), all 8 views, 5 existing components, plus ~6 new components (SkeletonBlock, ToastContainer, AppBreadcrumb, EmptyState, etc.).
- **Dependencies**: None added. Pure Vue 3 + Tailwind CSS.
- **Risk**: Low. Purely cosmetic/UX layer. No data flow or API changes.
