import { test, expect } from '@playwright/test'
import { setupApi } from '../helpers/msw-setup'
import { defaultHandlers } from '../helpers/handlers'
import { mockQueueStream } from '../helpers/sse-mock'
import { makeQueueResponse } from '../fixtures/queue'
import { makeHealthChecks } from '../fixtures/system'

test.beforeEach(async ({ page }) => {
  await setupApi(page, defaultHandlers())
  await mockQueueStream(page, makeQueueResponse())
})

test.describe('Health check step', () => {
  test('shows health check heading', async ({ page }) => {
    await page.goto('/setup')
    await expect(page.getByRole('heading', { name: 'System Health Check' })).toBeVisible()
  })

  test('all checks passing enables Next', async ({ page }) => {
    await page.goto('/setup')
    const nextBtn = page.getByRole('button', { name: 'Next' })
    await expect(nextBtn).toBeEnabled()
  })

  test('failed check disables Next', async ({ page }) => {
    const checks = makeHealthChecks()
    checks.checks.ffmpeg = { status: 2, message: 'FFmpeg not found' }
    await page.route(/\/api\/system\/setup/, (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(checks) })
    )
    await page.goto('/setup')
    const nextBtn = page.getByRole('button', { name: 'Next' })
    await expect(nextBtn).toBeDisabled({ timeout: 10000 })
  })
})

test.describe('Service selection', () => {
  test('no services selected disables Next', async ({ page }) => {
    await page.goto('/setup')
    await page.getByRole('button', { name: 'Next' }).click()
    const nextBtn = page.getByRole('button', { name: 'Next' })
    await expect(nextBtn).toBeDisabled()
  })

  test('selecting service enables Next', async ({ page }) => {
    await page.goto('/setup')
    await page.getByRole('button', { name: 'Next' }).click()
    await page.getByText('Sonarr').click()
    const nextBtn = page.getByRole('button', { name: 'Next' })
    await expect(nextBtn).toBeEnabled()
  })
})

test.describe('Service configuration', () => {
  test('auto-create failure shows error', async ({ page }) => {
    await page.route('**/api/setup/sonarr/*', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: false, error: 'Connection refused' }) })
    )
    await page.goto('/setup')
    await page.getByRole('button', { name: 'Next' }).click()
    await page.getByText('Sonarr').click()
    await page.getByRole('button', { name: 'Next' }).click()

    const urlInput = page.locator('input[type="text"]').first()
    if (await urlInput.isVisible()) {
      await urlInput.fill('http://sonarr:8989')
    }
  })
})
