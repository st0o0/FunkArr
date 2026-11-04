import { test, expect } from '@playwright/test'
import { setupApi } from '../helpers/msw-setup'
import { defaultHandlers } from '../helpers/handlers'
import { mockQueueStream, mockLogStream } from '../helpers/sse-mock'
import { makeQueueResponse } from '../fixtures/queue'

test.beforeEach(async ({ page }) => {
  await setupApi(page, defaultHandlers())
  await mockQueueStream(page, makeQueueResponse())
  await mockLogStream(page, [
    JSON.stringify({ timestamp: new Date().toISOString(), level: 'Information', message: 'App started', sourceContext: 'FunkArr', exception: null }),
    JSON.stringify({ timestamp: new Date().toISOString(), level: 'Error', message: 'Connection refused', sourceContext: 'FunkArr.Download', exception: 'HttpRequestException' }),
  ])
})

test.describe('Sections', () => {
  test('renders download settings', async ({ page }) => {
    await page.goto('/settings')
    await expect(page.getByRole('heading', { name: 'Downloads' })).toBeVisible()
  })

  test('renders network routes', async ({ page }) => {
    await page.goto('/settings')
    await expect(page.getByRole('heading', { name: 'Network Routes' })).toBeVisible()
  })

  test('renders cache stats', async ({ page }) => {
    await page.goto('/settings')
    await expect(page.getByRole('heading', { name: 'Metadata Cache' })).toBeVisible()
  })

  test('renders broadcast variants', async ({ page }) => {
    await page.goto('/settings')
    await expect(page.getByRole('heading', { name: 'Broadcast variants' })).toBeVisible()
  })

  test('shows always active when no schedule', async ({ page }) => {
    await page.goto('/settings')
    await expect(page.getByText('Always active')).toBeVisible()
  })
})

test.describe('Logs', () => {
  test('log entries visible', async ({ page }) => {
    await page.goto('/settings')
    await expect(page.getByText('App started')).toBeVisible()
  })

  test('log section heading visible', async ({ page }) => {
    await page.goto('/settings')
    await expect(page.getByRole('heading', { name: 'Logs' })).toBeVisible()
  })
})

test.describe('Fallbacks', () => {
  test('individual API failure does not crash page', async ({ page }) => {
    await page.route('**/api/system/cache', (route) => route.fulfill({ status: 500, body: 'error' }))
    const errors: string[] = []
    page.on('pageerror', (e) => errors.push(e.message))
    await page.goto('/settings')
    await page.waitForLoadState('networkidle')
    expect(errors).toHaveLength(0)
    await expect(page.getByRole('heading', { name: 'Downloads' })).toBeVisible()
  })
})
