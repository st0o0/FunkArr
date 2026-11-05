import { describe, test, expect, vi, beforeEach } from 'vitest'
import { ref } from 'vue'
import { QueueStatus, QueuePhase } from '../api/enums'

vi.mock('../composables/useQueueStream', () => {
  const items = ref<any[]>([])
  return {
    useQueueStream: () => ({
      items,
      totalSlots: ref(2),
      activeCount: ref(0),
      queuedCount: ref(0),
      isPaused: ref(false),
      isScheduleActive: ref(true),
      nextWindow: ref(null),
      connected: ref(true),
      release: () => {},
    }),
    __setItems: (newItems: any[]) => { items.value = newItems },
  }
})

describe('useGroupedQueue', () => {
  beforeEach(() => {
    vi.resetModules()
  })

  test('groups items by series', async () => {
    const { __setItems } = await import('../composables/useQueueStream') as any
    __setItems([
      { downloadId: '1', title: 'Tatort.S01E01.GERMAN.1080p.WEB.h264-FunkArr', status: QueueStatus.Processing, channel: 'ARD', category: 'tv', hasSubtitles: false, totalDuration: 0, totalBytes: 0, bytesDownloaded: 0, percentage: 0, phase: QueuePhase.VideoDownload, speed: 0, eta: '', priority: 0 },
      { downloadId: '2', title: 'Tatort.S01E02.GERMAN.1080p.WEB.h264-FunkArr', status: QueueStatus.Queued, channel: 'ARD', category: 'tv', hasSubtitles: false, totalDuration: 0, totalBytes: 0, bytesDownloaded: 0, percentage: 0, phase: 0, speed: 0, eta: '', priority: 0 },
      { downloadId: '3', title: 'Polizeiruf.S01E01.GERMAN.1080p.WEB.h264-FunkArr', status: QueueStatus.Queued, channel: 'ZDF', category: 'tv', hasSubtitles: false, totalDuration: 0, totalBytes: 0, bytesDownloaded: 0, percentage: 0, phase: 0, speed: 0, eta: '', priority: 0 },
    ])
    const { useGroupedQueue } = await import('../composables/useGroupedQueue')
    const { groups } = useGroupedQueue()
    expect(groups.value.length).toBe(2)
  })

  test('empty queue returns empty groups', async () => {
    const { __setItems } = await import('../composables/useQueueStream') as any
    __setItems([])
    const { useGroupedQueue } = await import('../composables/useGroupedQueue')
    const { groups } = useGroupedQueue()
    expect(groups.value.length).toBe(0)
  })
})
