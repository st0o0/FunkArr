<template>
  <div class="max-w-5xl mx-auto">
    <div class="mb-5">
      <h1 class="text-xl font-semibold text-text-primary tracking-tight">{{ $t('home.title') }}</h1>
    </div>

    <!-- Pipeline status bar -->
    <div class="bg-surface-raised rounded-lg border border-border-default px-4 py-3 mb-4">
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-4 text-sm">
          <div class="flex items-center gap-2">
            <span class="w-2 h-2 rounded-full shrink-0" :class="pipelineDotClass" />
            <span class="text-text-primary font-medium">{{ pipelineLabel }}</span>
          </div>
          <span v-if="activeCount > 0 || queuedCount > 0" class="text-text-secondary">
            {{ activeCount }} {{ $t('home.downloading') }} · {{ queuedCount }} {{ $t('home.queued') }}
            <span v-if="totalSpeed > 0"> · {{ formatSpeed(totalSpeed) }}</span>
          </span>
        </div>
        <div class="flex items-center gap-2">
          <button
            v-if="isPaused"
            @click="handleResume"
            class="px-2.5 py-1 text-xs font-medium rounded-md bg-accent text-white hover:bg-accent/80 transition-colors"
          >{{ $t('home.resume') }}</button>
          <button
            v-else-if="activeCount > 0 || queuedCount > 0"
            @click="handlePause"
            class="px-2.5 py-1 text-xs font-medium rounded-md bg-surface-elevated text-text-body border border-border-default hover:bg-surface-overlay transition-colors"
          >{{ $t('home.pause') }}</button>
          <router-link to="/activity" class="text-xs text-accent hover:text-accent/80 transition-colors">{{ $t('home.view') }}</router-link>
        </div>
      </div>
      <div v-if="activeCount > 0" class="mt-2 h-1 bg-surface-elevated rounded-full overflow-hidden">
        <div class="h-full bg-accent rounded-full transition-all duration-700" :style="{ width: `${overallProgress}%` }" />
      </div>
    </div>

    <!-- Main grid: 2 columns, stretch to equal height -->
    <div class="grid grid-cols-[1fr_320px] gap-4">
      <!-- Left column -->
      <div class="space-y-4">
        <!-- Active downloads (when downloading) -->
        <div v-if="activeItems.length > 0" class="bg-surface-raised rounded-lg border border-border-default overflow-hidden">
          <div class="px-4 py-2.5 border-b border-border-subtle">
            <h2 class="text-sm font-medium text-text-primary">{{ $t('home.activeDownloads') }}</h2>
          </div>
          <div class="divide-y divide-border-subtle">
            <div v-for="item in activeItems" :key="item.downloadId" class="px-4 py-3">
              <div class="flex items-center justify-between mb-1.5">
                <router-link :to="`/activity/${item.downloadId}`" class="text-sm text-text-primary font-medium hover:text-accent transition-colors truncate">
                  <ReleaseTitle :title="item.title" compact />
                </router-link>
                <span class="text-xs text-text-secondary tabular-nums shrink-0 ml-3">{{ item.percentage }}%</span>
              </div>
              <div class="h-1 bg-surface-elevated rounded-full overflow-hidden">
                <div class="h-full bg-accent rounded-full transition-all duration-700 ease-out" :style="{ width: `${item.percentage}%` }" />
              </div>
              <div class="flex justify-between text-xs text-text-muted mt-1 tabular-nums">
                <span>{{ formatSize(item.bytesDownloaded) }} / {{ formatSize(item.totalBytes) }}</span>
                <span v-if="item.speed > 0">{{ formatSpeed(item.speed) }} · {{ item.eta }}</span>
              </div>
            </div>
          </div>
        </div>

        <!-- Recent activity -->
        <div class="bg-surface-raised rounded-lg border border-border-default overflow-hidden">
          <div class="flex items-center justify-between px-4 py-2.5 border-b border-border-subtle">
            <h2 class="text-sm font-medium text-text-primary">{{ $t('home.recentActivity') }}</h2>
            <router-link to="/activity?tab=history" class="text-xs text-accent hover:text-accent/80 transition-colors">{{ $t('home.viewAll') }}</router-link>
          </div>
          <div v-if="recentItems.length === 0" class="px-4 py-8 text-center">
            <svg class="w-6 h-6 mx-auto mb-2 text-text-muted" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
              <path d="M8 2v8M5 7l3 3 3-3"/><path d="M2 12h12"/>
            </svg>
            <p class="text-sm text-text-body font-medium">{{ $t('home.noRecentActivity') }}</p>
            <p class="text-xs text-text-secondary mt-1">{{ $t('home.noRecentActivityHint') }}</p>
          </div>
          <div v-else>
            <div
              v-for="item in recentItems"
              :key="item.downloadId"
              class="flex items-center gap-3 px-4 py-2.5 border-b border-border-subtle last:border-b-0 hover:bg-surface-elevated/30 transition-colors"
            >
              <span class="w-1.5 h-1.5 rounded-full shrink-0" :class="item.status === HistoryStatus.Completed ? 'bg-status-ok' : 'bg-status-fail'" />
              <div class="min-w-0 flex-1">
                <ReleaseTitle :title="item.title" compact />
              </div>
              <div class="flex items-center gap-2 shrink-0">
                <button
                  v-if="item.status === HistoryStatus.Failed"
                  @click="handleRetry(item.downloadId)"
                  class="text-[11px] px-1.5 py-0.5 rounded bg-surface-elevated text-text-secondary hover:text-accent transition-colors"
                >{{ $t('home.retry') }}</button>
                <span v-if="item.totalBytes > 0" class="text-xs text-text-secondary tabular-nums">{{ formatSize(item.totalBytes) }}</span>
                <span class="text-xs text-text-muted tabular-nums">{{ formatRelativeDate(item.completedAt) }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Right column — stretch to match left -->
      <div class="flex flex-col gap-4">
        <!-- System health -->
        <div class="bg-surface-raised rounded-lg border border-border-default px-4 py-3">
          <div class="flex items-center justify-between mb-3">
            <h2 class="text-sm font-medium text-text-primary">{{ $t('home.system') }}</h2>
            <router-link to="/setup" class="text-xs text-accent hover:text-accent/80 transition-colors">{{ $t('home.details') }}</router-link>
          </div>
          <div class="flex items-center gap-2 mb-3">
            <span class="w-2 h-2 rounded-full shrink-0" :class="healthDotClass" />
            <span class="text-sm text-text-primary font-medium">{{ healthLabel }}</span>
          </div>
          <div class="grid grid-cols-2 gap-x-4 gap-y-2 text-xs">
            <div>
              <span class="text-text-muted">{{ $t('home.version') }}</span>
              <div class="text-text-body tabular-nums">v{{ appVersion || '...' }}</div>
            </div>
            <div>
              <span class="text-text-muted">{{ $t('home.rulesets') }}</span>
              <div class="text-text-body tabular-nums">{{ rulesetCount }}</div>
            </div>
          </div>
        </div>

        <!-- Feed ingest -->
        <div class="bg-surface-raised rounded-lg border border-border-default px-4 py-3">
          <div class="flex items-center justify-between mb-3">
            <h2 class="text-sm font-medium text-text-primary">{{ $t('home.feedIngest') }}</h2>
            <div class="flex items-center gap-2">
              <button
                @click="handleCrawl"
                :disabled="feedRunning"
                class="text-xs px-2 py-0.5 rounded-md bg-surface-elevated text-text-body border border-border-default hover:bg-surface-overlay disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
              >{{ $t('home.crawlNow') }}</button>
              <router-link to="/feed" class="text-xs text-accent hover:text-accent/80 transition-colors">{{ $t('home.view') }}</router-link>
            </div>
          </div>
          <div class="space-y-2 text-xs">
            <div class="flex justify-between">
              <span class="text-text-muted">{{ $t('home.lastCrawl') }}</span>
              <span class="text-text-body">{{ feedLastRun ? formatRelativeDate(feedLastRun) : $t('home.never') }}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-text-muted">{{ $t('home.feedOutcome') }}</span>
              <span class="text-text-body" :class="feedOutcomeClass">{{ feedOutcome || '-' }}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-text-muted">{{ $t('home.feedEntries') }}</span>
              <span class="text-text-body tabular-nums">{{ feedTotal }}</span>
            </div>
          </div>
        </div>

        <!-- Storage -->
        <div class="bg-surface-raised rounded-lg border border-border-default px-4 py-3">
          <div class="flex items-center justify-between mb-3">
            <h2 class="text-sm font-medium text-text-primary">{{ $t('home.storage') }}</h2>
          </div>
          <div class="flex items-baseline gap-1.5 mb-2">
            <span class="text-sm font-semibold text-text-primary tabular-nums">{{ storage ? formatSize(storageUsed) : '-' }}</span>
            <span v-if="storage" class="text-xs text-text-secondary">/ {{ formatSize(storage.completeDirectory.totalBytes) }}</span>
          </div>
          <div v-if="storage" class="h-1.5 rounded-full bg-surface-elevated overflow-hidden mb-2">
            <div
              class="h-full rounded-full transition-all"
              :class="storagePercent > 90 ? 'bg-status-warn' : 'bg-accent'"
              :style="{ width: `${Math.min(storagePercent, 100)}%` }"
            />
          </div>
          <div v-if="historyStats" class="grid grid-cols-2 gap-x-4 gap-y-1.5 text-xs">
            <div class="flex justify-between">
              <span class="text-text-muted">{{ $t('home.completed') }}</span>
              <span class="text-status-ok tabular-nums">{{ historyStats.totalCompleted }}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-text-muted">{{ $t('home.failed') }}</span>
              <span class="text-status-fail tabular-nums">{{ historyStats.totalFailed }}</span>
            </div>
          </div>
        </div>

        <!-- Rulesets — grows to fill remaining space -->
        <router-link to="/rulesets" class="block bg-surface-raised rounded-lg border border-border-default px-4 py-3 hover:bg-surface-elevated/50 transition-colors flex-1">
          <div class="flex items-center justify-between mb-2">
            <h2 class="text-sm font-medium text-text-primary">{{ $t('home.rulesets') }}</h2>
            <svg class="w-3.5 h-3.5 text-text-muted" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M6 4l4 4-4 4"/></svg>
          </div>
          <div class="flex items-baseline gap-2">
            <span class="text-lg font-semibold text-text-primary tabular-nums">{{ rulesetCount }}</span>
            <span v-if="communityVersion" class="text-xs text-text-muted">v{{ communityVersion }}</span>
          </div>
        </router-link>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useGroupedQueue } from '../composables/useGroupedQueue'
