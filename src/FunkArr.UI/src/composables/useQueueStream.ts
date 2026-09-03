import { ref, readonly } from 'vue'
import type { QueueItem, QueueResponse } from '../api/downloads'

const items = ref<QueueItem[]>([])
const totalSlots = ref(0)
const connected = ref(false)

let eventSource: EventSource | null = null
let refCount = 0

function connect() {
  if (eventSource) return

  eventSource = new EventSource('/api/downloads/queue/stream')

  eventSource.addEventListener('queue', (e: MessageEvent) => {
    const data: QueueResponse = JSON.parse(e.data)
    items.value = data.items
    totalSlots.value = data.totalSlots
    connected.value = true
  })

  eventSource.onerror = () => {
    connected.value = false
  }

  eventSource.onopen = () => {
    connected.value = true
  }
}

function disconnect() {
  if (eventSource) {
    eventSource.close()
    eventSource = null
    connected.value = false
  }
}

export function useQueueStream() {
  refCount++
  if (refCount === 1) {
    connect()
  }

  function release() {
    refCount--
    if (refCount <= 0) {
      refCount = 0
      disconnect()
    }
  }

  return {
    items: readonly(items),
    totalSlots: readonly(totalSlots),
    connected: readonly(connected),
    release,
  }
}
