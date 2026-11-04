import type { QueueItem, QueueResponse } from '../../src/api/downloads'

let counter = 0

export function makeQueueItem(overrides?: Partial<QueueItem>): QueueItem {
  const id = `queue-${++counter}-${Date.now()}`
  return {
    downloadId: id,
    title: 'Test.Show.S01E01.GERMAN.1080p.WEB.h264-FunkArr',
    status: 0,
    channel: 'ARD',
    category: 'tv',
    hasSubtitles: true,
    totalDuration: 2882,
    totalBytes: 637_000_000,
    bytesDownloaded: 425_000_000,
    percentage: 67,
    phase: 0,
    speed: 12_400_000,
    eta: '2:14',
    priority: 0,
    ...overrides,
  }
}

export function makeQueueResponse(overrides?: Partial<QueueResponse>): QueueResponse {
  const items = overrides?.items ?? []
  return {
    items,
    totalSlots: 2,
    activeCount: items.filter(i => i.status === 0).length,
    queuedCount: items.filter(i => i.status === 1).length,
    isPaused: false,
    isScheduleActive: true,
    nextWindow: null,
    ...overrides,
  }
}
