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
  filteredTotal: number
  ingest: FeedIngestStatusOutput
}

export interface FeedQueryParams {
  limit?: number
  offset?: number
  type?: string
  channel?: string
  quality?: number
  q?: string
}

export async function getFeed(params: FeedQueryParams = {}): Promise<FeedOverviewResponse> {
  const searchParams = new URLSearchParams()
  if (params.limit) searchParams.set('limit', String(params.limit))
  if (params.offset) searchParams.set('offset', String(params.offset))
  if (params.type) searchParams.set('type', params.type)
  if (params.channel) searchParams.set('channel', params.channel)
  if (params.quality) searchParams.set('quality', String(params.quality))
  if (params.q) searchParams.set('q', params.q)

  const qs = searchParams.toString()
  const res = await fetch(`/api/feed${qs ? `?${qs}` : ''}`)
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
