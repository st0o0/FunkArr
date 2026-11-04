export function makeHealthChecks() {
  return {
    checks: {
      apiKey: { status: 0, masked: 'funk***01' },
      mediathekViewWeb: { status: 0, message: 'Connected' },
      dataDirectory: { status: 0, path: '/app/data' },
      completeDirectory: { status: 0, path: '/shared/downloads/complete' },
      incompleteDirectory: { status: 0, path: '/shared/downloads/incomplete' },
      indexerApi: { status: 0 },
      downloadApi: { status: 0 },
      ffmpeg: { status: 0, version: '7.1' },
    },
    setupConnectionInfo: {
      indexerApiPath: '/index/api',
      downloadApiPath: '/download/api',
      defaultPort: 6969,
    },
  }
}

export function makeStorageStatus() {
  return {
    completeDirectory: { path: '/shared/downloads/complete', availableBytes: 500_000_000_000, totalBytes: 1_000_000_000_000 },
    incompleteDirectory: { path: '/shared/downloads/incomplete', availableBytes: 500_000_000_000, totalBytes: 1_000_000_000_000 },
  }
}

export function makeVersion() {
  return { appVersion: '0.1.0-dev', communityRulesetVersion: 'v0.31' }
}

export function makeCacheStats() {
  return { tvdbEntries: 42, tmdbEntries: 18, oldestEntry: new Date(Date.now() - 86400000).toISOString() }
}

export function makeRoutes() {
  return {
    definitions: [
      { name: 'direct', proxy: null },
      { name: 'at-proxy', proxy: 'gluetun-at:8888' },
    ],
    channelRoutes: [
      { pattern: 'ORF*', route: 'at-proxy' },
    ],
    defaultRoute: 'direct',
  }
}

export function makeVariants() {
  return {
    variants: [
      { variant: 0, policy: 1 },
      { variant: 1, policy: 0 },
      { variant: 2, policy: 0 },
      { variant: 3, policy: 0 },
      { variant: 4, policy: 1 },
    ],
  }
}

export function makeLogEntries() {
  return [
    { timestamp: new Date().toISOString(), level: 'Information', message: 'Application started', sourceContext: 'FunkArr', exception: null },
    { timestamp: new Date().toISOString(), level: 'Warning', message: 'Slow query detected', sourceContext: 'FunkArr.Search', exception: null },
    { timestamp: new Date().toISOString(), level: 'Error', message: 'Connection refused', sourceContext: 'FunkArr.Download', exception: 'System.Net.Http.HttpRequestException' },
  ]
}
