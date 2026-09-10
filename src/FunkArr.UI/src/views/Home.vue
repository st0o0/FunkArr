<template>
  <div>
    <div class="mb-6">
      <h1 class="text-lg font-medium text-text-primary">Dashboard</h1>
    </div>

    <div class="grid gap-3 grid-cols-3 mb-6">
      <div class="bg-surface-raised rounded-lg border border-border-default px-4 py-3">
        <div class="text-xs text-text-secondary mb-1">Active</div>
        <div class="text-xl font-semibold text-text-primary tabular-nums">{{ activeCount }} <span class="text-sm text-text-secondary font-normal">/ {{ totalSlots }}</span></div>
      </div>
      <div class="bg-surface-raised rounded-lg border border-border-default px-4 py-3">
        <div class="text-xs text-text-secondary mb-1">Queued</div>
        <div class="text-xl font-semibold text-text-primary tabular-nums">{{ queuedCount }}</div>
      </div>
      <div class="bg-surface-raised rounded-lg border border-border-default px-4 py-3">
        <div class="text-xs text-text-secondary mb-1">Speed</div>
        <div class="text-xl font-semibold text-text-primary tabular-nums">{{ formatSpeed(totalSpeed) }}</div>
      </div>
    </div>

    <div v-if="historyStats" class="grid gap-3 grid-cols-5 mb-6">
      <div class="bg-surface-raised rounded-lg border border-border-default px-4 py-3">
        <div class="text-xs text-text-secondary mb-1">Completed</div>
        <div class="text-xl font-semibold text-text-primary tabular-nums">{{ historyStats.totalCompleted }}</div>
      </div>
      <div class="bg-surface-raised rounded-lg border border-border-default px-4 py-3">
        <div class="text-xs text-text-secondary mb-1">Failed</div>
        <div class="text-xl font-semibold text-text-primary tabular-nums">{{ historyStats.totalFailed }}</div>
      </div>
      <div class="bg-surface-raised rounded-lg border border-border-default px-4 py-3">
        <div class="text-xs text-text-secondary mb-1">Success Rate</div>
        <div class="text-xl font-semibold text-text-primary tabular-nums">{{ formatPercent(historyStats.successRate) }}</div>
      </div>
      <div class="bg-surface-raised rounded-lg border border-border-default px-4 py-3">
        <div class="text-xs text-text-secondary mb-1">Total Size</div>
        <div class="text-xl font-semibold text-text-primary tabular-nums">{{ formatSize(historyStats.totalBytes) }}</div>
      </div>
      <div class="bg-surface-raised rounded-lg border border-border-default px-4 py-3">
        <div class="text-xs text-text-secondary mb-1">Avg Time</div>
        <div class="text-xl font-semibold text-text-primary tabular-nums">{{ formatDuration(historyStats.averageDownloadTimeSeconds) }}</div>
      </div>
    </div>

    <div v-if="storage" class="mb-6">
      <div class="bg-surface-raised rounded-lg border border-border-default px-4 py-3">
        <div class="flex items-center justify-between mb-2">
          <div class="text-xs text-text-secondary">Storage</div>
          <div class="text-xs text-text-secondary tabular-nums">
            {{ formatSize(storage.completeDirectory.totalBytes - storage.completeDirectory.availableBytes) }}
            / {{ formatSize(storage.completeDirectory.totalBytes) }}
          </div>
        </div>
        <div class="w-full h-2 rounded-full bg-surface-elevated overflow-hidden">
          <div
            class="h-full rounded-full transition-all"
            :class="storagePercent > 90 ? 'bg-status-warn' : 'bg-brand-500'"
            :style="{ width: `${Math.min(storagePercent, 100)}%` }"
          />
        </div>
      </div>
    </div>

    <div class="grid gap-4 lg:grid-cols-2">
      <HealthWidget />
      <ActiveDownloads />
    </div>

    <div v-if="cacheStats" class="mt-4">
      <div class="bg-surface-raised rounded-lg border border-border-default px-4 py-3">
        <div class="text-xs text-text-secondary mb-2">Metadata Cache</div>
        <div class="flex items-center gap-6 text-sm">
          <div class="text-text-body">
            <span class="font-medium tabular-nums">{{ cacheStats.tvdbEntries }}</span>
            <span class="text-text-secondary ml-1">TVDB</span>
          </div>
          <div class="text-text-body">
            <span class="font-medium tabular-nums">{{ cacheStats.tmdbEntries }}</span>
            <span class="text-text-secondary ml-1">TMDB</span>
          </div>
          <div v-if="cacheStats.oldestEntry" class="text-text-secondary text-xs">
            Oldest: {{ formatRelativeDate(cacheStats.oldestEntry) }}
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import HealthWidget from '../components/HealthWidget.vue'
import ActiveDownloads from '../components/ActiveDownloads.vue'
import { useGroupedQueue } from '../composables/useGroupedQueue'
import { getHistoryStats, type HistoryStatsResponse } from '../api/downloads'
import { getStorageStatus, getCacheStats, type StorageStatusResponse, type CacheStatsResponse } from '../api/setup'
import { formatSpeed, formatSize, formatDuration, formatPercent, formatRelativeDate } from '../utils/format'

const { activeCount, queuedCount, totalSpeed, totalSlots, release } = useGroupedQueue()

const historyStats = ref<HistoryStatsResponse | null>(null)
const storage = ref<StorageStatusResponse | null>(null)
const cacheStats = ref<CacheStatsResponse | null>(null)

const storagePercent = computed(() => {
  if (!storage.value || storage.value.completeDirectory.totalBytes === 0) return 0
  const used = storage.value.completeDirectory.totalBytes - storage.value.completeDirectory.availableBytes
  return (used / storage.value.completeDirectory.totalBytes) * 100
})

onMounted(async () => {
  const [statsResult, storageResult, cacheResult] = await Promise.allSettled([
    getHistoryStats(),
    getStorageStatus(),
    getCacheStats(),
  ])
  if (statsResult.status === 'fulfilled') historyStats.value = statsResult.value
  if (storageResult.status === 'fulfilled') storage.value = storageResult.value
  if (cacheResult.status === 'fulfilled') cacheStats.value = cacheResult.value
})

onUnmounted(release)
</script>
