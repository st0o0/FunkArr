import { ref, readonly } from 'vue'
import type { LogEntry } from '../api/setup'

const entries = ref<LogEntry[]>([])
const connected = ref(false)

let eventSource: EventSource | null = null
let refCount = 0

function connect() {
  if (eventSource && eventSource.readyState !== EventSource.CLOSED) return
  if (eventSource) {
    eventSource.close()
    eventSource = null
  }

  eventSource = new EventSource('/api/system/logs/stream')

  eventSource.onmessage = (e: MessageEvent) => {
    const entry: LogEntry = JSON.parse(e.data)
    entries.value = [...entries.value.slice(-499), entry]
    connected.value = true
  }

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

export function useLogStream() {
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
    entries: readonly(entries),
    connected: readonly(connected),
    release,
  }
}
