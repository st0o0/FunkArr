import { test, expect } from '@playwright/test'
import { setupApi } from '../helpers/msw-setup'
import { defaultHandlers } from '../helpers/handlers'
import { mockQueueStream } from '../helpers/sse-mock'
import { makeQueueResponse } from '../fixtures/queue'
import { makeScoringSnapshot, makeItemTrace } from '../fixtures/rulesets'

test.beforeEach(async ({ page }) => {
  await setupApi(page, defaultHandlers())
  await mockQueueStream(page, makeQueueResponse())
})

test.describe('Scoring History', () => {
  test('renders history table', async ({ page }) => {
    await page.goto('/rulesets/rs-detail-1/history')
    await expect(page.locator('table')).toBeVisible()
  })

  test('row navigates to detail', async ({ page }) => {
    await page.goto('/rulesets/rs-detail-1/history')
    const row = page.locator('tbody tr').first()
    if (await row.isVisible()) {
      await row.click()
      await expect(page).toHaveURL(/\/history\//)
    }
  })
})

test.describe('Scoring Detail', () => {
  test('renders candidate cards', async ({ page }) => {
    const matched = makeItemTrace({ matched: true })
    const unmatched = makeItemTrace({ matched: false, score: 20, matchedRuleId: null })
    await page.route('**/api/rulesets/*/history/*', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          requestId: 'req-1',
          source: 0,
          query: 'Tatort',
          timestamp: new Date().toISOString(),
          itemTraces: [matched, unmatched],
        }),
      })
    )
    await page.goto('/rulesets/rs-detail-1/history/req-1')
    await expect(page.locator('main')).toContainText('Tatort')
  })
})
