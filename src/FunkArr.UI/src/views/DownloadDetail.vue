<template>
  <div>
    <div class="flex items-center gap-2 mb-4">
      <router-link to="/activity" class="text-text-secondary hover:text-text-primary transition-colors">
        <svg class="w-5 h-5" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
          <path d="M10 4l-4 4 4 4" />
        </svg>
      </router-link>
      <h1 class="text-lg font-semibold text-text-primary truncate">{{ displayItem?.title ?? $t('downloadDetail.loading') }}</h1>
    </div>

    <div v-if="loading" class="text-text-secondary text-sm">{{ $t('downloadDetail.loading') }}</div>
    <div v-else-if="!displayItem" class="text-text-secondary text-sm">{{ $t('downloadDetail.notFound') }}</div>

    <div v-else class="space-y-5">
      <div class="bg-surface-raised border border-border-default rounded-lg p-4 space-y-3">
        <div class="flex items-center gap-2 flex-wrap">
          <span class="px-2 py-0.5 rounded text-xs font-medium" :class="statusClass">{{ statusLabel }}</span>
          <span v-if="displayChannel" class="px-2 py-0.5 rounded text-xs bg-surface-elevated text-text-body">{{ displayChannel }}</span>
          <span v-if="displayHasSubtitles" class="px-2 py-0.5 rounded text-xs bg-accent-dim/20 text-accent">SUB</span>
        </div>

        <!-- Progress bar (active only) -->
        <div v-if="isActive" class="space-y-2">
          <div class="w-full bg-surface-elevated rounded-full h-2">
            <div class="bg-accent h-2 rounded-full transition-all" :style="{ width: `${queueItem?.percentage ?? 0}%` }" />
          </div>
          <div class="flex justify-between text-xs text-text-secondary tabular-nums">
            <span>{{ formatSize(queueItem?.bytesDownloaded ?? 0) }} / {{ formatSize(queueItem?.totalBytes ?? 0) }}</span>
            <span v-if="queueItem?.speed">{{ formatSpeed(queueItem.speed) }}</span>
            <span v-if="queueItem?.eta">{{ queueItem.eta }}</span>
          </div>
        </div>

        <!-- Phase stepper (always shown) -->
        <div class="col-span-2">
          <dt class="text-text-secondary text-sm mb-2">{{ $t('downloadDetail.phase') }}</dt>
          <dd>
            <div class="flex items-center gap-1">
              <template v-for="(step, idx) in phaseLabels" :key="step.key">
                <div class="flex items-center gap-1.5">
                  <div class="flex items-center justify-center w-5 h-5 rounded-full text-[10px] font-bold shrink-0" :class="stepClass(step.key)">
                    <svg v-if="isStepDone(step.key)" class="w-3 h-3" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M3 8.5l3.5 3.5 6.5-7" /></svg>
                    <span v-else-if="isStepFailed(step.key)" class="text-status-fail">!</span>
                    <span v-else>{{ idx + 1 }}</span>
                  </div>
                  <span class="text-xs whitespace-nowrap" :class="stepLabelClass(step.key)">{{ step.label }}</span>
                </div>
                <div v-if="idx < phaseLabels.length - 1" class="w-6 h-px shrink-0" :class="isStepDone(step.key) ? 'bg-green-500/50' : 'bg-border-default'" />
              </template>
            </div>
          </dd>
        </div>

        <dl class="grid grid-cols-2 gap-x-6 gap-y-2 text-sm">
          <div>
            <dt class="text-text-secondary">{{ $t('downloadDetail.category') }}</dt>
            <dd class="text-text-primary">{{ displayItem.category || '-' }}</dd>
          </div>
          <div>
            <dt class="text-text-secondary">{{ $t('downloadDetail.size') }}</dt>
            <dd class="text-text-primary">{{ formatSize(displayItem.totalBytes) }}</dd>
          </div>
          <div v-if="isActive">
            <dt class="text-text-secondary">{{ $t('downloadDetail.priority') }}</dt>
            <dd class="text-text-primary">{{ priorityLabel }}</dd>
          </div>
          <div v-if="historyItem?.filePath">
            <dt class="text-text-secondary">{{ $t('downloadDetail.filePath') }}</dt>
            <dd class="text-text-primary text-xs break-all">{{ historyItem.filePath }}</dd>
          </div>
          <div v-if="historyItem?.downloadTimeSeconds">
            <dt class="text-text-secondary">{{ $t('downloadDetail.duration') }}</dt>
            <dd class="text-text-primary">{{ formatDuration(historyItem.downloadTimeSeconds) }}</dd>
          </div>
          <div v-if="historyItem?.completedAt">
            <dt class="text-text-secondary">{{ $t('downloadDetail.completedAt') }}</dt>
            <dd class="text-text-primary">{{ new Date(historyItem.completedAt).toLocaleString() }}</dd>
          </div>
        </dl>

        <div v-if="historyItem?.failMessage" class="mt-3 p-3 bg-red-500/10 border border-red-500/30 rounded text-sm text-red-400">
          {{ historyItem.failMessage }}
        </div>
      </div>

      <div class="flex gap-2">
        <button v-if="isActive" @click="handleForceStart" class="px-3 py-1.5 text-sm bg-accent text-white rounded hover:bg-accent/80 transition-colors">
          {{ $t('downloadDetail.forceStart') }}
        </button>
        <button v-if="historyItem?.status === HistoryStatus.Failed" @click="handleRetry" class="px-3 py-1.5 text-sm bg-accent text-white rounded hover:bg-accent/80 transition-colors">
          {{ $t('downloadDetail.retry') }}
        </button>
        <button @click="handleDelete" class="px-3 py-1.5 text-sm bg-surface-elevated text-text-body rounded hover:bg-red-500/20 hover:text-red-400 transition-colors">
          {{ $t('downloadDetail.delete') }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useQueueStream } from '../composables/useQueueStream'
