# Design: shared-prefilter

## Context

Three independent locations skip/filter Mediathek result items by inspecting
title/topic text for German-language keywords:

- `RuleSetMatchingEngine.AccessibilityKeywords` / `ShouldSkipAccessibility`
  (src/FunkArr/RuleSet/RuleSetMatchingEngine.cs:10-17, 201-211) — used before
  evaluating rules in `EvaluateRules` and `EvaluateRulesWithTraces`.
- `MatchingPipeline.SkipKeywords` / `ShouldSkip`
  (src/FunkArr/Search/MatchingPipeline.cs:10-18, 221-233) — used in the
  generic (non-ruleset) `Execute`/`ExecuteAsync`/`FilterResults` paths.
- `RuleSetGeneratorActor.IsAccessibilityVariant`
  (src/FunkArr/RuleSet/RuleSetGeneratorActor.cs:181-185) — used while
  analyzing sample items to generate a candidate ruleset.

The three lists have already drifted:

| Keyword | RuleSetMatchingEngine | MatchingPipeline | RuleSetGeneratorActor |
|---|---|---|---|
| Audiodeskription | yes | yes | yes |
| Gebärdensprache | yes | yes | yes |
| Gebardensprache (no umlaut) | yes | no | yes |
| klare Sprache | yes | no | yes |
| Hörfassung | yes | yes | no |
| Trailer | no | yes | no |
| Vorschau | no | yes | no |
| Teaser | no | yes | no |

None of the three implementations agree on the full set, and there is no
single place to add a new keyword — a contributor has to know all three
files exist and update them in lockstep, or the lists silently diverge
further (as they already have).

## Goals / Non-Goals

**Goals**
- One canonical, tested definition of every skip keyword.
- Preserve the existing behavioral split: the ruleset engine only ever
  applies accessibility filtering; the generic pipeline applies both
  accessibility and content-type filtering.
- Make adding a keyword a one-file change.
- No behavior change to `RuleSetGeneratorActor`'s pattern analysis beyond
  reusing the shared accessibility check (same four keywords, same
  case-insensitive `Contains` semantics).

**Non-Goals**
- Not introducing configuration-driven or externally-loaded keyword lists
  (e.g. from `appsettings.json`). Keywords stay compiled-in constants; this
  change is purely about deduplication, not making the list runtime-editable.
- Not changing matching/scoring logic, rule evaluation order, or the shape
  of `FilteredTrace`/`MatchedTrace`/`UnmatchedTrace`.
- Not unifying the *call sites* (each of the three files keeps its own
  control flow) — only the keyword data and the `Contains` check move to a
  shared helper.

## Decisions

### Decision: Two-tier keyword system

`ContentFilter` exposes two independent keyword categories instead of one
merged list:

1. **Accessibility keywords** — `Audiodeskription`, `Gebärdensprache`,
   `Gebardensprache`, `klare Sprache`, `Hörfassung`. These mark alternate
   audio/language tracks (audio description, sign language, "easy language"
   narration) that are never the media a user is searching for, regardless
   of which matching path is in use.
2. **Content-type keywords** — `Trailer`, `Vorschau`, `Teaser`. These mark
   promotional/preview items that are not full episodes.

`ContentFilter` provides three methods built from these two lists:

- `IsAccessibilityVariant(string title)` — accessibility keywords only,
  title only. Replaces `RuleSetGeneratorActor.IsAccessibilityVariant`.
- `ShouldSkipAccessibilityOnly(string title)` — accessibility keywords only,
  title only. Replaces `RuleSetMatchingEngine.ShouldSkipAccessibility`. This
  is functionally identical to `IsAccessibilityVariant`; both are kept as
  named entry points because the call sites and their vocabulary predate
  this change (`ShouldSkip*` in the matching engine, `IsAccessibilityVariant`
  in the generator) and callers should not have to know they are the same
  check under the hood.
- `ShouldSkip(string title, string topic)` — accessibility keywords OR
  content-type keywords, checked against both title and topic. Replaces
  `MatchingPipeline.ShouldSkip`.

Why not a single merged keyword list with one `ShouldSkip` method? Because
the ruleset engine's skip check and the generic pipeline's skip check are
*not* the same operation — see the next decision.

### Decision: RuleSet engine never applies content-type filtering

`RuleSetMatchingEngine` intentionally only calls
`ShouldSkipAccessibilityOnly`, never the combined `ShouldSkip`. Rulesets are
authored per-show (community or generated) and may deliberately contain a
rule that matches a "Trailer" or "Teaser" item — for example, a show whose
Mediathek listing has no full-episode uploads for a given week and where the
ruleset author wants a teaser clip to stand in, or a rule that targets a
"Vorschau"-titled item because that is how the broadcaster labels next
week's preview episode for that specific show. Pre-filtering those items out
before rule evaluation would make such rules unsatisfiable and silently
break any ruleset that relies on them.

Accessibility variants (audio description, sign language, "easy language")
are different in kind: they are the *same underlying episode*, dubbed or
narrated for an accessibility need, never a distinct piece of content a
ruleset would intentionally target. Skipping them unconditionally, before
rule evaluation, is safe for every ruleset.

The generic `MatchingPipeline` (used when no ruleset applies) has no
per-show authored intent to protect, so it is free to apply the stricter
combined filter and drop trailers/teasers/previews outright.

### Decision: Placement and shape

`ContentFilter` is a `public static class` in `FunkArr.Shared`
(`src/FunkArr/Shared/ContentFilter.cs`), alongside `FileService` and
`StreamSupervision`. It has no dependencies (no DI registration needed) so
existing static callers (`RuleSetMatchingEngine`, `MatchingPipeline`,
`RuleSetGeneratorActor` are all `static`/use static helpers) can call it
directly without constructor changes.

Keyword arrays stay `private static readonly string[]`, matching the
existing style in all three source files. Matching stays
`string.Contains(keyword, StringComparison.OrdinalIgnoreCase)`, identical to
today's semantics — no regex, no culture-aware comparison, so behavior is
byte-for-byte preserved for existing inputs.

## Risks / Trade-offs

- **Widening effect on `RuleSetMatchingEngine` and `RuleSetGeneratorActor`.**
  Both currently skip on a 4-5 keyword accessibility list that already
  disagree (`RuleSetMatchingEngine` includes `Hörfassung`,
  `RuleSetGeneratorActor` does not). Unifying on the union (all five
  keywords) means `RuleSetGeneratorActor.IsAccessibilityVariant` will now
  also recognize `Hörfassung`-titled items as accessibility variants when
  analyzing sample data for ruleset generation. This is judged correct
  (`Hörfassung` — "audio version for the visually impaired" — belongs in
  the accessibility set) but is a small behavior change worth calling out
  during review.
- **`MatchingPipeline.ShouldSkip` gains two keywords it lacked before**
  (`Gebardensprache` without umlaut, `klare Sprache`), so the generic
  pipeline will now also skip those variants. This is the same
  drift-resolution trade-off: broadening to the union is intentional and
  desired, not an oversight.
- **No externalized configuration.** If operators later want to add
  broadcaster-specific skip keywords without a code change, this design
  does not provide that — it only removes internal duplication. Treated as
  out of scope per Non-Goals; can be a follow-up change if needed.
