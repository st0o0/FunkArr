import { test, expect } from '@playwright/test'
import { setupApi } from '../helpers/msw-setup'
import { defaultHandlers } from '../helpers/handlers'
import { mockQueueStream } from '../helpers/sse-mock'
import { makeQueueResponse } from '../fixtures/queue'

test.beforeEach(async ({ page }) => {
  await setupApi(page, defaultHandlers())
  await mockQueueStream(page, makeQueueResponse())
})

test.describe('Route loading', () => {
  const routes = [
    { path: '/', title: 'Overview' },
    { path: '/activity', title: 'Activity' },
    { path: '/setup', title: 'Setup' },
    { path: '/rulesets', title: 'RuleSets' },
    { path: '/feed', title: 'Feed' },
    { path: '/settings', title: 'Settings' },
  ]

  for (const route of routes) {
    test(`${route.path} loads without errors`, async ({ page }) => {
      const errors: string[] = []
      page.on('pageerror', (err) => errors.push(err.message))

      await page.goto(route.path)
      await page.waitForLoadState('networkidle')

      expect(errors).toHaveLength(0)
    })
  }

  test('/rulesets/:id loads without errors', async ({ page }) => {
    const errors: string[] = []
    page.on('pageerror', (err) => errors.push(err.message))
    await page.goto('/rulesets/rs-detail-1')
    await page.waitForLoadState('networkidle')
    expect(errors).toHaveLength(0)
  })

  test('/rulesets/new loads without errors', async ({ page }) => {
    const errors: string[] = []
    page.on('pageerror', (err) => errors.push(err.message))
    await page.goto('/rulesets/new')
    await page.waitForLoadState('networkidle')
    expect(errors).toHaveLength(0)
  })
})

test.describe('Redirects', () => {
  test('/queue redirects to /activity', async ({ page }) => {
    await page.goto('/queue')
    await expect(page).toHaveURL('/activity')
  })

  test('/history redirects to /activity', async ({ page }) => {
    await page.goto('/history')
    await expect(page).toHaveURL('/activity')
  })

  test('/search redirects to /rulesets', async ({ page }) => {
    await page.goto('/search')
    await expect(page).toHaveURL('/rulesets')
  })

  test('/nonexistent redirects to /', async ({ page }) => {
    await page.goto('/this-does-not-exist')
    await expect(page).toHaveURL('/')
  })
})
