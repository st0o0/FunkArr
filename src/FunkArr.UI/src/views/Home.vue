<template>
  <div class="max-w-3xl mx-auto">
    <div class="mb-5">
      <h1 class="text-xl font-semibold text-text-primary tracking-tight">Overview</h1>
    </div>

    <!-- Stats row -->
    <div class="grid grid-cols-3 gap-3 mb-4">
      <router-link to="/setup" class="bg-surface-raised rounded-lg border border-border-default px-4 py-3 hover:bg-surface-elevated/50 transition-colors">
        <div class="flex items-center gap-2 mb-1">
          <span class="w-2 h-2 rounded-full shrink-0" :class="healthDotClass" />
          <span class="text-xs text-text-secondary">System</span>
        </div>
        <span class="text-lg font-semibold text-text-primary tabular-nums">{{ healthLabel }}</span>
      </router-link>

      <router-link to="/activity/history" class="bg-surface-raised rounded-lg border border-border-default px-4 py-3 hover:bg-surface-elevated/50 transition-colors">
        <div class="text-xs text-text-secondary mb-1">Recent Downloads</div>
        <span class="text-lg font-semibold text-text-primary tabular-nums">{{ recentItems.length }}</span>
      </router-link>

      <div class="bg-surface-raised rounded-lg border border-border-default px-4 py-3">
        <div class="text-xs text-text-secondary mb-1">Storage</div>
        <div class="flex items-baseline gap-1.5">
          <span class="text-lg font-semibold text-text-primary tabular-nums">{{ storage ? formatSize(storageUsed) : '—' }}</span>
          <span v-if="storage" class="text-xs text-text-muted">/ {{ formatSize(storage.completeDirectory.totalBytes) }}</span>
        </div>
        <div v-if="storage" class="mt-2 h-1 rounded-full bg-surface-elevated overflow-hidden">
          <div
            class="h-full rounded-full transition-all"
            :class="storagePercent > 90 ? 'bg-status-warn' : 'bg-accent'"
            :style="{ width: `${Math.min(storagePercent, 100)}%` }"
          />
        </div>
      </div>
    </div>

    <!-- Queue progress (only when active) -->
    <div v-if="activeCount > 0 || queuedCount > 0" class="bg-surface-raised rounded-lg border border-border-default px-4 py-3 mb-4">
      <div class="space-y-2">
        <div class="flex items-center justify-between text-sm">
          <span class="text-text-body">
            <span class="text-text-primary font-medium">{{ activeCount }}</span> downloading
            <span class="text-text-secondary"> · {{ queuedCount }} queued</span>
            <span v-if="totalSpeed > 0" class="text-text-secondary"> · {{ formatSpeed(totalSpeed) }}</span>
          </span>
          <router-link to="/activity" class="text-xs text-accent hover:text-accent/80 transition-colors">View</router-link>
        </div>
        <div class="h-1.5 bg-surface-elevated rounded-full overflow-hidden">
          <div
            class="h-full bg-accent rounded-full transition-all duration-700"
            :style="{ width: `${overallProgress}%` }"
          />
        </div>
      </div>
    </div>

    <!-- Recent activity feed -->
    <div class="bg-surface-raised rounded-lg border border-border-default overflow-hidden">
      <div class="flex items-center justify-between px-4 py-3 border-b border-border-subtle">
        <h2 class="text-sm font-medium text-text-primary">Recent Activity</h2>
        <router-link to="/activity/history" class="text-xs text-accent hover:text-accent/80 transition-colors">View all</router-link>
      </div>
      <div v-if="recentItems.length === 0" class="px-4 py-8 text-center">
        <p class="text-sm text-text-muted">No recent activity</p>
        <p class="text-xs text-text-muted mt-1">Downloads appear when Sonarr or Radarr trigger a search</p>
      </div>
      <div v-else>
        <div
          v-for="item in recentItems"
          :key="item.downloadId"
          class="flex items-center gap-3 px-4 py-3 border-b border-border-subtle last:border-b-0 hover:bg-surface-elevated/30 transition-colors"
        >
          <span
            class="w-1.5 h-1.5 rounded-full shrink-0"
            :class="item.status === 'Completed' ? 'bg-status-ok' : 'bg-status-fail'"
          />
          <div class="min-w-0 flex-1">
            <ReleaseTitle :title="item.title" compact />
          </div>
          <div class="flex items-center gap-3 shrink-0">
            <span v-if="item.totalBytes > 0" class="text-xs text-text-secondary tabular-nums">{{ formatSize(item.totalBytes) }}</span>
            <span class="text-xs text-text-body tabular-nums">{{ formatRelativeDate(item.completedAt) }}</span>
          </div>
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
  if (!health.value) return '...'
  const checks = Object.values(health.value.checks)
  const failCount = checks.filter(c => c.status === 'fail').length
  const warnCount = checks.filter(c => c.status === 'warn').length
  if (failCount > 0) return `${failCount} ${failCount === 1 ? 'issue' : 'issues'}`
  if (warnCount > 0) return `${warnCount} ${warnCount === 1 ? 'warning' : 'warnings'}`
  return 'Healthy'
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
