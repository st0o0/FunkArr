## 1. Backend — Topic filter on recent matches

- [x] 1.1 Add `topic` query parameter to `RecentMatchActor.GetRecent` message and filter logic (case-insensitive LINQ Where on SearchTopic)
- [x] 1.2 Add `topic` query parameter to `MatchIntelligenceController.GetRecent` endpoint and pass to actor message
- [x] 1.3 Update OpenAPI spec for the new query parameter
- [x] 1.4 Verify `perRuleHitCounts` dictionary key format in ShowActor/TopicStats (confirm it's stringified rule index)

## 2. Header — Gear icon with status dot

- [x] 2.1 Remove "Settings" from the `tabs` array in `App.vue`
- [x] 2.2 Add gear icon SVG and status dot element to the right side of the header bar, wrapped in a `<router-link to="/settings">`
- [x] 2.3 Add `usePolling` for `/api/v1/setup/status` with 30s interval in `App.vue`
- [x] 2.4 Implement status dot color logic: green (all ok), red (critical: !configured or !ffmpeg), amber (degraded)
- [x] 2.5 Hide the dot until first successful status response

## 3. Match detail view — Route and data

- [x] 3.1 Add `/matches/:id` route to `router.ts` pointing to new `MatchDetailView.vue`
- [x] 3.2 Create `MatchDetailView.vue` with data fetching — load recent matches from API, find record by ID client-side, show "Record no longer available" if not found

## 4. Match detail view — Layout

- [x] 4.1 Implement header section: topic, season/episode, timestamp, source badge, total results
- [x] 4.2 Implement summary bar: three stat boxes (matched green, filtered amber, unmatched red)
- [x] 4.3 Implement matched items section: item title, rule index, strategy name, confidence badge (green ≥0.8, amber ≥0.5, red <0.5), episode name
- [x] 4.4 Implement filtered items section: item title, human-readable reason label, filter field/op/value vs. actual value
- [x] 4.5 Implement unmatched items section: item title, per-rule failure pipeline (vertical step list), collapse by default when >5 items
- [x] 4.6 Add footer with link to ruleset detail view (`/rulesets/:topic`)

## 5. Recent matches list — Link to detail

- [x] 5.1 Update `MatchesView.vue` Recent tab: replace inline expand with `<router-link>` to `/matches/:id` for each record

## 6. Ruleset detail — Per-rule hit stats

- [x] 6.1 Fetch `TopicStats` via `GET /api/v1/matches/topics/{tvdbId}` in `RulesetDetail.vue`
- [x] 6.2 Pass per-rule hit count and percentage to each `RuleCard` component
- [x] 6.3 Add mini progress bar with count and percentage to `RuleCard.vue`, show "No match data yet" when no stats available

## 7. Build and verify

- [x] 7.1 Build the Vue frontend (`npm run build` in FunkArr.UI) and verify no errors
- [x] 7.2 Build the .NET backend (`dotnet build`) and verify no errors
- [x] 7.3 Run existing tests (`dotnet run --project FunkArr.Tests/FunkArr.Tests.csproj`) and verify no regressions
