import { describe, test, expect } from 'vitest'
import { queueItemToRow, historyItemToRow, priorityLabel } from '../utils/activityMapping'
import type { QueueItem, HistoryItem } from '../api/downloads'

const t = (key: string) => key

function makeQueueItem(overrides: Partial<QueueItem> = {}): QueueItem {
  return {
    downloadId: 'dl-1', title: 'Test.S01E01.1080p-FunkArr', status: 0, channel: 'ARD',
    category: 'tv', hasSubtitles: true, totalDuration: 2882, totalBytes: 637_000_000,
    bytesDownloaded: 400_000_000, percentage: 63, phase: 0, speed: 12_000_000,
    eta: '1:30', priority: 0, ...overrides,
  }
}

function makeHistoryItem(overrides: Partial<HistoryItem> = {}): HistoryItem {
  return {
    downloadId: 'h-1', title: 'Tatort.S2026E22.1080p-FunkArr', category: 'tv',
    totalBytes: 1_200_000_000, downloadTimeSeconds: 842, filePath: '/test.mkv',
    status: 0, failMessage: null, completedAt: new Date().toISOString(), ...overrides,
  }
}

describe('queueItemToRow', () => {
  test('active item has type active', () => {
    const row = queueItemToRow(makeQueueItem({ status: 0 }), 0, t)
    expect(row.type).toBe('active')
    expect(row.isQueue).toBe(true)
  })

  test('active item shows Downloading for phase 0', () => {
    const row = queueItemToRow(makeQueueItem({ status: 0, phase: 0 }), 0, t)
    expect(row.statusLabel).toBe('Downloading')
    expect(row.phaseLabel).toBe('Downloading')
  })

  test('active item shows Remuxing for phase 1', () => {
    const row = queueItemToRow(makeQueueItem({ status: 0, phase: 1 }), 0, t)
    expect(row.phaseLabel).toBe('Remuxing')
  })

  test('active item shows Moving for phase 2', () => {
    const row = queueItemToRow(makeQueueItem({ status: 0, phase: 2 }), 0, t)
    expect(row.phaseLabel).toBe('Moving')
  })

  test('queued item has type queued', () => {
    const row = queueItemToRow(makeQueueItem({ status: 1 }), 0, t)
    expect(row.type).toBe('queued')
    expect(row.statusDot).toBe('bg-text-muted')
  })

  test('position is 1-indexed', () => {
    expect(queueItemToRow(makeQueueItem(), 0, t).position).toBe(1)
    expect(queueItemToRow(makeQueueItem(), 4, t).position).toBe(5)
  })

  test('size formatted when > 0', () => {
    const row = queueItemToRow(makeQueueItem({ totalBytes: 637_000_000 }), 0, t)
    expect(row.size).toBe('607.5 MB')
  })

  test('size is dash when 0', () => {
    const row = queueItemToRow(makeQueueItem({ totalBytes: 0 }), 0, t)
    expect(row.size).toBe('-')
  })

  test('preserves channel', () => {
    const row = queueItemToRow(makeQueueItem({ channel: 'ZDF' }), 0, t)
    expect(row.channel).toBe('ZDF')
  })
})

describe('historyItemToRow', () => {
  test('completed item has type completed', () => {
    const row = historyItemToRow(makeHistoryItem({ status: 0 }))
    expect(row.type).toBe('completed')
    expect(row.statusLabel).toBe('Completed')
    expect(row.statusDot).toBe('bg-status-ok')
  })

  test('failed item has type failed', () => {
    const row = historyItemToRow(makeHistoryItem({ status: 1, failMessage: 'I/O error' }))
    expect(row.type).toBe('failed')
    expect(row.statusLabel).toBe('Failed')
    expect(row.statusDot).toBe('bg-status-fail')
    expect(row.failMessage).toBe('I/O error')
  })

  test('position is null for history items', () => {
    expect(historyItemToRow(makeHistoryItem()).position).toBeNull()
  })

  test('isQueue is false', () => {
    expect(historyItemToRow(makeHistoryItem()).isQueue).toBe(false)
  })
})

describe('priorityLabel', () => {
  test('high priority', () => {
    expect(priorityLabel(1, t)).toBe('queue.priorityHigh')
  })

  test('normal priority', () => {
    expect(priorityLabel(0, t)).toBe('queue.priorityNormal')
  })

  test('low priority', () => {
    expect(priorityLabel(-1, t)).toBe('queue.priorityLow')
  })
})
