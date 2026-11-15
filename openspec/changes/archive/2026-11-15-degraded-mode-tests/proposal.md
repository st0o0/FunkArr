# Degraded Mode Tests

## Why

No tests verify system behavior when external services fail. The QA plan (Part D)
identifies concrete gaps: MVW queue saturation, enrichment circuit breakers, GitHub
update failures, and FFmpeg crashes. All are testable at unit/actor level.

## What Changes

Add tests verifying graceful degradation:
- MVW queue full → callers get `QueryMediathekQueueFull`
- RuleSetUpdater handles GitHub API timeouts and non-success HTTP
- FFmpeg crash mid-remux → worker transitions to Failed and frees slot
- Download worker state transitions on failure scenarios
