import type { FeedEntryOutput, FeedOverviewResponse, FeedIngestStatusOutput } from '../../src/api/feed'

let counter = 0

export function makeFeedEntry(overrides?: Partial<FeedEntryOutput>): FeedEntryOutput {
  return {
    stableId: `feed-${++counter}`,
    firstSeenAt: new Date().toISOString(),
    mediaType: 0,
    title: 'Tatort - Angezählt',
    channel: 'ORF',
    topic: 'Tatort',
    quality: 1080,
    season: '2026',
    episode: '22',
    ...overrides,
  }
}

export function makeFeedIngestStatus(overrides?: Partial<FeedIngestStatusOutput>): FeedIngestStatusOutput {
  return {
    enabled: true,
    running: false,
    lastRunAt: new Date().toISOString(),
    lastOutcome: 'completed',
    lastNewItems: 42,
    lastPublished: 38,
    lastUnmatched: 4,
    lastGap: false,
    consecutiveFailures: 0,
    ...overrides,
  }
}

export function makeFeedResponse(overrides?: Partial<FeedOverviewResponse>): FeedOverviewResponse {
  return {
    entries: [makeFeedEntry()],
    total: 1,
    filteredTotal: 1,
    ingest: makeFeedIngestStatus(),
    ...overrides,
  }
}
