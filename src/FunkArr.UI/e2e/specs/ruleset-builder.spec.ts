import { test, expect } from '@playwright/test'
import { setupApi } from '../helpers/msw-setup'
import { defaultHandlers } from '../helpers/handlers'
import { mockQueueStream } from '../helpers/sse-mock'
import { makeQueueResponse } from '../fixtures/queue'

test.beforeEach(async ({ page }) => {
  await setupApi(page, defaultHandlers())
  await mockQueueStream(page, makeQueueResponse())
})

test.describe('Create form', () => {
  test('new ruleset page loads', async ({ page }) => {
    await page.goto('/rulesets/new')
    await expect(page.locator('label', { hasText: /topic/i })).toBeVisible()
  })
})

test.describe('Edit form', () => {
  test.fixme('pre-fills with existing data', async ({ page }) => {
    await page.route(/\/api\/rulesets\//, (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            ruleSetId: 'rs-detail-1',
            identity: { topic: 'Tatort', aliases: [], tvdbId: null, imdbId: null, tmdbId: null },
            source: { communityPath: null, localPath: null, communityModified: null, localModified: null },
            defaultConfidence: 80, rules: [], enrichment: { enabled: false, methods: [], title: { threshold: 0 }, airdate: { tolerance: 0 }, runtime: { tolerance: 0, mode: 0 }, year: { tolerance: 0 } },
          }),
        })
      }
      return route.fallback()
    })
    await page.goto('/rulesets/rs-detail-1/edit')
    await expect(page.locator('input[type="text"]').first()).toHaveValue('Tatort', { timeout: 10000 })
  })
})

test.describe('Live preview', () => {
  test('preview panel is visible', async ({ page }) => {
    await page.goto('/rulesets/new')
    await expect(page.getByText(/preview/i).first()).toBeVisible()
  })
})
