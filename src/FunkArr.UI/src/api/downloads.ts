export interface QueueItem {
  downloadId: string
  title: string
  status: number
  channel: string
  category: string
  hasSubtitles: boolean
  totalDuration: number
  totalBytes: number
  bytesDownloaded: number
  percentage: number
  phase: string
  speed: number
  eta: string
  priority: string
}

export interface QueueResponse {
  items: QueueItem[]
  totalSlots: number
  activeCount: number
  queuedCount: number
  isPaused: boolean
  isScheduleActive: boolean
  nextWindow: string | null
}

export interface HistoryItem {
  downloadId: string
  title: string
  category: string
  totalBytes: number
  downloadTimeSeconds: number
  filePath: string | null
  status: number
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

async function fetchJsonAction(url: string, method: string, body: unknown): Promise<{ success: boolean; error?: string }> {
  const res = await fetch(url, {
    method,
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
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

export interface HistoryStatsResponse {
  totalCompleted: number
  totalFailed: number
  totalBytes: number
  averageDownloadTimeSeconds: number
  successRate: number
}

export function getHistoryStats(): Promise<HistoryStatsResponse> {
  return fetchJson('/api/downloads/history/stats')
}

export function getHistoryCategories(): Promise<string[]> {
  return fetchJson('/api/downloads/history/categories')
}

export function moveDownload(id: string, position: number, priority?: string) {
  const body: Record<string, unknown> = { position }
  if (priority) body.priority = priority
  return fetchJsonAction(`/api/downloads/queue/${id}/move`, 'POST', body)
}

export function setDownloadPriority(id: string, priority: string) {
  return fetchJsonAction(`/api/downloads/queue/${id}/priority`, 'POST', { priority })
}

export function forceStartDownload(id: string) {
  return fetchAction(`/api/downloads/queue/${id}/force-start`, 'POST')
}

export function pauseDownloads() {
  return fetchAction('/api/downloads/pause', 'POST')
}

export function resumeDownloads() {
  return fetchAction('/api/downloads/resume', 'POST')
}

export interface DownloadSettingsResponse {
  concurrentDownloads: number
  schedule: { start: string; end: string }[]
}

export function getDownloadSettings(): Promise<DownloadSettingsResponse> {
  return fetchJson('/api/downloads/settings')
}
