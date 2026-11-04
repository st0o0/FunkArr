<template>
  <div class="max-w-5xl mx-auto">
    <div class="flex items-center justify-between mb-5">
      <div class="flex items-center gap-3">
        <h1 class="text-xl font-semibold text-text-primary tracking-tight">{{ $t('activity.title') }}</h1>
        <span v-if="totalSpeed > 0" class="text-xs text-accent tabular-nums font-medium px-2 py-0.5 rounded-md bg-accent/10">{{ formatSpeed(totalSpeed) }}</span>
      </div>
      <div class="flex items-center gap-2">
        <span v-if="pipelineIsPaused" class="text-xs text-status-warn px-2 py-0.5 rounded-md bg-status-warn/10">{{ $t('queue.paused') }}</span>
        <span v-else-if="!pipelineScheduleActive && pipelineNextWindow" class="text-xs text-status-info px-2 py-0.5 rounded-md bg-status-info/10">
          {{ $t('queue.scheduledAt', { time: formatAbsoluteDate(pipelineNextWindow) }) }}
        </span>
        <button
          @click="handleTogglePause"
          class="px-2.5 py-1 text-xs rounded-md border border-border-default text-text-secondary hover:text-text-body hover:bg-surface-elevated transition-colors"
        >
          {{ pipelineIsPaused ? $t('queue.resume') : $t('queue.pause') }}
        </button>
      </div>
    </div>

    <!-- Search -->
    <input
      v-model="searchQuery"
      type="text"
      :placeholder="$t('activity.search')"
      class="w-full bg-surface-elevated border border-border-default rounded-md px-3 py-1.5 text-sm text-text-body placeholder-text-muted mb-3 focus:outline-none focus:border-border-focus"
    />

    <!-- Filter chips -->
    <div class="flex items-center gap-1.5 mb-3">
      <button
        v-for="chip in filterChips"
        :key="chip.id"
        @click="activeFilter = chip.id"
        class="px-3 py-1 rounded-md text-xs transition-colors"
        :class="activeFilter === chip.id
          ? 'bg-surface-elevated text-text-primary'
          : 'text-text-secondary hover:text-text-body'"
      >
        {{ chip.label }}
        <span class="ml-1 tabular-nums" :class="activeFilter === chip.id ? 'text-text-body' : 'text-text-secondary'">{{ chip.count }}</span>
      </button>
    </div>

    <!-- Filter dropdowns -->
    <div class="flex items-center gap-2 mb-4">
      <select
        v-model="selectedPriority"
        class="bg-surface-elevated border border-border-default rounded-md px-3 py-1.5 text-xs text-text-body focus:border-border-focus focus:outline-none transition-colors"
      >
        <option value="">{{ $t('activity.allPriorities') }}</option>
        <option :value="1">{{ $t('queue.priorityHigh') }}</option>
        <option :value="0">{{ $t('queue.priorityNormal') }}</option>
        <option :value="-1">{{ $t('queue.priorityLow') }}</option>
      </select>
      <select
        v-model="selectedChannel"
        class="bg-surface-elevated border border-border-default rounded-md px-3 py-1.5 text-xs text-text-body focus:border-border-focus focus:outline-none transition-colors"
      >
        <option value="">{{ $t('activity.allChannels') }}</option>
        <option v-for="ch in channels" :key="ch" :value="ch">{{ ch }}</option>
      </select>
      <select
        v-if="showCategoryFilter"
        v-model="selectedCategory"
        class="bg-surface-elevated border border-border-default rounded-md px-3 py-1.5 text-xs text-text-body focus:border-border-focus focus:outline-none transition-colors"
      >
        <option value="">{{ $t('activity.allCategories') }}</option>
        <option v-for="cat in categories" :key="cat" :value="cat">{{ cat }}</option>
      </select>
    </div>

    <!-- Empty state -->
    <EmptyState
      v-if="tableRows.length === 0 && !historyLoading"
      icon='<path d="M8 2v8M5 7l3 3 3-3"/><path d="M2 12h12"/>'
      :title="$t('activity.noDownloads')"
      :description="$t('activity.noDownloadsHint')"
    />

    <!-- Table -->
    <div v-else class="overflow-x-auto rounded-lg border border-border-default">
      <table class="w-full text-sm table-fixed">
        <colgroup>
          <col class="w-10" />
          <col class="w-auto" />
          <col class="w-36" />
          <col class="w-16" />
          <col class="w-20" />
          <col class="w-24" />
        </colgroup>
        <thead>
          <tr class="bg-surface-raised text-left text-xs text-text-secondary border-b border-border-default">
            <th class="px-3 py-2.5 font-medium text-center">{{ $t('activity.position') }}</th>
            <th class="px-4 py-2.5 font-medium">{{ $t('activity.title_column') }}</th>
            <th class="px-3 py-2.5 font-medium">{{ $t('activity.status') }}</th>
            <th class="px-3 py-2.5 font-medium">{{ $t('activity.priority_column') }}</th>
            <th class="px-3 py-2.5 font-medium text-right">{{ $t('activity.size') }}</th>
            <th class="px-3 py-2.5 font-medium text-right">{{ $t('activity.actions') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="row in tableRows"
            :key="row.id"
            class="border-b border-border-subtle last:border-b-0 hover:bg-surface-elevated/40 transition-colors cursor-pointer"
            @click="$router.push(`/activity/${row.id}`)"
          >
            <td class="px-3 py-2.5 text-center text-xs text-text-secondary tabular-nums">{{ row.position ?? '' }}</td>
            <td class="px-4 py-2.5 max-w-0">
              <ReleaseTitle :title="row.title" hideQuality />
              <div v-if="row.channel" class="text-xs text-text-secondary mt-0.5">{{ row.channel }}</div>
              <details v-if="row.failMessage" class="mt-1">
                <summary class="text-xs text-status-fail cursor-pointer truncate">{{ row.failMessage.split('\n')[0].slice(0, 60) }}</summary>
                <pre class="text-xs text-status-fail/80 mt-1 whitespace-pre-wrap font-mono bg-surface-elevated/50 rounded p-2 max-h-24 overflow-y-auto">{{ row.failMessage }}</pre>
              </details>
            </td>
            <td class="px-3 py-2.5">
              <div v-if="row.type === 'active'" class="space-y-1">
                <span class="text-xs text-accent font-medium">{{ row.phaseLabel }}</span>
                <div class="w-full bg-surface-elevated rounded-full h-1">
                  <div class="bg-accent h-1 rounded-full transition-all" :style="{ width: `${row.percentage}%` }" />
                </div>
                <span class="text-[10px] text-text-secondary tabular-nums">{{ row.percentage }}% · {{ formatSpeed(row.speed) }}</span>
              </div>
              <span v-else class="inline-flex items-center gap-1.5 text-xs">
                <span class="w-1.5 h-1.5 rounded-full" :class="row.statusDot" />
                {{ row.statusLabel }}
              </span>
            </td>
            <td class="px-3 py-2.5 text-xs text-text-secondary">{{ row.priorityLabel }}</td>
            <td class="px-3 py-2.5 text-text-secondary tabular-nums text-right text-xs">{{ row.size }}</td>
            <td class="px-3 py-2.5 text-right" @click.stop>
              <div class="flex items-center justify-end gap-1">
                <button
                  v-if="row.type === 'failed'"
                  @click="handleRetry(row.id)"
                  class="px-2 py-1 text-xs text-text-secondary hover:text-text-body border border-border-default rounded transition-colors"
                >{{ $t('activity.retry') }}</button>
                <button
                  @click="handleDeleteRow(row)"
                  class="p-1 text-text-secondary hover:text-status-fail transition-colors rounded"
                  :title="$t('queue.delete')"
                >
                  <svg class="w-3.5 h-3.5" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round">
                    <path d="M4 4l8 8M12 4l-8 8" />
                  </svg>
                </button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Pagination (history filters only) -->
    <div v-if="showPagination" class="flex items-center justify-between mt-4 text-xs text-text-secondary">
      <span class="tabular-nums">{{ rangeStart + 1 }}-{{ Math.min(rangeStart + pageSize, historyTotal) }} {{ $t('common.of') }} {{ historyTotal }}</span>
      <div class="flex gap-1.5">
        <button :disabled="page <= 1" @click="page--"
          class="px-2.5 py-1 rounded-md border border-border-default text-text-secondary hover:bg-surface-elevated disabled:opacity-30 disabled:cursor-not-allowed transition-colors">
          {{ $t('activity.prev') }}
        </button>
        <button :disabled="page >= totalPages" @click="page++"
          class="px-2.5 py-1 rounded-md border border-border-default text-text-secondary hover:bg-surface-elevated disabled:opacity-30 disabled:cursor-not-allowed transition-colors">
          {{ $t('activity.next') }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQueueStream } from '../composables/useQueueStream'
import {
  getHistory, getHistoryCategories, getHistoryStats, deleteHistoryItem, retryDownload,
  deleteQueueItem, pauseDownloads, resumeDownloads,
  type QueueItem, type HistoryItem, type HistoryResponse, type HistoryStatsResponse,
} from '../api/downloads'
import { QueueStatus, QueuePriority, HistoryStatus } from '../api/enums'
import EmptyState from '../components/EmptyState.vue'
import ReleaseTitle from '../components/ReleaseTitle.vue'
import { useToast } from '../composables/useToast'
import { formatSpeed, formatSize, formatAbsoluteDate } from '../utils/format'

type FilterId = 'all' | 'active' | 'queued' | 'completed' | 'failed'

interface TableRow {
  id: string
  position: number | null
  title: string
  channel: string
  type: 'active' | 'queued' | 'completed' | 'failed'
  statusLabel: string
  statusDot: string
  phaseLabel: string
  percentage: number
  speed: number
  priorityLabel: string
  priority: number
  size: string
  failMessage: string | null
  isQueue: boolean
}

const { t } = useI18n()
const { toast } = useToast()
const {
  items: queueItems, activeCount, queuedCount,
  isPaused: pipelineIsPaused, isScheduleActive: pipelineScheduleActive,
  nextWindow: pipelineNextWindow, release,
} = useQueueStream()

const activeFilter = ref<FilterId>('all')
const searchQuery = ref('')
const selectedPriority = ref<number | ''>('')
const selectedChannel = ref('')
const selectedCategory = ref('')

const history = ref<HistoryResponse | null>(null)
const historyStats = ref<HistoryStatsResponse | null>(null)
const historyLoading = ref(false)
const categories = ref<string[]>([])
const pageSize = 25
const page = ref(1)
const rangeStart = computed(() => (page.value - 1) * pageSize)
const historyTotal = computed(() => history.value?.totalItems ?? 0)
const totalPages = computed(() => historyTotal.value > 0 ? Math.ceil(historyTotal.value / pageSize) : 1)
const needsHistory = computed(() => activeFilter.value === 'all' || activeFilter.value === 'completed' || activeFilter.value === 'failed')
const showCategoryFilter = needsHistory
const showPagination = computed(() => needsHistory.value && totalPages.value > 1)

const phaseNames = ['Downloading', 'Remuxing', 'Moving'] as const

const totalSpeed = computed(() =>
  queueItems.value
    .filter(i => i.status === QueueStatus.Processing)
    .reduce((sum, i) => sum + i.speed, 0)
)

const channels = computed(() => {
  const set = new Set(queueItems.value.map(i => i.channel).filter(Boolean))
  return [...set].sort()
})

function priorityLabel(p: number): string {
  if (p === QueuePriority.High) return t('queue.priorityHigh')
  if (p === QueuePriority.Low) return t('queue.priorityLow')
  return t('queue.priorityNormal')
}

function queueItemToRow(item: QueueItem, idx: number): TableRow {
  const isActive = item.status === QueueStatus.Processing
  return {
    id: item.downloadId,
    position: idx + 1,
    title: item.title,
    channel: item.channel,
    type: isActive ? 'active' : 'queued',
    statusLabel: isActive ? (phaseNames[item.phase] ?? 'Processing') : t('queue.queuedStatus'),
    statusDot: isActive ? 'bg-accent' : 'bg-text-muted',
    phaseLabel: phaseNames[item.phase] ?? 'Processing',
    percentage: item.percentage,
    speed: item.speed,
    priorityLabel: priorityLabel(item.priority),
    priority: item.priority,
    size: item.totalBytes > 0 ? formatSize(item.totalBytes) : '-',
    failMessage: null,
    isQueue: true,
  }
}

function historyItemToRow(item: HistoryItem): TableRow {
  const isFailed = item.status === HistoryStatus.Failed
  return {
    id: item.downloadId,
    position: null,
    title: item.title,
    channel: '',
    type: isFailed ? 'failed' : 'completed',
    statusLabel: isFailed ? 'Failed' : 'Completed',
    statusDot: isFailed ? 'bg-status-fail' : 'bg-status-ok',
    phaseLabel: '',
    percentage: 0,
    speed: 0,
    priorityLabel: '',
    priority: 0,
    size: item.totalBytes > 0 ? formatSize(item.totalBytes) : '-',
    failMessage: item.failMessage,
    isQueue: false,
  }
}

const filteredQueueItems = computed(() => {
  let items = [...queueItems.value]
  const q = searchQuery.value.toLowerCase()
  if (q) items = items.filter(i => i.title.toLowerCase().includes(q))
  if (selectedPriority.value !== '') items = items.filter(i => i.priority === Number(selectedPriority.value))
  if (selectedChannel.value) items = items.filter(i => i.channel === selectedChannel.value)
  return items
})

const activeRows = computed(() =>
  filteredQueueItems.value
    .filter(i => i.status === QueueStatus.Processing)
    .map((item, idx) => queueItemToRow(item, idx))
)

const queuedRows = computed(() =>
  filteredQueueItems.value
    .filter(i => i.status === QueueStatus.Queued)
    .map((item, idx) => queueItemToRow(item, activeRows.value.length + idx))
)

const historyRows = computed(() =>
  (history.value?.items ?? []).map(historyItemToRow)
)

const tableRows = computed<TableRow[]>(() => {
  switch (activeFilter.value) {
    case 'active': return activeRows.value
    case 'queued': return queuedRows.value
    case 'completed': return historyRows.value.filter(r => r.type === 'completed')
    case 'failed': return historyRows.value.filter(r => r.type === 'failed')
    default: return [...activeRows.value, ...queuedRows.value, ...historyRows.value]
  }
})

const completedCount = computed(() => historyStats.value?.totalCompleted ?? 0)
const failedCount = computed(() => historyStats.value?.totalFailed ?? 0)

const filterChips = computed(() => [
  { id: 'all' as FilterId, label: t('activity.all'), count: activeCount.value + queuedCount.value + completedCount.value + failedCount.value },
  { id: 'active' as FilterId, label: t('activity.active'), count: activeCount.value },
  { id: 'queued' as FilterId, label: t('activity.queued'), count: queuedCount.value },
  { id: 'completed' as FilterId, label: t('activity.completed'), count: completedCount.value },
  { id: 'failed' as FilterId, label: t('activity.failed'), count: failedCount.value },
])

async function fetchHistory() {
  historyLoading.value = true
  try {
    const cat = selectedCategory.value || undefined
    const [hist] = await Promise.all([getHistory(rangeStart.value, pageSize, cat), fetchStats()])
    history.value = hist
  } catch { /* ignore */ }
  finally { historyLoading.value = false }
}

async function fetchStats() {
  try { historyStats.value = await getHistoryStats() } catch { /* ignore */ }
}

async function fetchCategories() {
  try { categories.value = await getHistoryCategories() } catch { /* ignore */ }
}

async function handleTogglePause() {
  try {
    if (pipelineIsPaused.value) {
      await resumeDownloads()
      toast(t('queue.resumed'))
    } else {
      await pauseDownloads()
      toast(t('queue.pausedMessage'))
    }
  } catch {
    toast(t('queue.toggleFailed'), 'error')
  }
}

async function handleDeleteRow(row: TableRow) {
  try {
    if (row.isQueue) {
      await deleteQueueItem(row.id)
      toast(t('activity.downloadCancelled'))
    } else {
      await deleteHistoryItem(row.id)
      toast(t('activity.entryDeleted'))
      await fetchHistory()
    }
  } catch {
    toast(t('activity.failedToDelete'), 'error')
  }
}

async function handleRetry(id: string) {
  try {
    await retryDownload(id)
    toast(t('activity.retryStarted'))
    await fetchHistory()
  } catch {
    toast(t('activity.failedToRetry'), 'error')
  }
}

watch(activeFilter, () => {
  page.value = 1
  if (needsHistory.value) fetchHistory()
})

watch(page, () => fetchHistory())

watch(selectedCategory, () => {
  page.value = 1
  fetchHistory()
})

onMounted(() => {
  fetchHistory()
  fetchCategories()
})

onUnmounted(release)
</script>
