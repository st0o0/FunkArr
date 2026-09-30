## 1. Color Palette & Design Tokens

- [x] 1.1 Replace all color tokens in `style.css` @theme block with warm neutral palette (surfaces, brand amber, text, borders)
- [x] 1.2 Update all `bg-brand-*`, `text-brand-*`, `border-brand-*` references across all Vue files to work with amber tones (verify no blue-specific assumptions like `bg-brand-900/30` that need adjustment)
- [x] 1.3 Verify WCAG AA contrast ratios for text-primary/body/secondary on all surface levels

## 2. Shared Components

- [x] 2.1 Create `AppBreadcrumb.vue` component accepting items array `[{label, to?}]` with chevron separators
- [x] 2.2 Create `SkeletonLine.vue`, `SkeletonCard.vue`, `SkeletonTable.vue` with CSS shimmer animation
- [x] 2.3 Create `useToast` composable with reactive toast array, `toast(message, variant?)` method, auto-dismiss 3s
- [x] 2.4 Create `ToastContainer.vue` with Teleport to body, bottom-right positioning, enter/leave transitions, max 3 visible
- [x] 2.5 Create `EmptyState.vue` component accepting icon (SVG path string), title, and description props

## 3. Sidebar & Layout

- [x] 3.1 Refactor `AppLayout.vue` sidebar to support collapsed (56px) and expanded (200px) modes with CSS width transition
- [x] 3.2 Add sidebar toggle button, localStorage persistence (`funkarr-sidebar`), tooltip on hover in collapsed mode
- [x] 3.3 Update active nav item styling from `bg-brand-600/15` to amber left-border accent
- [x] 3.4 Remove global `max-w-6xl` from AppLayout main content area, move padding to slot wrapper

## 4. Route Transitions

- [x] 4.1 Wrap `<router-view>` in `App.vue` with `<Transition name="page" mode="out-in">` and add CSS for fade + slide-up (150ms)

## 5. Card Hierarchy & Button Variants

- [x] 5.1 Update `QueueCard.vue` to Level 2 card style (hover lift, no visible border at rest)
- [x] 5.2 Update RuleSet list items in `RuleSetList.vue` to Level 2 card style
- [x] 5.3 Update table rows in `History.vue` and `ScoringHistory.vue` to Level 3 style (hover:bg-surface-elevated/60)
- [x] 5.4 Standardize all buttons across views to four variants (primary/secondary/ghost/danger)

## 6. Per-View Content Widths

- [x] 6.1 Set per-view max-width: Queue (max-w-4xl), RuleSetList/Detail (max-w-4xl), Setup (max-w-2xl), ScoringDetail (max-w-5xl)
- [x] 6.2 Remove max-width from Dashboard, History, ScoringHistory, RuleSetBuilder (full width)
- [x] 6.3 Update RuleSetBuilder grid from `grid-cols-2` to `grid-cols-[1fr_380px]`

## 7. Loading Skeletons per View

- [x] 7.1 Replace "Loading..." in `Home.vue` (HealthWidget, ActiveDownloads) with SkeletonCard
- [x] 7.2 Replace "Loading..." in `Queue.vue` with SkeletonCard stack
- [x] 7.3 Replace "Loading..." in `History.vue` with SkeletonTable
- [x] 7.4 Replace "Loading..." in `RuleSetList.vue` with SkeletonCard stack
- [x] 7.5 Replace "Loading..." in `RuleSetDetail.vue` and `RuleSetBuilder.vue` with appropriate skeletons
- [x] 7.6 Replace "Loading..." in `ScoringHistory.vue` and `ScoringDetail.vue` with SkeletonTable/SkeletonCard
- [x] 7.7 Replace "Running checks..." in `Setup.vue` health check step with skeleton

## 8. Empty States per View

- [x] 8.1 Replace bare "No active or queued downloads" in `Queue.vue` with EmptyState (download icon + description)
- [x] 8.2 Replace bare "No download history" in `History.vue` with EmptyState
- [x] 8.3 Replace bare "No rulesets registered" in `RuleSetList.vue` with EmptyState including "New RuleSet" CTA
- [x] 8.4 Replace bare empty text in `ActiveDownloads.vue` and `HealthWidget.vue` with compact empty states

## 9. Toast Integration

- [x] 9.1 Mount `ToastContainer` in `App.vue`
- [x] 9.2 Add toast calls to `RuleSetBuilder.vue` on save success/error
- [x] 9.3 Add toast calls to `History.vue` on delete/retry
- [x] 9.4 Add toast calls to `Queue.vue` on cancel
- [x] 9.5 Add toast calls to `RuleSetDetail.vue` on delete success/error
- [x] 9.6 Add toast to clipboard copy in `Setup.vue`

## 10. Micro-Interactions

- [x] 10.1 Add `active:scale-[0.98]` press feedback to all primary/secondary/danger buttons
- [x] 10.2 Update progress bar transitions in `QueueCard.vue` and `ActiveDownloads.vue` to `duration-700 ease-out`
- [x] 10.3 Add sticky `thead` to tables in `History.vue`, `ScoringHistory.vue`
- [x] 10.4 Ensure all clickable table rows have `cursor-pointer` and consistent hover state

## 11. Breadcrumb Migration

- [x] 11.1 Replace manual breadcrumb markup in `RuleSetDetail.vue` with `AppBreadcrumb`
- [x] 11.2 Replace manual breadcrumb markup in `RuleSetBuilder.vue` with `AppBreadcrumb`
- [x] 11.3 Replace manual breadcrumb markup in `ScoringHistory.vue` with `AppBreadcrumb`
- [x] 11.4 Replace manual breadcrumb markup in `ScoringDetail.vue` with `AppBreadcrumb`
