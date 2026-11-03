import { computed } from 'vue'
import { useQueueStream } from './useQueueStream'
import { QueueStatus, QueuePriority } from '../api/enums'

export function usePriorityQueue() {
  const stream = useQueueStream()

  const active = computed(() =>
    stream.items.value.filter(i => i.status === QueueStatus.Processing)
  )

  const queued = computed(() =>
    stream.items.value.filter(i => i.status === QueueStatus.Queued)
  )

  const high = computed(() =>
    queued.value.filter(i => i.priority === QueuePriority.High)
  )

  const normal = computed(() =>
    queued.value.filter(i => i.priority === QueuePriority.Normal)
  )

  const low = computed(() =>
    queued.value.filter(i => i.priority === QueuePriority.Low)
  )

  const totalSpeed = computed(() =>
    active.value.reduce((sum, i) => sum + i.speed, 0)
  )

  const overallProgress = computed(() => {
    if (active.value.length === 0) return 0
    const total = active.value.reduce((sum, i) => sum + i.percentage, 0)
    return Math.round(total / active.value.length)
  })

  return {
    ...stream,
    active,
    queued,
    high,
    normal,
    low,
    totalSpeed,
    overallProgress,
  }
}
