## Why

The UI was built view-by-view during feature development and has accumulated structural issues:

1. **Dashboard is a trophy wall.** 8 stat cards, storage bar, cache stats, health widget, active downloads - lots of numbers, none of them answer "is everything okay?" at a glance. Most are interesting once and never again.

2. **Downloads and History are the same pipeline split across two views.** They're used at different moments but conceptually identical - items flowing through stages.

3. **Search is a standalone view disconnected from its only action.** The only reason to search the Mediathek is to understand what candidates look like when authoring RuleSet rules. Standalone Search has no actions - you look at results and then leave. Inside the RuleSetBuilder, search results become test candidates for rule validation.

4. **RuleSets are buried under "System"** despite being the core product workflow.

5. **Visual language is functional dark-mode defaults** with no hierarchy: one button style, inconsistent content widths, uppercase micro-text labels, no type scale.

## What Changes

### Information Architecture

Current nav (6 items, 2 groups):
- Media: Dashboard, Search, Downloads, History
- System: RuleSets, Setup

New nav (4 items, flat):
- Overview (minimal status + recent activity feed)
- Activity (merged Downloads + History with filter tabs)
- RuleSets (promoted to top-level)
- Setup (gear icon at sidebar bottom)

Key decisions:
- **Delete standalone Search view.** Move Mediathek search into RuleSetBuilder's right pane alongside test scoring. Search + Test become one workflow: find real candidates, test rules against them, see results.
- **Merge Downloads + History into Activity.** Three filter tabs: Active, Queued, History. Same pipeline, one view.
- **Radical Dashboard simplification.** Replace 8 stat cards + 4 widgets with: one status line (health + storage), one progress indicator (active/queued/speed), and a short recent activity feed (last ~10 completions/failures). Answer "is everything okay?" in 2 seconds.
- **Promote RuleSets** from System to top-level nav.
- **Remove nav section headers** ("Media", "System") - 4 items need no grouping.
- **Default sidebar expanded** - 4 items don't need space-saving.

### RuleSetBuilder Rework

The right pane changes from a test-only DebuggerPanel to a combined Search + Test panel:
- Search Mediathek for real candidates (currently in standalone Search view)
- Results populate the test candidate list
- "Test Rules" runs the existing test scoring against found candidates
- Results show per-candidate match/fail with rule traces inline

This is the most impactful UX change: the core authoring loop (search -> see candidates -> write rules -> test -> iterate) becomes a single screen instead of bouncing between Search and Builder.

### Visual System (Modern Arr)

- **Color**: Adjust surfaces for more contrast (base #0f0f11, raised #18181b), borders more visible (0.08), single amber accent + dim variant, cool slate secondary for technical elements
- **Typography**: Inter via Google Fonts. 5-step scale (18/14/13/12/11px). Kill uppercase tracking-widest labels. Tighter heading tracking. Minimum 11px.
- **Buttons**: Three tiers - Primary (amber bg, black text), Secondary (elevated bg, border), Ghost (text-only)
- **Layout**: Consistent max-width across all views. Page padding px-6 py-5.
- **Motion**: Remove animate-pulse from download status dots. Keep page transitions.

## Capabilities

### New Capabilities
- `activity-view`: Unified Activity view merging Downloads and History with tab-based filtering
- `ui-design-system`: Consolidated color/type/button/spacing token system in style.css
- `builder-search-panel`: Integrated Mediathek search + test scoring in RuleSetBuilder right pane

### Modified Capabilities
- `dashboard-ui`: Simplified to status line + progress + recent activity feed
- `app-layout`: Sidebar restructure (expanded default, flat nav, Setup as gear icon, 4 items)
- `ruleset-list-ui`: Promoted nav position
- `ruleset-builder-ui`: Right pane becomes Search + Test (absorbs standalone Search functionality)
- `download-queue-ui`: Absorbed into Activity view's Active/Queued tabs
- `download-history-ui`: Absorbed into Activity view's History tab

### Removed Capabilities
- `standalone-search-view`: Deleted - functionality absorbed into RuleSetBuilder

## Impact

- **UI only** - no backend changes, no API changes, no message changes
- **Deleted files**: Search.vue, Queue.vue, History.vue (logic redistributed)
- **AppLayout.vue**: Full rewrite (4 nav items, flat, expanded default, Setup gear icon)
- **style.css**: Full rewrite (new token system, Inter font, button utility classes)
- **Home.vue -> Overview.vue**: Radical simplification (status + recent feed)
- **Queue.vue + History.vue -> Activity.vue**: Merge into single tabbed view
- **RuleSetBuilder.vue + DebuggerPanel.vue**: Rework right pane to Search + Test
- **main.ts**: Route changes (remove /search, /queue, /history; add /activity)
- **All views/components**: Token updates, button tier assignment, typography scale
