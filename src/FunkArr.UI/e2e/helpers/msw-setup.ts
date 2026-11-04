import { type Page, type Route } from '@playwright/test'

export type ApiHandler = {
  method: string
  pattern: string | RegExp
  handler: (route: Route) => Promise<void> | void
}

export function apiHandler(
  method: string,
  pattern: string,
  body: unknown,
  status = 200,
): ApiHandler {
  const escaped = pattern.replace(/[.*+?^${}()|[\]\\]/g, '\\$&').replace(/\\\*/g, '[^/]*')
  return {
    method: method.toUpperCase(),
    pattern: new RegExp(`/api/${escaped}(\\?[^/]*)?$`),
    handler: (route) => {
      return route.fulfill({
        status,
        contentType: 'application/json',
        body: JSON.stringify(body),
      })
    },
  }
}

export async function setupApi(page: Page, handlers: ApiHandler[]) {
  for (const h of handlers) {
    await page.route(h.pattern, (route) => {
      if (route.request().method() === h.method) {
        return h.handler(route)
      }
      return route.fallback()
    })
  }
}

export async function overrideApi(page: Page, handler: ApiHandler) {
  await page.route(handler.pattern, (route) => {
    if (route.request().method() === handler.method) {
      return handler.handler(route)
    }
    return route.fallback()
  })
}
