import { test, expect } from '@playwright/test'
import { setupApi } from '../helpers/msw-setup'
import { defaultHandlers } from '../helpers/handlers'
import { mockQueueStream } from '../helpers/sse-mock'
import { makeQueueItem, makeQueueResponse } from '../fixtures/queue'
import { makeHistoryItem, makeFailedHistoryItem } from '../fixtures/history'

const activeDownload = makeQueueItem({
  downloadId: 'dl-1',
  title: 'In.aller.Freundschaft.S10E30.GERMAN.1080p.WEB.h264-FunkArr',
  status: 0,
  phase: 1,
  percentage: 67,
  speed: 12_400_000,
  eta: '2:14',
  channel: 'MDR',
  hasSubtitles: true,
  bytesDownloaded: 425_000_000,
  totalBytes: 637_000_000,
})

test.beforeEach(async ({ page }) => {
  await setupApi(page, defaultHandlers())
})

test.describe('Active state', () => {
  test.beforeEach(async ({ page }) => {
    await mockQueueStream(page, makeQueueResponse({ items: [activeDownload], activeCount: 1 }))
    await page.route('**/api/downloads/history/dl-1', (route) =>
      route.fulfill({ status: 404, body: '' })
    )
  })

  test.fixme('shows progress bar and speed', async ({ page }) => {
    // SSE EventSource mock timing: data may not arrive before assertion
    await page.goto('/activity/dl-1')
    await expect(page.getByText(/67%/)).toBeVisible({ timeout: 10000 })
  })

  test('shows phase stepper labels', async ({ page }) => {
    await page.goto('/activity/dl-1')
    await expect(page.getByText('Download', { exact: true })).toBeVisible()
    await expect(page.getByText('Remux', { exact: true })).toBeVisible()
    await expect(page.getByText('Move', { exact: true })).toBeVisible()
  })

  test('shows channel and SUB badges', async ({ page }) => {
    await page.goto('/activity/dl-1')
    await expect(page.getByText('MDR')).toBeVisible()
    await expect(page.getByText('SUB')).toBeVisible()
  })

  test('shows Force Start button', async ({ page }) => {
    await page.goto('/activity/dl-1')
    await expect(page.getByRole('button', { name: /force start/i })).toBeVisible()
  })
})

test.describe('Completed state', () => {
  const completed = makeHistoryItem({
    downloadId: 'dl-completed',
    title: 'Tatort.S2026E22.GERMAN.1080p.WEB.h264-FunkArr',
    filePath: '/shared/tv/Tatort/S2026E22.mkv',
    downloadTimeSeconds: 842,
    totalBytes: 1_200_000_000,
    status: 0,
  })

  test.beforeEach(async ({ page }) => {
    await mockQueueStream(page, makeQueueResponse())
    await page.route('**/api/downloads/history/dl-completed', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(completed) })
    )
  })

  test('shows Completed badge', async ({ page }) => {
    await page.goto('/activity/dl-completed')
    await expect(page.locator('span').filter({ hasText: /^Completed$/ }).first()).toBeVisible()
  })

  test('all phase steps show green checkmarks', async ({ page }) => {
    await page.goto('/activity/dl-completed')
    const checks = page.locator('svg path[d="M3 8.5l3.5 3.5 6.5-7"]')
    await expect(checks).toHaveCount(3)
  })

  test('shows file path and duration', async ({ page }) => {
    await page.goto('/activity/dl-completed')
    await expect(page.getByText('/shared/tv/Tatort/S2026E22.mkv')).toBeVisible()
    await expect(page.getByText(/14m/)).toBeVisible()
  })

  test('no Force Start button', async ({ page }) => {
    await page.goto('/activity/dl-completed')
    await expect(page.getByRole('button', { name: /force start/i })).toBeHidden()
  })
})

