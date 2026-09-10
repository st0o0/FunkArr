export interface MediathekSearchItem {
  title: string
  topic: string
  channel: string
  duration: number
  quality: number
  description: string | null
  timestamp: number
  size: number
  hasSubtitles: boolean
  hasHd: boolean
  websiteUrl: string | null
}

export interface MediathekSearchResponse {
  items: MediathekSearchItem[]
  totalResults: number
}

export interface MediathekSearchParams {
  q?: string
  channel?: string
  topic?: string
  durationMin?: number
  durationMax?: number
  offset?: number
  limit?: number
  sortBy?: string
  sortOrder?: string
}

export async function searchMediathek(params: MediathekSearchParams): Promise<MediathekSearchResponse> {
  const urlParams = new URLSearchParams()
  if (params.q) urlParams.set('q', params.q)
  if (params.channel) urlParams.set('channel', params.channel)
  if (params.topic) urlParams.set('topic', params.topic)
  if (params.durationMin != null) urlParams.set('durationMin', String(params.durationMin))
  if (params.durationMax != null) urlParams.set('durationMax', String(params.durationMax))
  if (params.offset != null) urlParams.set('offset', String(params.offset))
  if (params.limit != null) urlParams.set('limit', String(params.limit))
  if (params.sortBy) urlParams.set('sortBy', params.sortBy)
  if (params.sortOrder) urlParams.set('sortOrder', params.sortOrder)

  const res = await fetch(`/api/mediathek/search?${urlParams}`)
  if (!res.ok) {
    throw new Error(`${res.status} ${res.statusText}`)
  }
  return res.json()
}
