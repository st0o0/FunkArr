export function formatSize(bytes: number): string {
  if (bytes >= 1_073_741_824) return `${(bytes / 1_073_741_824).toFixed(1)} GB`
  if (bytes >= 1_048_576) return `${(bytes / 1_048_576).toFixed(1)} MB`
  if (bytes >= 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${bytes} B`
}

export function formatSpeed(bytesPerSecond: number): string {
  if (bytesPerSecond >= 1_048_576) return `${(bytesPerSecond / 1_048_576).toFixed(1)} MB/s`
  if (bytesPerSecond >= 1024) return `${(bytesPerSecond / 1024).toFixed(1)} KB/s`
  return `${bytesPerSecond} B/s`
}

export function formatDuration(seconds: number): string {
  if (seconds <= 0) return '—'
  const h = Math.floor(seconds / 3600)
  const m = Math.floor((seconds % 3600) / 60)
  const s = seconds % 60
  if (h > 0) return `${h}h ${String(m).padStart(2, '0')}m`
  return `${m}m ${String(s).padStart(2, '0')}s`
}

export function formatRelativeDate(isoString: string): string {
  const date = new Date(isoString)
  const now = Date.now()
  const diffMs = now - date.getTime()
  const diffSeconds = Math.floor(diffMs / 1000)

  if (diffSeconds < 60) return 'just now'
  if (diffSeconds < 3600) return `${Math.floor(diffSeconds / 60)} minutes ago`
  if (diffSeconds < 86400) return `${Math.floor(diffSeconds / 3600)} hours ago`

  return date.toLocaleDateString('de-DE', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}
