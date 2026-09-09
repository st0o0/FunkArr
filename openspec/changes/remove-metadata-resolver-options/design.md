## Context

`MetadataResolverOptions` is an `IOptions<T>` class in FunkArr.Core that provides fallback defaults (strategy="fuzzy", threshold=0.7, airdate tolerance=7 days) for episode resolution. The `ResolutionConfig` record in FunkArr.Messages already has identical defaults in its constructor. Additionally, RuleSets can carry a per-RuleSet `resolution` section that flows through `MatchingConfig.Resolution` and `ScoreCompleted.Resolution`. No actual RuleSet uses this override.

The result is three redundant layers of defaulting for three values that never vary.

## Goals / Non-Goals

**Goals:**
- Remove `MetadataResolverOptions` and its DI registration entirely
- Remove the per-RuleSet `Resolution` passthrough from `MatchingConfig`, `ScoreCompleted`, and `RuleSetMerger`
- `TvSearchWorker` uses `new ResolutionConfig()` directly (record defaults)
- Zero behavioral change - same defaults apply everywhere

**Non-Goals:**
- Changing the resolution strategies or their behavior
- Removing `ResolutionConfig` itself (it's still the message contract for `ResolveEpisodes`)
- Modifying persistence DTOs

## Decisions

### Use ResolutionConfig record defaults instead of options class

The `ResolutionConfig` record already defines the same defaults. `new ResolutionConfig()` produces the exact same values that `MetadataResolverOptions` provided.

### Remove per-RuleSet resolution override entirely

Rather than keeping the plumbing but just removing the options class, we remove the entire per-RuleSet override path. Since no RuleSet uses it, the code is dead weight.

### Keep ResolutionConfig as a message record

`ResolutionConfig` is the parameter to `ResolveEpisodes` and is used internally by the MetadataResolver domain. It stays.

## Risks / Trade-offs

- **Lost extension point**: Per-RuleSet resolution tuning becomes impossible without re-adding the plumbing. Mitigated by: no RuleSet has ever used it.
- **Hardcoded defaults**: Resolution defaults can no longer be changed via appsettings. Mitigated by: the defaults have never been changed.
