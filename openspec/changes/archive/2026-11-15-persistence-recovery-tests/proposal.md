# Persistence Recovery Tests

## Why

No actor restart or recovery test exists anywhere in the test suite today. All 6
persistent actors (DownloadManager, DownloadWorker, DownloadHistoryManager,
FeedCrawlManager, FeedStoreManager, HistoryWorker) lack tests that verify state
survives restart via journal replay and snapshot recovery. This is the single
largest gap in the QA plan (Part B).

## What

Add recovery tests for each persistent actor using Akka TestKit's in-memory
persistence. Each test creates an actor, persists events via commands, stops and
recreates the actor (triggering journal replay), then verifies state via queries.

## Scope

- DownloadManager: snapshot restore, ResetDispatched, priority, pause state
- DownloadWorker: journal replay, transient phase reset, Failed recovery
- DownloadHistoryManager: snapshot + event replay, trim survival
- FeedCrawlManager: cursor, ConsecutiveFailures, harvester child
- FeedStoreManager: entries from snapshot + events, trim timer
- HistoryWorker: OnReplaySuccess trims, configurable interval, stats after replay
