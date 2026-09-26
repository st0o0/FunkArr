export interface CheckResult {
  status: number
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
  const res = await fetch('/api/system/setup')
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

export interface VersionResponse {
  appVersion: string
  communityRulesetVersion: string | null
}

async function fetchJson<T>(url: string): Promise<T> {
  const res = await fetch(url)
  if (!res.ok) {
    throw new Error(`${res.status} ${res.statusText}`)
  }
  return res.json()
}

export function getStorageStatus(): Promise<StorageStatusResponse> {
  return fetchJson('/api/system/storage')
}

export function getCacheStats(): Promise<CacheStatsResponse> {
  return fetchJson('/api/system/cache')
}

export function getSystemVersion(): Promise<VersionResponse> {
  return fetchJson('/api/system/version')
}

export interface RouteDefinition {
  name: string
  proxy: string | null
}

export interface ChannelRouteMapping {
  pattern: string
  route: string
}

export interface RoutesResponse {
  definitions: RouteDefinition[]
  channelRoutes: ChannelRouteMapping[]
  defaultRoute: string
}

export function getRoutes(): Promise<RoutesResponse> {
  return fetchJson('/api/system/routes')
}

export interface LogEntry {
  timestamp: string
  level: string
  message: string
  sourceContext: string | null
  exception: string | null
}

export function getLogs(): Promise<LogEntry[]> {
  return fetchJson('/api/system/logs')
}
