export interface CreateArrResourceRequest {
  url: string
  apiKey: string
  funkArrUrl?: string
}

export interface CreateArrResourceResponse {
  success: boolean
  error?: string
}

async function postSetup(path: string, request: CreateArrResourceRequest): Promise<CreateArrResourceResponse> {
  const res = await fetch(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })
  if (!res.ok) {
    return { success: false, error: `${res.status} ${res.statusText}` }
  }
  return res.json()
}

export function createProwlarrIndexer(request: CreateArrResourceRequest): Promise<CreateArrResourceResponse> {
  return postSetup('/api/setup/prowlarr/indexer', request)
}

export function createSonarrIndexer(request: CreateArrResourceRequest): Promise<CreateArrResourceResponse> {
  return postSetup('/api/setup/sonarr/indexer', request)
}

export function createSonarrDownloadClient(request: CreateArrResourceRequest): Promise<CreateArrResourceResponse> {
  return postSetup('/api/setup/sonarr/download-client', request)
}

export function createRadarrIndexer(request: CreateArrResourceRequest): Promise<CreateArrResourceResponse> {
  return postSetup('/api/setup/radarr/indexer', request)
}

export function createRadarrDownloadClient(request: CreateArrResourceRequest): Promise<CreateArrResourceResponse> {
  return postSetup('/api/setup/radarr/download-client', request)
}
