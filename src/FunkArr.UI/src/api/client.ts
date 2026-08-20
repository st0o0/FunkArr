export const API_BASE = '/api/v1'

export function getApiKey(): string | null {
  return localStorage.getItem('funkarr-apikey')
}

export function setApiKey(key: string) {
  localStorage.setItem('funkarr-apikey', key)
}

export async function api<T>(path: string, options?: RequestInit): Promise<T> {
  const apiKey = getApiKey()
  const separator = path.includes('?') ? '&' : '?'
  const url = apiKey ? `${path}${separator}apikey=${encodeURIComponent(apiKey)}` : path

  const response = await fetch(url, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...options?.headers,
    },
  })

  if (!response.ok) {
    const text = await response.text()
    throw new Error(`${response.status}: ${text}`)
  }

  return response.json()
}

export async function apiPost<T>(path: string, body: unknown): Promise<T> {
  return api<T>(path, {
    method: 'POST',
    body: JSON.stringify(body),
  })
}

export async function apiPut<T>(path: string, body: unknown): Promise<T> {
  return api<T>(path, {
    method: 'PUT',
    body: JSON.stringify(body),
  })
}

export async function apiDelete(path: string): Promise<void> {
  const apiKey = getApiKey()
  const separator = path.includes('?') ? '&' : '?'
  const url = apiKey ? `${path}${separator}apikey=${encodeURIComponent(apiKey)}` : path

  const response = await fetch(url, { method: 'DELETE' })
  if (!response.ok) {
    const text = await response.text()
    throw new Error(`${response.status}: ${text}`)
  }
}
