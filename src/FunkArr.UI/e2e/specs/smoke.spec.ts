import { test, expect } from '@playwright/test'

function setupMocks(test: any) {
  test.beforeEach(async ({ page }: any) => {
    await page.route(/\/api\//, (route: any) => {
      const url = route.request().url()
      if (url.includes('/stream')) {
        return route.fulfill({
          status: 200,
          contentType: 'text/event-stream',
          headers: { 'Cache-Control': 'no-cache' },
          body: 'event: queue\ndata: {"items":[],"totalSlots":2,"activeCount":0,"queuedCount":0,"isPaused":false,"isScheduleActive":true,"nextWindow":null}\n\n',
        })
      }
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: '{"items":[],"totalItems":0,"rulesets":[],"checks":{},"entries":[],"total":0,"filteredTotal":0,"ingest":{"enabled":false,"running":false},"appVersion":"0.0.0-test","communityRulesetVersion":null,"variants":[],"definitions":[],"channelRoutes":[],"defaultRoute":"direct","tvdbEntries":0,"tmdbEntries":0,"completeDirectory":{"path":"/tmp","availableBytes":1000000,"totalBytes":2000000},"incompleteDirectory":{"path":"/tmp","availableBytes":1000000,"totalBytes":2000000},"totalCompleted":0,"totalFailed":0,"totalBytes":0,"averageDownloadTimeSeconds":0,"successRate":0,"concurrentDownloads":1,"schedule":[],"setupConnectionInfo":{"indexerApiPath":"/index/api","downloadApiPath":"/download/api","defaultPort":6969}}',
      })
    })
  })
}

test.describe('Route loading', () => {
  setupMocks(test)

  const routes = [
    { path: '/', name: 'Dashboard' },
    { path: '/activity', name: 'Activity' },
    { path: '/setup', name: 'Setup' },
    { path: '/rulesets', name: 'RuleSets' },
    { path: '/feed', name: 'Feed' },
    { path: '/settings', name: 'Settings' },
  ]

  for (const route of routes) {
    test(`${route.path} (${route.name}) loads without errors`, async ({ page }) => {
      const errors: string[] = []
      page.on('pageerror', (err) => errors.push(err.message))
      await page.goto(route.path)
      await page.waitForLoadState('networkidle')
      expect(errors).toHaveLength(0)
    })
  }

  test('/rulesets/:id loads without errors', async ({ page }) => {
    await page.route(/\/api\/rulesets\//, (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          ruleSetId: 'test', identity: { topic: 'Test', aliases: [], tvdbId: null, imdbId: null, tmdbId: null },
          source: { communityPath: null, localPath: null, communityModified: null, localModified: null },
          defaultConfidence: 80, rules: [], enrichment: { enabled: false, methods: [], title: { threshold: 0 }, airdate: { tolerance: 0 }, runtime: { tolerance: 0, mode: 0 }, year: { tolerance: 0 } },
        }),
      })
    )
    const errors: string[] = []
    page.on('pageerror', (err) => errors.push(err.message))
    await page.goto('/rulesets/test-rs')
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

  test('/activity/:id loads for completed download', async ({ page }) => {
    await page.route(/\/api\/downloads\/history\//, (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          downloadId: 'test-dl', title: 'Test.S01E01.1080p-FunkArr', category: 'tv',
          totalBytes: 1000000, downloadTimeSeconds: 60, filePath: '/test.mkv',
          status: 0, failMessage: null, completedAt: new Date().toISOString(),
        }),
      })
    )
    const errors: string[] = []
    page.on('pageerror', (err) => errors.push(err.message))
    await page.goto('/activity/test-dl')
    await page.waitForLoadState('networkidle')
    expect(errors).toHaveLength(0)
  })
})

test.describe('Redirects', () => {
  test('/queue redirects to /activity', async ({ page }) => {
    await page.goto('/queue', { waitUntil: 'commit' })
    await expect(page).toHaveURL(/\/activity/)
  })

  test('/history redirects to /activity', async ({ page }) => {
    await page.goto('/history', { waitUntil: 'commit' })
    await expect(page).toHaveURL(/\/activity/)
  })

  test('/search redirects to /rulesets', async ({ page }) => {
    await page.goto('/search', { waitUntil: 'commit' })
    await expect(page).toHaveURL(/\/rulesets/)
  })

  test('/nonexistent redirects to /', async ({ page }) => {
    await page.goto('/this-does-not-exist', { waitUntil: 'commit' })
    await expect(page).toHaveURL(/^\/$|localhost:\d+\/$/)
  })
})
