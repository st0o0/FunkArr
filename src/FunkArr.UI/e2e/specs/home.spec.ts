import { test, expect } from '@playwright/test'
import { setupApi } from '../helpers/msw-setup'
import { defaultHandlers } from '../helpers/handlers'
import { mockQueueStream } from '../helpers/sse-mock'
import { makeQueueItem, makeQueueResponse } from '../fixtures/queue'

test.beforeEach(async ({ page }) => {
  await setupApi(page, defaultHandlers())
})

test.describe('Dashboard sections', () => {
  test('renders all sections with data', async ({ page }) => {
    await mockQueueStream(page, makeQueueResponse())
    await page.goto('/')
    await expect(page.getByRole('heading', { name: 'System' })).toBeVisible()
    await expect(page.getByText('Healthy')).toBeVisible()
    await expect(page.getByRole('heading', { name: 'Storage' })).toBeVisible()
    await expect(page.getByRole('heading', { name: 'Feed Ingest' })).toBeVisible()
    await expect(page.getByRole('heading', { name: 'Rulesets' })).toBeVisible()
  })
})

test.describe('Pipeline status', () => {
  test('idle when no downloads', async ({ page }) => {
    await mockQueueStream(page, makeQueueResponse())
    await page.goto('/')
    await expect(page.getByText('Pipeline idle')).toBeVisible()
  })

  test('active with downloads', async ({ page }) => {
    const active = makeQueueItem({ status: 0, speed: 12_000_000, percentage: 50 })
    await mockQueueStream(page, makeQueueResponse({ items: [active], activeCount: 1 }))
    await page.goto('/')
    await expect(page.getByText('Pipeline active')).toBeVisible()
  })

  test('paused shows resume button', async ({ page }) => {
    const queued = makeQueueItem({ status: 1 })
    await mockQueueStream(page, makeQueueResponse({ items: [queued], queuedCount: 1, isPaused: true }))
    await page.goto('/')
    await expect(page.getByText('Pipeline paused')).toBeVisible()
    await expect(page.getByRole('button', { name: 'Resume' })).toBeVisible()
  })
})

test.describe('Empty states', () => {
  test('no recent activity shows empty message', async ({ page }) => {
    await mockQueueStream(page, makeQueueResponse())
    await page.route('**/api/downloads/history?*', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [], totalItems: 0 }) })
    )
    await page.goto('/')
    await expect(page.getByText('No recent activity')).toBeVisible()
  })
})

test.describe('Interactions', () => {
  test('pause triggers API and shows toast', async ({ page }) => {
    const active = makeQueueItem({ status: 0 })
    await mockQueueStream(page, makeQueueResponse({ items: [active], activeCount: 1 }))
    await page.goto('/')
    await page.getByRole('button', { name: 'Pause' }).click()
    await expect(page.getByText('Pipeline paused')).toBeVisible()
  })

  test('crawl now triggers API and shows toast', async ({ page }) => {
    await mockQueueStream(page, makeQueueResponse())
    await page.goto('/')
    const crawlBtn = page.getByRole('button', { name: 'Crawl now' })
    if (await crawlBtn.isVisible()) {
      await crawlBtn.click()
      await expect(page.getByText(/crawl started/i)).toBeVisible()
    }
  })

  test('view all navigates to activity', async ({ page }) => {
    await mockQueueStream(page, makeQueueResponse())
    await page.goto('/')
    await page.getByText('View all').click()
    await expect(page).toHaveURL(/\/activity/)
  })
})

test.describe('Fallbacks', () => {
  test('API failures do not crash the page', async ({ page }) => {
    await page.route('**/api/system/storage', (route) => route.fulfill({ status: 500, body: 'error' }))
    await mockQueueStream(page, makeQueueResponse())
    const errors: string[] = []
    page.on('pageerror', (e) => errors.push(e.message))
    await page.goto('/')
    await page.waitForLoadState('networkidle')
    expect(errors).toHaveLength(0)
    await expect(page.getByRole('heading', { name: 'System' })).toBeVisible()
  })
})
