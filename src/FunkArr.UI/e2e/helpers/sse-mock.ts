import { type Page } from '@playwright/test'
import type { QueueResponse } from '../../src/api/downloads'

export async function mockQueueStream(page: Page, initial: QueueResponse) {
  await page.addInitScript((data) => {
    const OrigEventSource = window.EventSource

    window.EventSource = class MockEventSource {
      static readonly CONNECTING = 0
      static readonly OPEN = 1
      static readonly CLOSED = 2
      readonly CONNECTING = 0
      readonly OPEN = 1
      readonly CLOSED = 2
      readyState = 0
      url: string
      withCredentials = false
      onopen: ((this: EventSource, ev: Event) => any) | null = null
      onmessage: ((this: EventSource, ev: MessageEvent) => any) | null = null
      onerror: ((this: EventSource, ev: Event) => any) | null = null
      private _listeners: Record<string, Function[]> = {}

      constructor(url: string | URL) {
        this.url = url.toString()
        if (!this.url.includes('/api/downloads/queue/stream')) {
          return new OrigEventSource(url) as any
        }
        this.readyState = 1
        setTimeout(() => {
          if (this.onopen) this.onopen.call(this as any, new Event('open'))
          const emit = () => {
            const evt = new MessageEvent('queue', { data: JSON.stringify(data) })
            const listeners = this._listeners['queue'] || []
            for (const fn of listeners) fn(evt)
            if (this.onmessage) this.onmessage.call(this as any, evt)
          }
          emit()
          setTimeout(emit, 200)
        }, 100)
      }

      addEventListener(type: string, fn: Function) {
        if (!this._listeners[type]) this._listeners[type] = []
        this._listeners[type].push(fn)
      }

      removeEventListener(type: string, fn: Function) {
        const list = this._listeners[type]
        if (list) this._listeners[type] = list.filter(f => f !== fn)
      }

      dispatchEvent(event: Event): boolean {
        const listeners = this._listeners[event.type] || []
        for (const fn of listeners) fn(event)
        return true
      }

      close() {
        this.readyState = 2
      }
    } as any
  }, initial)
}

export async function mockLogStream(page: Page, entries: string[] = []) {
  await page.route('**/api/system/logs/stream', (route) => {
    const body = entries.map(e => `data: ${e}\n\n`).join('')
    return route.fulfill({
      status: 200,
      contentType: 'text/event-stream',
      headers: { 'Cache-Control': 'no-cache' },
      body,
    })
  })
}
