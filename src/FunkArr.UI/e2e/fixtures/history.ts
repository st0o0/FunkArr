import type { HistoryItem, HistoryResponse, HistoryStatsResponse } from '../../src/api/downloads'

let counter = 0

export function makeHistoryItem(overrides?: Partial<HistoryItem>): HistoryItem {
  const id = `hist-${++counter}-${Date.now()}`
  return {
    downloadId: id,
    title: 'Tatort.S2026E22.Tatort.Angezählt.GERMAN.1080p.WEB.h264-FunkArr',
    category: 'tv',
    totalBytes: 1_200_000_000,
    downloadTimeSeconds: 842,
    filePath: '/shared/tv/Tatort/Season 2026/Tatort.S2026E22.mkv',
    status: 0,
    failMessage: null,
    completedAt: new Date().toISOString(),
    ...overrides,
  }
}

export function makeFailedHistoryItem(overrides?: Partial<HistoryItem>): HistoryItem {
  return makeHistoryItem({
    title: 'Failed.Show.S01E01.GERMAN.720p.WEB.h264-FunkArr',
    status: 1,
    filePath: null,
    failMessage: 'Error opening input files: I/O error',
    ...overrides,
  })
}

export function makeHistoryResponse(overrides?: Partial<HistoryResponse>): HistoryResponse {
  return {
    items: [],
    totalItems: 0,
    ...overrides,
  }
}

export function makeHistoryStats(overrides?: Partial<HistoryStatsResponse>): HistoryStatsResponse {
  return {
    totalCompleted: 10,
    totalFailed: 2,
    totalBytes: 15_000_000_000,
    averageDownloadTimeSeconds: 600,
    successRate: 0.833,
    ...overrides,
  }
}
