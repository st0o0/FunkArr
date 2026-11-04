import { test, expect } from '@playwright/test'
import { setupApi } from '../helpers/msw-setup'
import { defaultHandlers, activeItem, queuedItem } from '../helpers/handlers'
import { mockQueueStream } from '../helpers/sse-mock'
import { makeQueueItem, makeQueueResponse } from '../fixtures/queue'
import { makeHistoryItem, makeFailedHistoryItem, makeHistoryResponse, makeHistoryStats } from '../fixtures/history'

const queueResponse = makeQueueResponse({
  items: [activeItem, queuedItem],
  activeCount: 1,
  queuedCount: 1,
})

test.beforeEach(async ({ page }) => {
  await setupApi(page, defaultHandlers())
  await mockQueueStream(page, queueResponse)
})

test.describe('Table rendering', () => {
  test('renders active download with progress', async ({ page }) => {
    await page.goto('/activity')
    const row = page.locator('tbody tr').first()
    await expect(row).toContainText('In aller Freundschaft')
    await expect(row).toContainText('Downloading')
  })

  test('renders queued download with status dot', async ({ page }) => {
    await page.goto('/activity')
    const rows = page.locator('tbody tr')
    await expect(rows.nth(1)).toContainText('Queued')
  })

  test('renders completed download from history', async ({ page }) => {
    await page.goto('/activity')
    await expect(page.locator('tbody tr').nth(2)).toContainText('Completed', { timeout: 10000 })
  })

  test('renders failed download with retry button', async ({ page }) => {
    await page.goto('/activity')
    await expect(page.locator('tbody').getByRole('button', { name: /retry/i }).first()).toBeVisible({ timeout: 10000 })
  })

  test('row click navigates to detail', async ({ page }) => {
    await page.goto('/activity')
    await page.locator('tbody tr').first().waitFor({ timeout: 10000 })
    await page.locator('tbody tr').first().locator('td').first().click()
    await expect(page).toHaveURL(/\/activity\//)
  })
})

test.describe('Filter chips', () => {
  test('shows correct counts', async ({ page }) => {
    await page.goto('/activity')
    const chips = page.locator('button.rounded-md.text-xs')
    await expect(chips.filter({ hasText: /^All/ })).toContainText('4')
    await expect(chips.filter({ hasText: /^Active/ })).toContainText('1')
    await expect(chips.filter({ hasText: /^Queued/ })).toContainText('1')
    await expect(chips.filter({ hasText: /^Completed/ })).toContainText('1')
    await expect(chips.filter({ hasText: /^Failed/ })).toContainText('1')
  })

  test('active filter shows only active items', async ({ page }) => {
    await page.goto('/activity')
    await page.locator('button', { hasText: /^Active/ }).click()
    const rows = page.locator('tbody tr')
    await expect(rows).toHaveCount(1)
    await expect(rows.first()).toContainText('In aller Freundschaft')
  })

  test('queued filter shows only queued items', async ({ page }) => {
    await page.goto('/activity')
    await page.locator('button', { hasText: /^Queued/ }).click()
    const rows = page.locator('tbody tr')
    await expect(rows).toHaveCount(1)
    await expect(rows.first()).toContainText('Tatort')
  })

  test('completed filter shows only completed items', async ({ page }) => {
    await page.goto('/activity')
    await page.locator('button', { hasText: /^Completed/ }).click()
    await expect(page.locator('tbody tr').first()).toContainText('Completed', { timeout: 10000 })
  })

  test('failed filter shows only failed items', async ({ page }) => {
    await page.goto('/activity')
    await page.locator('button', { hasText: /^Failed/ }).click()
    await expect(page.locator('tbody tr').first()).toContainText('Failed', { timeout: 10000 })
  })
})

test.describe('Search', () => {
  test('filters queue items by title', async ({ page }) => {
    await page.goto('/activity')
    await page.getByPlaceholder('Search...').fill('freundschaft')
    const rows = page.locator('tbody tr')
    await expect(rows.first()).toContainText('In aller Freundschaft')
    await expect(page.locator('tbody tr', { hasText: 'Tatort' }).filter({ hasText: 'Queued' })).toBeHidden()
  })

  test('clear search restores queue items', async ({ page }) => {
    await page.goto('/activity')
    await page.getByPlaceholder('Search...').fill('freundschaft')
    await page.getByPlaceholder('Search...').clear()
    await expect(page.locator('tbody tr').first()).toContainText('In aller Freundschaft')
  })
})

test.describe('Dropdowns', () => {
  test.fixme('priority filter filters queue items', async ({ page }) => {
    // Requires SSE mock to be reconfigured mid-test with different data
  })

  test('category dropdown hidden on active filter', async ({ page }) => {
    await page.goto('/activity')
    await page.locator('button', { hasText: /^Active/ }).click()
    await expect(page.getByText('All categories')).toBeHidden()
  })

  test('category dropdown visible on completed filter', async ({ page }) => {
    await page.goto('/activity')
    await page.locator('button', { hasText: /^Completed/ }).click()
    await expect(page.locator('select').nth(2)).toBeVisible({ timeout: 10000 })
  })
})

test.describe('Pagination', () => {
  test('hidden on active filter', async ({ page }) => {
    await page.goto('/activity')
    await page.locator('button', { hasText: /^Active/ }).click()
    await expect(page.locator('text=/of \\d+/')).toBeHidden()
  })

  test('shown when history exceeds page size', async ({ page }) => {
    const items = Array.from({ length: 25 }, (_, i) => makeHistoryItem({ downloadId: `h-${i}` }))
    await page.route('**/api/downloads/history?*', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items, totalItems: 50 }) })
    )
    await page.goto('/activity')
    await expect(page.getByText('1-25 of 50')).toBeVisible()
  })
})

