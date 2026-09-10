export interface CheckResult {
  status: 'ok' | 'warn' | 'fail'
  message?: string
  value?: string
  masked?: string
  path?: string
  version?: string
}

export interface SetupConnectionInfo {
  indexerApiPath: string
  downloadApiPath: string
  defaultPort: number
}

export interface SetupHealthCheck {
  checks: Record<string, CheckResult>
  setupConnectionInfo: SetupConnectionInfo
}

export async function getSetupHealth(): Promise<SetupHealthCheck> {
  const res = await fetch('/api/health/setup')
  if (!res.ok) {
    throw new Error(`${res.status} ${res.statusText}`)
  }
  return res.json()
}

export interface StorageDirectory {
  path: string
  availableBytes: number
  totalBytes: number
}

export interface StorageStatusResponse {
  completeDirectory: StorageDirectory
  incompleteDirectory: StorageDirectory
}

export interface CacheStatsResponse {
  tvdbEntries: number
  tmdbEntries: number
  oldestEntry: string | null
}

async function fetchJson<T>(url: string): Promise<T> {
  const res = await fetch(url)
  if (!res.ok) {
    throw new Error(`${res.status} ${res.statusText}`)
  }
  return res.json()
}

export function getStorageStatus(): Promise<StorageStatusResponse> {
  return fetchJson('/api/health/storage')
}

export function getCacheStats(): Promise<CacheStatsResponse> {
  return fetchJson('/api/health/cache')
}
