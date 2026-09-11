<template>
  <div class="max-w-3xl mx-auto">
    <div class="mb-5">
      <h1 class="text-lg font-semibold text-text-primary">Overview</h1>
    </div>

    <!-- Status line: health + storage -->
    <div class="bg-surface-raised rounded-lg border border-border-default px-4 py-3 mb-3">
      <div class="flex items-center justify-between">
        <router-link to="/setup" class="flex items-center gap-2 hover:opacity-80 transition-opacity">
          <span
            class="w-2 h-2 rounded-full shrink-0"
            :class="healthDotClass"
          />
          <span class="text-sm text-text-body">{{ healthLabel }}</span>
        </router-link>
        <div v-if="storage" class="flex items-center gap-3">
          <div class="w-24 h-1.5 rounded-full bg-surface-elevated overflow-hidden">
            <div
              class="h-full rounded-full transition-all"
              :class="storagePercent > 90 ? 'bg-status-warn' : 'bg-accent'"
              :style="{ width: `${Math.min(storagePercent, 100)}%` }"
            />
          </div>
          <span class="text-xs text-text-secondary tabular-nums">
            {{ formatSize(storageUsed) }} / {{ formatSize(storage.completeDirectory.totalBytes) }}
          </span>
        </div>
      </div>
    </div>

    <!-- Queue progress line -->
    <div class="bg-surface-raised rounded-lg border border-border-default px-4 py-3 mb-4">
      <div v-if="activeCount > 0 || queuedCount > 0" class="space-y-2">
        <div class="flex items-center justify-between text-sm">
          <span class="text-text-body">
            {{ activeCount }} downloading
            <span class="text-text-secondary">- {{ queuedCount }} queued</span>
            <span v-if="totalSpeed > 0" class="text-text-secondary"> - {{ formatSpeed(totalSpeed) }}</span>
          </span>
          <router-link to="/activity" class="text-xs text-text-secondary hover:text-text-body transition-colors">View all</router-link>
        </div>
        <div class="h-1 bg-surface-elevated rounded-full overflow-hidden">
          <div
            class="h-full bg-accent rounded-full transition-all duration-700"
            :style="{ width: `${overallProgress}%` }"
          />
        </div>
      </div>
      <div v-else class="flex items-center justify-between">
        <span class="text-sm text-text-muted">No active downloads</span>
        <router-link to="/activity" class="text-xs text-text-secondary hover:text-text-body transition-colors">Activity</router-link>
      </div>
    </div>

    <!-- Recent activity feed -->
    <div class="bg-surface-raised rounded-lg border border-border-default overflow-hidden">
      <div class="flex items-center justify-between px-4 py-3 border-b border-border-subtle">
        <h2 class="text-sm font-semibold text-text-primary">Recent</h2>
        <router-link to="/activity" class="text-xs text-text-secondary hover:text-text-body transition-colors">View all</router-link>
      </div>
      <div v-if="recentItems.length === 0" class="px-4 py-6 text-center">
        <p class="text-sm text-text-muted">No recent activity</p>
      </div>
      <div v-else>
        <div
          v-for="item in recentItems"
          :key="item.downloadId"
          class="flex items-center gap-3 px-4 py-2.5 border-b border-border-subtle last:border-b-0"
        >
          <span
            class="w-1.5 h-1.5 rounded-full shrink-0"
            :class="item.status === 'Completed' ? 'bg-status-ok' : 'bg-status-fail'"
          />
          <div class="min-w-0 flex-1">
            <ReleaseTitle :title="item.title" compact />
          </div>
          <span
            class="text-xs text-text-muted shrink-0 tabular-nums"
            :title="item.status === 'Failed' && item.failMessage ? item.failMessage : undefined"
          >{{ formatRelativeDate(item.completedAt) }}</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useGroupedQueue } from '../composables/useGroupedQueue'
import { getHistory, type HistoryItem } from '../api/downloads'
import { getSetupHealth, getStorageStatus, type SetupHealthCheck, type StorageStatusResponse } from '../api/setup'
import { formatSpeed, formatSize, formatRelativeDate } from '../utils/format'
import ReleaseTitle from '../components/ReleaseTitle.vue'

const { activeCount, queuedCount, totalSpeed, items, release } = useGroupedQueue()

const health = ref<SetupHealthCheck | null>(null)
const storage = ref<StorageStatusResponse | null>(null)
const recentItems = ref<HistoryItem[]>([])

const overallProgress = computed(() => {
  const active = items.value.filter(i => i.status === 'Processing')
  if (active.length === 0) return 0
  const total = active.reduce((sum, i) => sum + i.percentage, 0)
  return Math.round(total / active.length)
})

const storageUsed = computed(() => {
  if (!storage.value) return 0
  return storage.value.completeDirectory.totalBytes - storage.value.completeDirectory.availableBytes
})

const storagePercent = computed(() => {
  if (!storage.value || storage.value.completeDirectory.totalBytes === 0) return 0
  return (storageUsed.value / storage.value.completeDirectory.totalBytes) * 100
})

const healthDotClass = computed(() => {
  if (!health.value) return 'bg-text-muted'
  const checks = Object.values(health.value.checks)
  if (checks.some(c => c.status === 'fail')) return 'bg-status-fail'
  if (checks.some(c => c.status === 'warn')) return 'bg-status-warn'
  return 'bg-status-ok'
})

const healthLabel = computed(() => {
  if (!health.value) return 'Loading...'
  const checks = Object.values(health.value.checks)
  const failCount = checks.filter(c => c.status === 'fail').length
  const warnCount = checks.filter(c => c.status === 'warn').length
  if (failCount > 0) return `${failCount} ${failCount === 1 ? 'failure' : 'failures'}`
  if (warnCount > 0) return `${warnCount} ${warnCount === 1 ? 'warning' : 'warnings'}`
  return 'System healthy'
})

function handleVisibilityChange() {
  if (document.visibilityState === 'visible') {
    fetchRecent()
  }
}

async function fetchRecent() {
  try {
    const result = await getHistory(0, 10)
    recentItems.value = result.items
  } catch { /* ignore */ }
}

onMounted(async () => {
  document.addEventListener('visibilitychange', handleVisibilityChange)

  const [healthResult, storageResult] = await Promise.allSettled([
    getSetupHealth(),
    getStorageStatus(),
  ])
  if (healthResult.status === 'fulfilled') health.value = healthResult.value
  if (storageResult.status === 'fulfilled') storage.value = storageResult.value

  fetchRecent()
})

onUnmounted(() => {
  document.removeEventListener('visibilitychange', handleVisibilityChange)
  release()
})
</script>