import { getHistory, getHistoryStats, retryDownload, pauseDownloads, resumeDownloads, type HistoryItem, type HistoryStatsResponse } from '../api/downloads'
import { getSetupHealth, getStorageStatus, getSystemVersion, type SetupHealthCheck, type StorageStatusResponse } from '../api/setup'
import { getFeed, triggerFeedCrawl } from '../api/feed'
import { listRuleSets } from '../api/rulesets'
import { QueueStatus, HistoryStatus, CheckStatus } from '../api/enums'
import { formatSpeed, formatSize, formatRelativeDate } from '../utils/format'
import { useToast } from '../composables/useToast'
import ReleaseTitle from '../components/ReleaseTitle.vue'

const { t } = useI18n()
const { toast } = useToast()
const { activeCount, queuedCount, totalSpeed, items, isPaused, release } = useGroupedQueue()

const health = ref<SetupHealthCheck | null>(null)
const storage = ref<StorageStatusResponse | null>(null)
const historyStats = ref<HistoryStatsResponse | null>(null)
const recentItems = ref<HistoryItem[]>([])
const rulesetCount = ref(0)
const communityVersion = ref<string | null>(null)
const appVersion = ref<string | null>(null)
const feedTotal = ref(0)
const feedLastRun = ref<string | null>(null)
const feedOutcome = ref<string | null>(null)
const feedRunning = ref(false)

