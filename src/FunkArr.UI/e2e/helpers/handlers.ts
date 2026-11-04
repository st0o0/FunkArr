import { type ApiHandler, apiHandler } from './msw-setup'
import { makeQueueItem, makeQueueResponse } from '../fixtures/queue'
import { makeHistoryItem, makeFailedHistoryItem, makeHistoryResponse, makeHistoryStats } from '../fixtures/history'
import { makeRuleSetListResponse, makeRuleSetDetail, makeScoringSnapshot } from '../fixtures/rulesets'
import { makeFeedResponse } from '../fixtures/feed'
import { makeHealthChecks, makeStorageStatus, makeVersion, makeCacheStats, makeRoutes, makeVariants, makeLogEntries } from '../fixtures/system'

const activeItem = makeQueueItem({ downloadId: 'active-1', title: 'In.aller.Freundschaft.S10E30.GERMAN.1080p.WEB.h264-FunkArr', status: 0, phase: 0, percentage: 67 })
const queuedItem = makeQueueItem({ downloadId: 'queued-1', title: 'Tatort.S2026E22.Tatort.Angezählt.GERMAN.720p.WEB.h264-FunkArr', status: 1, phase: -1, percentage: 0, speed: 0, eta: '', channel: 'ORF' })
const completedItem = makeHistoryItem({ downloadId: 'completed-1' })
const failedItem = makeFailedHistoryItem({ downloadId: 'failed-1' })

export function defaultHandlers(): ApiHandler[] {
  return [
    apiHandler('GET', 'downloads/queue', makeQueueResponse({
      items: [activeItem, queuedItem],
      activeCount: 1,
      queuedCount: 1,
    })),

    apiHandler('GET', 'downloads/history', makeHistoryResponse({
      items: [completedItem, failedItem],
      totalItems: 2,
    })),

    apiHandler('GET', 'downloads/history/stats', makeHistoryStats({ totalCompleted: 1, totalFailed: 1 })),
    apiHandler('GET', 'downloads/history/categories', ['tv']),
    apiHandler('GET', 'downloads/settings', { concurrentDownloads: 2, schedule: [] }),

    apiHandler('DELETE', 'downloads/queue/*', { success: true }),
    apiHandler('DELETE', 'downloads/history/*', { success: true }),
    apiHandler('POST', 'downloads/*/retry', { success: true }),
    apiHandler('POST', 'downloads/queue/*/force-start', { success: true }),
    apiHandler('POST', 'downloads/pause', { success: true }),
    apiHandler('POST', 'downloads/resume', { success: true }),

    apiHandler('GET', 'rulesets', makeRuleSetListResponse()),
    apiHandler('GET', 'rulesets/*', makeRuleSetDetail()),
    apiHandler('GET', 'rulesets/*/history*', { ruleSetId: 'rs-1', totalCount: 1, snapshots: [makeScoringSnapshot()] }),
    apiHandler('GET', 'rulesets/*/export', {}),
    apiHandler('DELETE', 'rulesets/*', { success: true }),
    apiHandler('POST', 'rulesets', { ruleSetId: 'new-rs' }),
    apiHandler('PUT', 'rulesets/*', { success: true }),
    apiHandler('POST', 'rulesets/test', { itemTraces: [] }),

    apiHandler('GET', 'mediathek/search*', []),

    apiHandler('GET', 'feed?*', makeFeedResponse()),
    apiHandler('GET', 'feed', makeFeedResponse()),
    apiHandler('POST', 'feed/crawl', { success: true }),

    apiHandler('GET', 'system/setup', makeHealthChecks()),
    apiHandler('GET', 'system/storage', makeStorageStatus()),
    apiHandler('GET', 'system/version', makeVersion()),
    apiHandler('GET', 'system/cache', makeCacheStats()),
    apiHandler('GET', 'system/routes', makeRoutes()),
    apiHandler('GET', 'system/variants', makeVariants()),
    apiHandler('GET', 'system/logs', makeLogEntries()),

    {
      method: 'GET',
      pattern: /\/api\/downloads\/queue\/stream/,
      handler: (route) => route.fulfill({
        status: 200,
        contentType: 'text/event-stream',
        headers: { 'Cache-Control': 'no-cache' },
        body: '',
      }),
    },

    {
      method: 'GET',
      pattern: /\/api\/system\/logs\/stream/,
      handler: (route) => route.fulfill({
        status: 200,
        contentType: 'text/event-stream',
        headers: { 'Cache-Control': 'no-cache' },
        body: '',
      }),
    },

    apiHandler('POST', 'setup/prowlarr/*', { success: true }),
    apiHandler('POST', 'setup/sonarr/*', { success: true }),
    apiHandler('POST', 'setup/radarr/*', { success: true }),
  ]
}

export { activeItem, queuedItem, completedItem, failedItem }