test.describe('Failed state', () => {
  const failed = makeFailedHistoryItem({
    downloadId: 'dl-failed',
    title: 'Broken.Download.S01E01.GERMAN.720p.WEB.h264-FunkArr',
    failMessage: 'Connection timeout',
  })

  test.beforeEach(async ({ page }) => {
    await mockQueueStream(page, makeQueueResponse())
    await page.route('**/api/downloads/history/dl-failed', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(failed) })
    )
  })

  test('shows Failed badge and error message', async ({ page }) => {
    await page.goto('/activity/dl-failed')
    await expect(page.locator('span').filter({ hasText: /^Failed$/ }).first()).toBeVisible()
    await expect(page.getByText('Connection timeout')).toBeVisible()
  })

  test('shows Retry button', async ({ page }) => {
    await page.goto('/activity/dl-failed')
    await expect(page.getByRole('button', { name: /retry/i })).toBeVisible()
  })
})

test.describe('Live transition with 404 retry', () => {
  test.fixme('retries history fetch after 404', async ({ page }) => {
    // Depends on SSE mock timing for the transition watcher to fire
    const completed = makeHistoryItem({ downloadId: 'dl-retry', status: 0 })
    await mockQueueStream(page, makeQueueResponse())
    let callCount = 0
    await page.route(/\/api\/downloads\/history\/dl-retry/, (route) => {
      callCount++
      if (callCount === 1) {
        return route.fulfill({ status: 404, body: '' })
      }
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(completed) })
    })
    await page.goto('/activity/dl-retry')
    await expect(page.locator('span').filter({ hasText: /^Completed$/ }).first()).toBeVisible({ timeout: 15000 })
    expect(callCount).toBeGreaterThanOrEqual(2)
  })
})

test.describe('Direct navigation', () => {
  test('direct to completed download loads correctly', async ({ page }) => {
    const completed = makeHistoryItem({ downloadId: 'direct-1', status: 0 })
    await mockQueueStream(page, makeQueueResponse())
    await page.route('**/api/downloads/history/direct-1', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(completed) })
    )
    await page.goto('/activity/direct-1')
    await expect(page.locator('span').filter({ hasText: /^Completed$/ }).first()).toBeVisible()
  })

  test('direct to non-existent shows not found', async ({ page }) => {
    await mockQueueStream(page, makeQueueResponse())
    await page.route('**/api/downloads/history/does-not-exist', (route) =>
      route.fulfill({ status: 404, body: '' })
    )
    await page.goto('/activity/does-not-exist')
    await expect(page.getByText('Download not found')).toBeVisible()
  })
})

test.describe('Action error handling', () => {
  test('delete failure shows error toast', async ({ page }) => {
    await mockQueueStream(page, makeQueueResponse())
    const completed = makeHistoryItem({ downloadId: 'err-1', status: 0 })
    await page.route('**/api/downloads/history/err-1', (route) => {
      if (route.request().method() === 'DELETE') {
        return route.fulfill({ status: 500, body: 'error' })
      }
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(completed) })
    })
    await page.goto('/activity/err-1')
    await page.getByRole('button', { name: /delete/i }).click()
    await expect(page.getByText(/failed to delete/i)).toBeVisible()
  })

  test.fixme('force start failure shows error toast', async ({ page }) => {
    // Depends on SSE mock delivering active download to detail page
    await mockQueueStream(page, makeQueueResponse({ items: [activeDownload], activeCount: 1 }))
    await page.route('**/api/downloads/history/dl-1', (route) =>
      route.fulfill({ status: 404, body: '' })
    )
    await page.route('**/api/downloads/queue/dl-1/force-start', (route) => {
      if (route.request().method() === 'POST') {
        return route.fulfill({ status: 500, body: '{}' })
      }
      return route.fallback()
    })
    await page.goto('/activity/dl-1')
    await page.getByRole('button', { name: /force start/i }).click()
    await expect(page.getByText(/failed/i).first()).toBeVisible({ timeout: 10000 })
  })
})