test.describe('Inline actions', () => {
  test('delete queue item shows toast', async ({ page }) => {
    await page.route(/\/api\/downloads\/queue\//, (route) => {
      if (route.request().method() === 'DELETE') {
        return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true }) })
      }
      return route.fallback()
    })
    await page.goto('/activity')
    const deleteBtn = page.locator('tbody tr').nth(1).locator('button[title="Delete"]')
    await deleteBtn.waitFor({ timeout: 10000 })
    await deleteBtn.click()
    await expect(page.getByText(/cancelled/i)).toBeVisible({ timeout: 5000 })
  })

  test('retry failed item shows toast', async ({ page }) => {
    await page.goto('/activity')
    const retryBtn = page.locator('tbody').getByRole('button', { name: /retry/i }).first()
    await retryBtn.click({ timeout: 10000 })
    await expect(page.getByText(/retry started/i)).toBeVisible()
  })
})

test.describe('Pipeline controls', () => {
  test('pause button triggers API call and toast', async ({ page }) => {
    await page.goto('/activity')
    await page.getByRole('button', { name: 'Pause' }).click()
    await expect(page.getByText(/paused/i)).toBeVisible()
  })

  test('paused badge visible when pipeline paused', async ({ page }) => {
    await mockQueueStream(page, makeQueueResponse({ isPaused: true }))
    await page.goto('/activity')
    await expect(page.getByText('Paused')).toBeVisible()
    await expect(page.getByRole('button', { name: 'Resume' })).toBeVisible()
  })
})

test.describe('Empty states', () => {
  test('no downloads shows empty state', async ({ page }) => {
    await mockQueueStream(page, makeQueueResponse())
    await page.route('**/api/downloads/history?*', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [], totalItems: 0 }) })
    )
    await page.route('**/api/downloads/history/stats', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ totalCompleted: 0, totalFailed: 0, totalBytes: 0, averageDownloadTimeSeconds: 0, successRate: 0 }) })
    )
    await page.goto('/activity')
    await expect(page.getByText('No downloads')).toBeVisible()
  })
})
