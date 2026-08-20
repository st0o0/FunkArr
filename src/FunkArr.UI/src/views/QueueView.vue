<script setup lang="ts">
import { api, API_BASE } from '@/api/client'
import { usePolling } from '@/composables/usePolling'
import DownloadCard from '@/components/DownloadCard.vue'

interface QueueItem {
  nzoId: string
  title: string
  status: string
  progressPercent: number
  downloadedBytes: number
  totalBytes: number
  enqueuedAt: string
}

const { data: queue, loading, error } = usePolling<QueueItem[]>(
  () => api<QueueItem[]>(`${API_BASE}/queue`),
  3000,
)
</script>

<template>
  <div>
    <p v-if="loading" class="text-center text-neutral-500 py-12">Loading...</p>

    <p v-else-if="error" class="text-center text-red-600 dark:text-red-400 py-12">{{ error }}</p>

    <p v-else-if="!queue || queue.length === 0" class="text-center text-neutral-500 py-12">
      No active downloads
    </p>

    <div v-else class="flex flex-col gap-3">
      <DownloadCard
        v-for="item in queue"
        :key="item.nzoId"
        :title="item.title"
        :status="item.status"
        :progress-percent="item.progressPercent"
        :downloaded-bytes="item.downloadedBytes"
        :total-bytes="item.totalBytes"
      />
    </div>
  </div>
</template>
