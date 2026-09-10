import { computed } from 'vue'
import { useQueueStream } from './useQueueStream'
import type { QueueItem } from '../api/downloads'
import { parseReleaseName } from '../utils/releaseTitle'

export interface QueueGroup {
  series: string
  items: QueueItem[]
  activeCount: number
  queuedCount: number
  totalBytes: number
}

export function useGroupedQueue() {
  const { items, totalSlots, activeCount, queuedCount, connected, release } = useQueueStream()

  const groups = computed<QueueGroup[]>(() => {
    const map = new Map<string, QueueItem[]>()

    for (const item of items.value) {
      const parsed = parseReleaseName(item.title)
      const key = parsed.series || 'Unknown'
      const list = map.get(key)
      if (list) {
        list.push(item)
      } else {
        map.set(key, [item])
      }
    }

    const result: QueueGroup[] = []
    for (const [series, groupItems] of map) {
      const activeCount = groupItems.filter(i => i.status === 'Processing').length
      const queuedCount = groupItems.filter(i => i.status === 'Queued').length
      const totalBytes = groupItems.reduce((sum, i) => sum + i.totalBytes, 0)
      result.push({ series, items: groupItems, activeCount, queuedCount, totalBytes })
    }

    result.sort((a, b) => {
      if (a.activeCount > 0 && b.activeCount === 0) return -1
      if (a.activeCount === 0 && b.activeCount > 0) return 1
      return a.series.localeCompare(b.series)
    })

    return result
  })

  const totalSpeed = computed(() => items.value.reduce((sum, i) => sum + i.speed, 0))

  return { groups, items, activeCount, queuedCount, totalSpeed, totalSlots, connected, release }
}
