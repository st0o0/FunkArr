# UI Scale & Polish

## Problem

FunkArr's UI was built for small-scale testing. Now that real usage with 59+ RuleSets and 50+ concurrent downloads is happening, several UX problems surface:

1. **Unreadable titles** — Release names like `Lass.dich.ueberwachen.S2025E01.Ein.unvergesslicher.Abend.-.fuers.Showpublikum.S2025E01.GERMAN.1080p.WEB.h264-FunkArr` are optimized for Sonarr, not humans.
2. **No grouping in Queue** — 55 items from different series are mixed in a flat list. Finding "all Babylon Berlin downloads" requires scrolling.
3. **Queue renders all items** — SSE stream sends the full queue every 3s, Vue renders every card. Works at 55 items, breaks at 500+.
4. **Dashboard lacks stats** — No total speed, no queue count summary, orphaned "View RuleSets" button.
5. **Minor polish gaps** — RuleSets not sorted, hardcoded category filter, no counts shown.

## Scope

Frontend-only changes in `FunkArr.UI`. No backend API changes required (History pagination already exists server-side, Queue SSE stream provides all data needed for client-side grouping).

## Capabilities

### 1. Readable Titles (all pages)
Parse release names into structured display: series name, season/episode, episode title, quality badge. Show the raw release name on hover/tooltip.

### 2. Queue Grouping
Group downloads by series with collapsible sections. Show per-group summary (count, active/queued, combined progress). Default collapsed for queued groups, expanded for downloading.

### 3. Queue Virtualization
Only render visible items. At 500+ items, rendering all cards causes jank. Use a lightweight virtual scroll approach or paginate client-side from the SSE data.

### 4. Dashboard Polish
- Replace "View RuleSets" button with stats row (total queue size, active speed, completed today)
- Show more than 3 active downloads in the widget (scrollable or summarized)

### 5. Small Fixes
- Sort RuleSets alphabetically by topic
- Show count on RuleSets page ("61 rulesets")
- Dynamic category list in History filter (from actual data, not hardcoded)
- Scoring Detail: filter toggle for matched/unmatched

## Priority Order

1. Readable titles (high impact, touches all pages)
2. Queue grouping (high impact for queue usability)  
3. Dashboard polish (medium impact, quick wins)
4. Small fixes (low effort, nice polish)
5. Queue virtualization (only needed at scale, can defer)

## Out of Scope

- Backend API changes
- New pages or navigation changes
- RuleSet builder improvements
- Mobile responsiveness overhaul
