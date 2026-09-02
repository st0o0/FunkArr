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
  connectionInfo: SetupConnectionInfo
}

export async function getSetupHealth(): Promise<SetupHealthCheck> {
  const res = await fetch('/api/health/setup')
  if (!res.ok) {
    throw new Error(`${res.status} ${res.statusText}`)
  }
  return res.json()
}
