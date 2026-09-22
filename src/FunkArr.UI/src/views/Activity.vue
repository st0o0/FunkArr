<template>
  <div class="max-w-3xl mx-auto">
    <div class="flex items-center justify-between mb-5">
      <div class="flex items-center gap-3">
        <h1 class="text-xl font-semibold text-text-primary tracking-tight">{{ $t('activity.title') }}</h1>
        <span v-if="totalSpeed > 0" class="text-xs text-accent tabular-nums font-medium px-2 py-0.5 rounded-md bg-accent/10">{{ formatSpeed(totalSpeed) }}</span>
      </div>
      <input
        v-model="searchQuery"
        type="text"
        :placeholder="$t('activity.search')"
        class="bg-surface-elevated border border-border-default rounded-md px-3 py-1.5 text-sm text-text-body placeholder-text-muted w-40 focus:outline-none focus:border-border-focus"
      />
    </div>

    <div class="flex items-center justify-between mb-4">
      <div class="flex items-center gap-1">
        <button
          v-for="tab in tabs"
          :key="tab.id"
          @click="activeTab = tab.id"
          class="px-3 py-1.5 rounded-md text-xs transition-colors"
          :class="activeTab === tab.id
            ? 'bg-surface-elevated text-text-primary'
            : 'text-text-secondary hover:text-text-body'"
        >
          {{ tab.label }}
          <span
            v-if="tab.count > 0"
            class="ml-1 tabular-nums"
            :class="activeTab === tab.id ? 'text-text-body' : 'text-text-secondary'"
          >{{ tab.count }}</span>
        </button>
      </div>

      <div v-if="activeTab === 'queue'" class="flex items-center gap-2">
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

    <!-- Queue tab -->
    <div v-if="activeTab === 'queue'">
      <EmptyState
        v-if="active.length === 0 && queued.length === 0"
        icon='<path d="M8 2v8M5 7l3 3 3-3"/><path d="M2 12h12"/>'
        :title="$t('activity.noActiveDownloads')"
        :description="$t('activity.noActiveDownloadsHint')"
      />
      <template v-else>
        <!-- Active downloads -->
        <div v-if="active.length > 0">
          <div class="flex items-center gap-2 mb-2">
            <div class="h-px flex-1 bg-accent/30" />
            <span class="text-xs font-medium px-2 text-accent">{{ $t('activity.active') }} ({{ active.length }})</span>
            <div class="h-px flex-1 bg-accent/30" />
          </div>
          <div class="bg-surface-raised rounded-lg border border-border-default px-4 py-2 mb-1">
            <div class="h-1 bg-surface-elevated rounded-full overflow-hidden">
              <div class="h-full bg-accent rounded-full transition-all duration-700" :style="{ width: `${overallProgress}%` }" />
            </div>
          </div>
          <div class="space-y-1.5 mb-2">
            <div
              v-for="item in active"
              :key="item.downloadId"
              class="px-4 py-2.5 bg-surface-raised rounded-lg border border-border-default"
            >
              <ActiveDownloadCard :item="item" @menu="openContextMenu" />
            </div>
          </div>
        </div>

        <!-- Priority buckets -->
        <PrioritySection
          :items="high"
          priority="High"
          :label="$t('queue.priorityHigh')"
          :show-empty="isDragActive"
          @menu="openContextMenu"
          @reorder="handleReorder"
          @priority-change="handlePriorityChange"
          @drag-start="onDragStart"
          @drag-end="onDragEnd"
        />
        <PrioritySection
          :items="normal"
          priority="Normal"
          :label="$t('queue.priorityNormal')"
          :show-empty="isDragActive"
          @menu="openContextMenu"
          @reorder="handleReorder"
          @priority-change="handlePriorityChange"
          @drag-start="onDragStart"
          @drag-end="onDragEnd"
        />
        <PrioritySection
          :items="low"
          priority="Low"
          :label="$t('queue.priorityLow')"
          :show-empty="isDragActive"
          @menu="openContextMenu"
          @reorder="handleReorder"
          @priority-change="handlePriorityChange"
          @drag-start="onDragStart"
          @drag-end="onDragEnd"
        />
      </template>
    </div>

    <!-- History tab -->
    <div v-if="activeTab === 'history'">
      <div class="flex items-center gap-2 mb-3">
        <select
          v-model="selectedCategory"
          class="bg-surface-elevated border border-border-default rounded-md px-3 py-1.5 text-sm text-text-body focus:border-border-focus focus:outline-none transition-colors"
        >
          <option value="">{{ $t('activity.allCategories') }}</option>
          <option v-for="cat in categories" :key="cat" :value="cat">{{ cat }}</option>
        </select>
      </div>

      <SkeletonTable v-if="historyLoading && !history" :rows="5" :columns="6" />
      <div v-else-if="historyError" class="text-status-fail text-sm">{{ historyError }}</div>

      <EmptyState
        v-else-if="history && history.items.length === 0"
        icon='<circle cx="8" cy="8" r="6"/><path d="M8 4v4l2.5 2.5"/>'
        :title="$t('activity.noDownloadHistory')"
        :description="$t('activity.noDownloadHistoryHint')"
      />

      <div v-else-if="history">
        <div class="overflow-x-auto rounded-lg border border-border-default">
          <table class="w-full text-sm table-fixed">
            <colgroup>
              <col class="w-auto" />
              <col class="w-16" />
              <col class="w-24" />
              <col class="w-20" />
              <col class="w-24" />
              <col class="w-36" />
              <col class="w-20" />
            </colgroup>
            <thead>
              <tr class="bg-surface-raised text-left text-xs text-text-secondary border-b border-border-default">
                <th class="px-4 py-2.5 font-medium">{{ $t('activity.title_column') }}</th>
                <th class="px-3 py-2.5 font-medium text-center">{{ $t('activity.quality') }}</th>
                <th class="px-3 py-2.5 font-medium text-right">{{ $t('activity.size') }}</th>
                <th class="px-3 py-2.5 font-medium text-right">{{ $t('activity.duration') }}</th>
                <th class="px-3 py-2.5 font-medium">{{ $t('activity.status') }}</th>
                <th class="px-3 py-2.5 font-medium">{{ $t('activity.completed') }}</th>
                <th class="px-3 py-2.5 font-medium"></th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="item in filteredHistoryItems"
                :key="item.downloadId"
                class="border-b border-border-subtle last:border-b-0 hover:bg-surface-elevated/40 transition-colors"
              >
                <td class="px-4 py-2.5 max-w-0">
                  <ReleaseTitle :title="item.title" hideQuality />
                  <details v-if="item.failMessage" class="mt-1 group">
                    <summary class="text-xs text-status-fail cursor-pointer hover:text-status-fail/80 transition-colors select-none truncate" :title="item.failMessage">
                      {{ item.failMessage.split('\n')[0].slice(0, 80) }}{{ item.failMessage.length > 80 ? '...' : '' }}
                    </summary>
                    <pre class="text-xs text-status-fail/80 mt-1 whitespace-pre-wrap font-mono bg-surface-elevated/50 rounded p-2 max-h-32 overflow-y-auto">{{ item.failMessage }}</pre>
                  </details>
                </td>
                <td class="px-3 py-2.5 text-center">
                  <span
                    v-if="getQuality(item.title)"
                    class="text-xs font-medium px-1.5 py-0.5 rounded bg-surface-elevated text-text-secondary"
                  >{{ getQuality(item.title) }}</span>
                </td>
                <td class="px-3 py-2.5 text-text-secondary tabular-nums text-right">{{ formatSize(item.totalBytes) }}</td>
                <td class="px-3 py-2.5 text-text-secondary tabular-nums text-right">{{ formatDuration(item.downloadTimeSeconds) }}</td>
                <td class="px-3 py-2.5">
                  <span class="inline-flex items-center gap-1.5 text-xs">
                    <span
                      class="w-1.5 h-1.5 rounded-full"
                      :class="item.status === HistoryStatus.Completed ? 'bg-status-ok' : 'bg-status-fail'"
                    />
                    {{ item.status === HistoryStatus.Completed ? 'Completed' : 'Failed' }}
                  </span>
                </td>
                <td class="px-3 py-2.5 text-text-secondary text-xs tabular-nums" :title="formatRelativeDate(item.completedAt)">{{ formatAbsoluteDate(item.completedAt) }}</td>
                <td class="px-3 py-2.5 text-right">
                  <div class="flex items-center justify-end gap-1">
                    <button
                      v-if="item.status === HistoryStatus.Failed"
                      @click="handleRetry(item.downloadId)"
                      class="px-2 py-1 text-xs text-text-secondary hover:text-text-body border border-border-default rounded transition-colors"
                    >
                      {{ $t('activity.retry') }}
                    </button>
                    <button
                      @click="handleDeleteHistory(item.downloadId)"
                      class="p-1 text-text-secondary hover:text-status-fail transition-colors rounded"
                      title="Delete"
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

        <div v-if="totalPages > 1" class="flex items-center justify-between mt-4 text-xs text-text-secondary">
          <span class="tabular-nums">{{ rangeStart + 1 }}-{{ Math.min(rangeStart + pageSize, history.totalItems) }} {{ $t('common.of') }} {{ history.totalItems }}</span>
          <div class="flex gap-1.5">
            <button
              :disabled="page <= 1"
              @click="page--"
              class="px-2.5 py-1 rounded-md border border-border-default text-text-secondary hover:bg-surface-elevated disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
            >
              {{ $t('activity.prev') }}
            </button>
            <button
              :disabled="page >= totalPages"
              @click="page++"
              class="px-2.5 py-1 rounded-md border border-border-default text-text-secondary hover:bg-surface-elevated disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
            >
              {{ $t('activity.next') }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <QueueContextMenu
      ref="contextMenu"
      @priority="handleSetPriority"
      @force-start="handleForceStart"
      @delete="handleDelete"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { usePriorityQueue } from '../composables/usePriorityQueue'
import {
  getHistory, getHistoryCategories, deleteHistoryItem, retryDownload,
  moveDownload, setDownloadPriority, forceStartDownload, deleteQueueItem,
  pauseDownloads, resumeDownloads,
  type QueueItem, type HistoryResponse,
} from '../api/downloads'
import ActiveDownloadCard from '../components/ActiveDownloadCard.vue'
import PrioritySection from '../components/PrioritySection.vue'
import QueueContextMenu from '../components/QueueContextMenu.vue'
import EmptyState from '../components/EmptyState.vue'
import SkeletonTable from '../components/SkeletonTable.vue'
import ReleaseTitle from '../components/ReleaseTitle.vue'
import { useToast } from '../composables/useToast'
import { formatSpeed, formatSize, formatDuration, formatRelativeDate, formatAbsoluteDate } from '../utils/format'
import { parseReleaseName } from '../utils/releaseTitle'
import { HistoryStatus } from '../api/enums'

const { t } = useI18n()
const { toast } = useToast()
const {
  active, high, normal, low, queued, totalSpeed, overallProgress,
  isPaused: pipelineIsPaused, isScheduleActive: pipelineScheduleActive,
  nextWindow: pipelineNextWindow, activeCount, queuedCount,
  isDragging, flushBuffer, release,
} = usePriorityQueue()

const activeTab = ref<'queue' | 'history'>('queue')
const searchQuery = ref('')
const isDragActive = ref(false)
const contextMenu = ref<InstanceType<typeof QueueContextMenu> | null>(null)

const tabs = computed(() => [
  { id: 'queue' as const, label: t('activity.queueTab'), count: activeCount.value + queuedCount.value },
  { id: 'history' as const, label: t('activity.history'), count: 0 },
])

function openContextMenu(event: MouseEvent, item: QueueItem) {
  contextMenu.value?.open(event, item)
}

function onDragStart() {
  isDragActive.value = true
  isDragging.value = true
}

function onDragEnd() {
  isDragActive.value = false
  isDragging.value = false
  flushBuffer()
}

async function handleReorder(item: QueueItem, newIndex: number) {
  try {
    await moveDownload(item.downloadId, newIndex)
  } catch {
    toast(t('queue.moveFailed'), 'error')
  }
}

async function handlePriorityChange(item: QueueItem, newIndex: number, newPriority: string) {
  try {
    await moveDownload(item.downloadId, newIndex, newPriority)
  } catch {
    toast(t('queue.moveFailed'), 'error')
  }
}

async function handleSetPriority(id: string, priority: string) {
  try {
    await setDownloadPriority(id, priority)
    toast(t('queue.priorityChanged'))
  } catch {
    toast(t('queue.priorityFailed'), 'error')
  }
}

async function handleForceStart(id: string) {
  try {
    await forceStartDownload(id)
    toast(t('queue.forceStarted'))
  } catch {
    toast(t('queue.forceStartFailed'), 'error')
  }
}

async function handleDelete(id: string) {
  try {
    await deleteQueueItem(id)
    toast(t('activity.downloadCancelled'))
  } catch {
    toast(t('activity.failedToCancel'), 'error')
  }
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

// History state
const history = ref<HistoryResponse | null>(null)
const historyLoading = ref(false)
const historyError = ref<string | null>(null)
const selectedCategory = ref('')
const categories = ref<string[]>([])
const pageSize = 25
const page = ref(1)
const rangeStart = computed(() => (page.value - 1) * pageSize)
const totalPages = computed(() => history.value ? Math.ceil(history.value.totalItems / pageSize) : 1)

const filteredHistoryItems = computed(() => {
  if (!history.value || !searchQuery.value) return history.value?.items ?? []
  const q = searchQuery.value.toLowerCase()
  return history.value.items.filter(i => i.title.toLowerCase().includes(q))
})

function getQuality(title: string): string | null {
  return parseReleaseName(title).quality
}

async function fetchHistory() {
  historyLoading.value = true
  try {
    history.value = await getHistory(rangeStart.value, pageSize, selectedCategory.value || undefined)
    historyError.value = null
  } catch (e) {
    historyError.value = e instanceof Error ? e.message : 'Failed to load history'
  } finally {
    historyLoading.value = false
  }
}

async function fetchCategories() {
  try {
    categories.value = await getHistoryCategories()
  } catch { /* ignore */ }
}

async function handleDeleteHistory(id: string) {
  try {
    await deleteHistoryItem(id)
    toast(t('activity.entryDeleted'))
    await fetchHistory()
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

watch(activeTab, (tab) => {
  if (tab === 'history' && !history.value) {
    fetchHistory()
    fetchCategories()
  }
})

watch(page, () => fetchHistory())

watch(selectedCategory, () => {
  page.value = 1
  fetchHistory()
})

onMounted(() => {
  if (activeTab.value === 'history') {
    fetchHistory()
    fetchCategories()
  }
})

onUnmounted(release)
</script>
