import { test, expect } from '@playwright/test'
import { setupApi } from '../helpers/msw-setup'
import { defaultHandlers } from '../helpers/handlers'
import { mockQueueStream } from '../helpers/sse-mock'
import { makeQueueResponse } from '../fixtures/queue'
import { makeRuleSetEntry, makeRuleSetListResponse } from '../fixtures/rulesets'

test.beforeEach(async ({ page }) => {
  await setupApi(page, defaultHandlers())
  await mockQueueStream(page, makeQueueResponse())
})

test.describe('List rendering', () => {
  test('renders ruleset cards', async ({ page }) => {
    const entries = [
      makeRuleSetEntry({ ruleSetId: 'rs-1', topic: 'Tatort' }),
      makeRuleSetEntry({ ruleSetId: 'rs-2', topic: 'Polizeiruf 110', aliases: [] }),
    ]
    await page.route('**/api/rulesets', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(makeRuleSetListResponse(entries)) })
    )
    await page.goto('/rulesets')
    await expect(page.getByRole('heading', { name: 'RuleSets' })).toBeVisible()
    await expect(page.locator('a[href*="rulesets/rs-1"]')).toBeVisible()
    await expect(page.locator('a[href*="rulesets/rs-2"]')).toBeVisible()
  })

  test('search filters by topic', async ({ page }) => {
    const entries = [
      makeRuleSetEntry({ ruleSetId: 'rs-1', topic: 'Tatort' }),
      makeRuleSetEntry({ ruleSetId: 'rs-2', topic: 'Polizeiruf 110', aliases: [] }),
    ]
    await page.route('**/api/rulesets', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(makeRuleSetListResponse(entries)) })
    )
    await page.goto('/rulesets')
    await page.getByPlaceholder(/search/i).fill('polizeiruf')
    await expect(page.locator('a[href*="rulesets/rs-2"]')).toBeVisible()
    await expect(page.locator('a[href*="rulesets/rs-1"]')).toBeHidden()
  })

  test('empty list shows create button', async ({ page }) => {
    await page.route('**/api/rulesets', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(makeRuleSetListResponse([])) })
    )
    await page.goto('/rulesets')
    await expect(page.getByText(/new/i).first()).toBeVisible()
  })

  test('click card navigates to detail', async ({ page }) => {
    await page.goto('/rulesets')
    await page.locator('a[href*="rulesets/"]').first().click()
    await expect(page).toHaveURL(/\/rulesets\//)
  })

  test('loading shows skeleton', async ({ page }) => {
    await page.route('**/api/rulesets', (route) => new Promise(() => {}))
    await page.goto('/rulesets')
    await expect(page.getByRole('heading', { name: 'RuleSets' })).toBeVisible()
  })
})
