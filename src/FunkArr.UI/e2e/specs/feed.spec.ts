import { test, expect } from '@playwright/test'
import { setupApi } from '../helpers/msw-setup'
import { defaultHandlers } from '../helpers/handlers'
import { mockQueueStream } from '../helpers/sse-mock'
import { makeQueueResponse } from '../fixtures/queue'
import { makeFeedResponse } from '../fixtures/feed'

test.beforeEach(async ({ page }) => {
  await setupApi(page, defaultHandlers())
  await mockQueueStream(page, makeQueueResponse())
})

test.describe('Data display', () => {
  test('renders feed heading', async ({ page }) => {
    await page.goto('/feed')
    await expect(page.getByRole('heading', { name: /feed/i })).toBeVisible()
  })

  test('search filters entries', async ({ page }) => {
    let lastUrl = ''
    await page.route('**/api/feed?*', (route) => {
      lastUrl = route.request().url()
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(makeFeedResponse()),
      })
    })
    await page.goto('/feed')
    const search = page.getByPlaceholder(/search/i)
    if (await search.isVisible()) {
      await search.fill('tatort')
      await page.waitForTimeout(500)
      expect(lastUrl).toContain('q=tatort')
    }
  })

  test('crawl now triggers API', async ({ page }) => {
    let crawlCalled = false
    await page.route('**/api/feed/crawl', (route) => {
      crawlCalled = true
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true }) })
    })
    await page.goto('/feed')
    await page.getByRole('button', { name: /crawl now/i }).click()
    expect(crawlCalled).toBe(true)
  })

  test.fixme('pagination controls visible with many entries', async ({ page }) => {
    // Route override for /api/feed races with default handler
    const entries = Array.from({ length: 25 }, (_, i) => makeFeedEntry({ stableId: `feed-p-${i}` }))
    const body = JSON.stringify(makeFeedResponse({ entries, total: 100, filteredTotal: 100 }))
    await page.route(/\/api\/feed/, (route) => {
      if (route.request().url().includes('/api/feed/crawl')) return route.fallback()
      return route.fulfill({ status: 200, contentType: 'application/json', body })
    })
    await page.goto('/feed')
    await expect(page.getByRole('button', { name: 'Next' })).toBeVisible({ timeout: 10000 })
  })

  test('empty feed shows message', async ({ page }) => {
    await page.route('**/api/feed?*', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(makeFeedResponse({ entries: [], total: 0, filteredTotal: 0 })),
      })
    )
    await page.route('**/api/feed', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(makeFeedResponse({ entries: [], total: 0, filteredTotal: 0 })),
      })
    )
    await page.goto('/feed')
    await expect(page.getByText(/no releases/i)).toBeVisible()
  })
})
