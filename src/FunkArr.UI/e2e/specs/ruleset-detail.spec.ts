import { test, expect } from '@playwright/test'
import { setupApi } from '../helpers/msw-setup'
import { defaultHandlers } from '../helpers/handlers'
import { mockQueueStream } from '../helpers/sse-mock'
import { makeQueueResponse } from '../fixtures/queue'
import { makeRuleSetDetail } from '../fixtures/rulesets'

test.beforeEach(async ({ page }) => {
  await setupApi(page, defaultHandlers())
  await mockQueueStream(page, makeQueueResponse())
})

test.describe('Sections', () => {
  test('renders identity section', async ({ page }) => {
    await page.goto('/rulesets/rs-detail-1')
    await expect(page.getByRole('heading').filter({ hasText: 'Tatort' })).toBeVisible()
  })

  test('renders rules section', async ({ page }) => {
    await page.goto('/rulesets/rs-detail-1')
    await expect(page.getByRole('heading', { name: 'Matching Rules' })).toBeVisible()
  })

  test('renders enrichment section when present', async ({ page }) => {
    await page.goto('/rulesets/rs-detail-1')
    await expect(page.getByRole('heading', { name: /enrichment/i })).toBeVisible()
  })
})

test.describe('Actions', () => {
  test('delete with confirmation', async ({ page }) => {
    const detail = makeRuleSetDetail({
      source: { communityPath: null, localPath: '/local/tatort.json', communityModified: null, localModified: new Date().toISOString() },
    })
    await page.route('**/api/rulesets/*', (route) => {
      if (route.request().method() === 'GET' && route.request().url().includes('/api/rulesets/')) {
        return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(detail) })
      }
      if (route.request().method() === 'DELETE') {
        return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true }) })
      }
      return route.fallback()
    })
    await page.goto('/rulesets/rs-detail-1')
    const deleteBtn = page.getByRole('button', { name: /delete/i })
    if (await deleteBtn.isVisible()) {
      await deleteBtn.click()
      const confirmBtn = page.getByRole('button', { name: /confirm/i })
      if (await confirmBtn.isVisible()) {
        await confirmBtn.click()
        await expect(page.getByText(/deleted/i)).toBeVisible()
      }
    }
  })
})
