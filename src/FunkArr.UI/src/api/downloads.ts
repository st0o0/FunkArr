export interface QueueItem {
  downloadId: string
  title: string
  status: 'Processing' | 'Queued'
  category: string
  totalBytes: number
  bytesDownloaded: number
  percentage: number
  speed: number
  eta: string
}

export interface QueueResponse {
  items: QueueItem[]
  totalSlots: number
}

export interface HistoryItem {
  downloadId: string
  title: string
  category: string
  totalBytes: number
  downloadTimeSeconds: number
  filePath: string | null
  status: 'Completed' | 'Failed'
  failMessage: string | null
  completedAt: string
}

export interface HistoryResponse {
  items: HistoryItem[]
  totalItems: number
}

async function fetchJson<T>(url: string): Promise<T> {
  const res = await fetch(url)
  if (!res.ok) {
    throw new Error(`${res.status} ${res.statusText}`)
  }
  return res.json()
}

async function fetchAction(url: string, method: string): Promise<{ success: boolean; error?: string }> {
  const res = await fetch(url, { method })
  return res.json()
}

export function getQueue(): Promise<QueueResponse> {
  return fetchJson('/api/downloads/queue')
}

export function getHistory(start = 0, limit = 25, category?: string): Promise<HistoryResponse> {
  const params = new URLSearchParams({ start: String(start), limit: String(limit) })
  if (category) params.set('category', category)
  return fetchJson(`/api/downloads/history?${params}`)
}

export function deleteQueueItem(id: string) {
  return fetchAction(`/api/downloads/queue/${id}`, 'DELETE')
}

export function deleteHistoryItem(id: string) {
  return fetchAction(`/api/downloads/history/${id}`, 'DELETE')
}

export function retryDownload(id: string) {
  return fetchAction(`/api/downloads/${id}/retry`, 'POST')
}