const activeItems = computed(() =>
  items.value.filter(i => i.status === QueueStatus.Processing).slice(0, 3)
)

const overallProgress = computed(() => {
  const active = items.value.filter(i => i.status === QueueStatus.Processing)
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
  if (checks.some(c => c.status === CheckStatus.Fail)) return 'bg-status-fail'
  if (checks.some(c => c.status === CheckStatus.Warn)) return 'bg-status-warn'
  return 'bg-status-ok'
})

const healthLabel = computed(() => {
  if (!health.value) return '...'
  const checks = Object.values(health.value.checks)
  const failCount = checks.filter(c => c.status === CheckStatus.Fail).length
  const warnCount = checks.filter(c => c.status === CheckStatus.Warn).length
  if (failCount > 0) return t('home.issueCount', { count: failCount }, failCount)
  if (warnCount > 0) return t('home.warningCount', { count: warnCount }, warnCount)
  return t('home.healthy')
})

const pipelineDotClass = computed(() => {
  if (isPaused.value) return 'bg-status-warn'
  if (activeCount.value > 0) return 'bg-status-ok animate-pulse'
  return 'bg-text-muted'
})

const pipelineLabel = computed(() => {
  if (isPaused.value) return t('home.pipelinePaused')
  if (activeCount.value > 0) return t('home.pipelineActive')
  return t('home.pipelineIdle')
})

