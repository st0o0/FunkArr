export interface FeedEntryOutput {
  stableId: string
  firstSeenAt: string
  mediaType: number
  title: string
  channel: string
  topic: string
  quality: number
  season: string | null
  episode: string | null
}

export interface FeedIngestStatusOutput {
  enabled: boolean
  running: boolean
  lastRunAt: string | null
  lastOutcome: string | null
  lastNewItems: number
  lastPublished: number
  lastUnmatched: number
  lastGap: boolean
  consecutiveFailures: number
}

export interface FeedOverviewResponse {
  entries: FeedEntryOutput[]
  total: number
  ingest: FeedIngestStatusOutput
}

export async function getFeed(limit = 100): Promise<FeedOverviewResponse> {
  const res = await fetch(`/api/feed?limit=${limit}`)
  if (!res.ok) {
    throw new Error(`${res.status} ${res.statusText}`)
  }
  return res.json()
}

export async function triggerFeedCrawl(): Promise<void> {
  const res = await fetch('/api/feed/crawl', { method: 'POST' })
  if (!res.ok) {
    throw new Error(`${res.status} ${res.statusText}`)
  }
}