import { useToast } from '../composables/useToast'
import { getHistoryItem, deleteQueueItem, deleteHistoryItem, retryDownload, forceStartDownload } from '../api/downloads'
import type { HistoryItem } from '../api/downloads'
import { QueueStatus, QueuePhase, QueuePriority, HistoryStatus } from '../api/enums'
import { formatSpeed, formatSize, formatDuration } from '../utils/format'

const { t } = useI18n()
const { toast } = useToast()
const route = useRoute()
const router = useRouter()
const id = computed(() => route.params.id as string)

const { items: queueItems, release } = useQueueStream()
onUnmounted(release)

const historyItem = ref<HistoryItem | null>(null)
const loading = ref(true)
const wasActive = ref(false)

const queueItem = computed(() =>
  queueItems.value.find(i => i.downloadId === id.value) ?? null
)

const isActive = computed(() => queueItem.value !== null)

const displayItem = computed(() => {
  if (queueItem.value) return {
    title: queueItem.value.title,
    category: queueItem.value.category,
    totalBytes: queueItem.value.totalBytes,
  }
  if (historyItem.value) return {
    title: historyItem.value.title,
    category: historyItem.value.category,
    totalBytes: historyItem.value.totalBytes,
  }
  return null
})

const displayChannel = computed(() => queueItem.value?.channel ?? '')
const displayHasSubtitles = computed(() => queueItem.value?.hasSubtitles ?? false)

const isCompleted = computed(() => historyItem.value?.status === HistoryStatus.Completed)
const isFailed = computed(() => historyItem.value?.status === HistoryStatus.Failed)