const feedOutcomeClass = computed(() => {
  if (!feedOutcome.value) return ''
  if (feedOutcome.value === 'completed') return 'text-status-ok'
  if (feedOutcome.value === 'failed') return 'text-status-fail'
  return ''
})

function handleVisibilityChange() {
  if (document.visibilityState === 'visible') fetchRecent()
}

async function fetchRecent() {
  try {
    const result = await getHistory(0, 10)
    recentItems.value = result.items
  } catch { /* ignore */ }
}

async function fetchFeedStatus() {
  try {
    const result = await getFeed({ limit: 0 })
    feedTotal.value = result.total
    feedLastRun.value = result.ingest.lastRunAt
    feedOutcome.value = result.ingest.lastOutcome
    feedRunning.value = result.ingest.running
  } catch { /* ignore */ }
}

async function handlePause() {
  try { await pauseDownloads(); toast(t('home.pauseToast')) } catch { /* ignore */ }
}

async function handleResume() {
  try { await resumeDownloads(); toast(t('home.resumeToast')) } catch { /* ignore */ }
}

async function handleRetry(id: string) {
  try {
    await retryDownload(id)
    toast(t('home.retryToast'))
    fetchRecent()
  } catch { /* ignore */ }
}

async function handleCrawl() {
  try {
    await triggerFeedCrawl()
    feedRunning.value = true
    toast(t('home.crawlToast'))
    setTimeout(fetchFeedStatus, 5000)
  } catch { /* ignore */ }
}

onMounted(async () => {
  document.addEventListener('visibilitychange', handleVisibilityChange)

  const [healthResult, storageResult, rulesetsResult, versionResult, statsResult] = await Promise.allSettled([
    getSetupHealth(),
    getStorageStatus(),
    listRuleSets(),
    getSystemVersion(),
    getHistoryStats(),
  ])
  if (healthResult.status === 'fulfilled') health.value = healthResult.value
  if (storageResult.status === 'fulfilled') storage.value = storageResult.value
  if (rulesetsResult.status === 'fulfilled') {
    rulesetCount.value = rulesetsResult.value.rulesets.length
    communityVersion.value = rulesetsResult.value.communityVersion
  }
  if (versionResult.status === 'fulfilled') appVersion.value = versionResult.value.appVersion
  if (statsResult.status === 'fulfilled') historyStats.value = statsResult.value

  fetchRecent()
  fetchFeedStatus()
})

onUnmounted(() => {
  document.removeEventListener('visibilitychange', handleVisibilityChange)
  release()
})
</script>
