import { test, expect } from '@playwright/test'
import { setupApi } from '../helpers/msw-setup'
import { defaultHandlers } from '../helpers/handlers'
import { mockQueueStream } from '../helpers/sse-mock'
import { makeQueueResponse } from '../fixtures/queue'

test.beforeEach(async ({ page }) => {
  await setupApi(page, defaultHandlers())
  await mockQueueStream(page, makeQueueResponse())
})

const nav = (page: any) => page.locator('nav')

test.describe('Sidebar navigation', () => {
  test('shows all nav items when expanded', async ({ page }) => {
    await page.goto('/')
    const sidebar = nav(page)
    await expect(sidebar.getByText('Overview')).toBeVisible()
    await expect(sidebar.getByText('Activity')).toBeVisible()
    await expect(sidebar.getByText('RuleSets')).toBeVisible()
    await expect(sidebar.getByText('Feed')).toBeVisible()
    await expect(sidebar.getByText('Settings')).toBeVisible()
  })

  test('navigates between routes', async ({ page }) => {
    await page.goto('/')
    await nav(page).getByText('RuleSets').click()
    await expect(page).toHaveURL('/rulesets')
  })
})

test.describe('Sidebar collapse', () => {
  test('collapse hides labels', async ({ page }) => {
    await page.goto('/')
    await page.getByTitle('Collapse').click()
    await expect(nav(page).getByText('Overview')).toBeHidden()
  })

  test('expand restores labels', async ({ page }) => {
    await page.goto('/')
    await page.getByTitle('Collapse').click()
    await page.getByTitle('Expand').click()
    await expect(nav(page).getByText('Overview')).toBeVisible()
  })

  test('collapse state persists across navigation', async ({ page }) => {
    await page.goto('/')
    await page.getByTitle('Collapse').click()
    await page.locator('a[href="/activity"]').click()
    await expect(page).toHaveURL('/activity')
    await page.waitForTimeout(500)
    await expect(page.locator('nav span.truncate').first()).toBeHidden()
  })

  test('collapsed state restored from localStorage', async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem('funkarr-sidebar', 'collapsed')
    })
    await page.goto('/')
    await expect(nav(page).getByText('Overview')).toBeHidden()
  })
})

test.describe('Locale switching', () => {
  test('switch to German changes UI text', async ({ page }) => {
    await page.goto('/')
    await expect(nav(page).getByText('Overview')).toBeVisible()
    await page.locator('select').last().selectOption('de')
    await expect(nav(page).getByText('Übersicht')).toBeVisible()
  })

  test('locale persists on reload', async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem('funkarr-locale', 'de')
    })
    await page.goto('/')
    await expect(nav(page).getByText('Übersicht')).toBeVisible()
  })
})

test.describe('Footer', () => {
  test('version visible when expanded', async ({ page }) => {
    await page.goto('/')
    await expect(page.locator('aside').getByText('v0.1.0-dev')).toBeVisible()
  })

  test('version hidden when collapsed', async ({ page }) => {
    await page.goto('/')
    await page.getByTitle('Collapse').click()
    await expect(page.locator('aside').getByText('v0.1.0-dev')).toBeHidden()
  })
})