const statusLabel = computed(() => {
  if (isActive.value) {
    const p = queueItem.value?.phase ?? -1
    if (p === QueuePhase.VideoDownload) return 'Downloading'
    if (p === QueuePhase.Remuxing) return 'Remuxing'
    if (p === QueuePhase.Moving) return 'Moving'
    return queueItem.value?.status === QueueStatus.Queued ? 'Queued' : 'Processing'
  }
  if (isCompleted.value) return 'Completed'
  if (isFailed.value) return 'Failed'
  return 'Unknown'
})

const statusClass = computed(() => {
  if (isActive.value) return 'bg-accent-dim/20 text-accent'
  if (isCompleted.value) return 'bg-green-500/20 text-green-400'
  if (isFailed.value) return 'bg-red-500/20 text-red-400'
  return 'bg-surface-elevated text-text-secondary'
})

const priorityLabel = computed(() => {
  const p = queueItem.value?.priority ?? 0
  if (p === QueuePriority.High) return t('queue.priorityHigh')
  if (p === QueuePriority.Low) return t('queue.priorityLow')
  return t('queue.priorityNormal')
})

const phaseSteps = [
  { value: 0, i18nKey: 'videodownload' },
  { value: 1, i18nKey: 'remuxing' },
  { value: 2, i18nKey: 'moving' },
] as const

const phaseLabels = computed(() => phaseSteps.map(s => ({
  key: s.value,
  label: t(`downloadDetail.phases.${s.i18nKey}`),
})))

function isStepDone(key: number): boolean {
  if (isCompleted.value) return true
  return isActive.value && key < (queueItem.value?.phase ?? -1)
}

function isStepFailed(key: number): boolean {
  return isFailed.value && key === 0
}

function isStepCurrent(key: number): boolean {
  return isActive.value && key === (queueItem.value?.phase ?? -1)
}

function stepClass(key: number): string {
  if (isStepDone(key)) return 'bg-green-500/20 text-green-400'
  if (isStepFailed(key)) return 'bg-red-500/20 text-red-400'
  if (isStepCurrent(key)) return 'bg-accent text-white'
  return 'bg-surface-elevated text-text-secondary'
}

function stepLabelClass(key: number): string {
  if (isStepDone(key)) return 'text-green-400'
  if (isStepFailed(key)) return 'text-red-400'
  if (isStepCurrent(key)) return 'text-accent font-medium'
  return 'text-text-secondary'
}

async function fetchHistoryById() {
  const item = await getHistoryItem(id.value)
  if (item) {
    historyItem.value = item
    return true
  }
  return false
}

watch(queueItem, async (newVal, oldVal) => {
  if (oldVal && !newVal && wasActive.value) {
    try {
      let found = await fetchHistoryById()
      if (!found) {
        await new Promise(r => setTimeout(r, 1500))
        found = await fetchHistoryById()
      }
      if (found) {
        if (historyItem.value?.status === HistoryStatus.Completed) {
          toast(t('activity.downloadCompleted'), 'success')
        } else if (historyItem.value?.status === HistoryStatus.Failed) {
          toast(t('activity.downloadFailed'), 'error')
        }
      }
    } catch { /* network error during transition — item will appear on next navigation */ }
  }
})

watch(isActive, (val) => {
  if (val) wasActive.value = true
})

onMounted(async () => {
  if (queueItem.value) {
    wasActive.value = true
  }
  try {
    await fetchHistoryById()
  } catch { /* ignore — item may only exist in queue */ }
  loading.value = false
})

async function handleDelete() {
  try {
    if (isActive.value) {
      await deleteQueueItem(id.value)
    } else {
      await deleteHistoryItem(id.value)
    }
    router.push('/activity')
  } catch {
    toast(t('activity.failedToDelete'), 'error')
  }
}

async function handleRetry() {
  try {
    await retryDownload(id.value)
    router.push('/activity')
  } catch {
    toast(t('activity.failedToRetry'), 'error')
  }
}

async function handleForceStart() {
  try {
    await forceStartDownload(id.value)
  } catch {
    toast(t('queue.forceStartFailed'), 'error')
  }
}
</script>
