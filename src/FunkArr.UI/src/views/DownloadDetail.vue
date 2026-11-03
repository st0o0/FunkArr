<template>
  <div>
    <div class="flex items-center gap-2 mb-4">
      <router-link to="/activity" class="text-text-secondary hover:text-text-primary transition-colors">
        <svg class="w-5 h-5" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
          <path d="M10 4l-4 4 4 4" />
        </svg>
      </router-link>
      <h1 class="text-lg font-semibold text-text-primary truncate">{{ item?.title ?? $t('downloadDetail.loading') }}</h1>
    </div>

    <div v-if="!item" class="text-text-secondary text-sm">{{ $t('downloadDetail.notFound') }}</div>

    <div v-else class="space-y-5">
      <div class="bg-surface-raised border border-border-default rounded-lg p-4 space-y-3">
        <div class="flex items-center gap-2 flex-wrap">
          <span class="px-2 py-0.5 rounded text-xs font-medium" :class="statusClass">{{ statusLabel }}</span>
          <span v-if="item.channel" class="px-2 py-0.5 rounded text-xs bg-surface-elevated text-text-body">{{ item.channel }}</span>
          <span v-if="item.hasSubtitles" class="px-2 py-0.5 rounded text-xs bg-accent-dim/20 text-accent">SUB</span>
        </div>

        <div v-if="isActive" class="space-y-2">
          <div class="w-full bg-surface-elevated rounded-full h-2">
            <div class="bg-accent h-2 rounded-full transition-all" :style="{ width: `${item.percentage ?? 0}%` }" />
          </div>
          <div class="flex justify-between text-xs text-text-secondary tabular-nums">
            <span>{{ formatBytes(item.bytesDownloaded) }} / {{ formatBytes(item.totalBytes) }}</span>
            <span v-if="item.speed">{{ formatSpeed(item.speed) }}</span>
            <span v-if="item.eta">{{ item.eta }}</span>
          </div>
        </div>

        <dl class="grid grid-cols-2 gap-x-6 gap-y-2 text-sm">
          <div>
            <dt class="text-text-secondary">{{ $t('downloadDetail.category') }}</dt>
            <dd class="text-text-primary">{{ item.category || '-' }}</dd>
          </div>
          <div>
            <dt class="text-text-secondary">{{ $t('downloadDetail.size') }}</dt>
            <dd class="text-text-primary">{{ formatBytes(item.totalBytes) }}</dd>
          </div>
          <div v-if="isActive && item.phase" class="col-span-2">
            <dt class="text-text-secondary mb-2">{{ $t('downloadDetail.phase') }}</dt>
            <dd>
              <div class="flex items-center gap-1">
                <template v-for="(step, idx) in phaseLabels" :key="step.key">
                  <div class="flex items-center gap-1.5">
                    <div class="flex items-center justify-center w-5 h-5 rounded-full text-[10px] font-bold shrink-0"
                      :class="stepClass(step.key)">
                      <svg v-if="isStepDone(step.key)" class="w-3 h-3" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M3 8.5l3.5 3.5 6.5-7" /></svg>
                      <span v-else>{{ idx + 1 }}</span>
                    </div>
                    <span class="text-xs whitespace-nowrap" :class="stepLabelClass(step.key)">{{ step.label }}</span>
                  </div>
                  <div v-if="idx < phaseLabels.length - 1" class="w-6 h-px shrink-0" :class="isStepDone(step.key) ? 'bg-green-500/50' : 'bg-border-default'" />
                </template>
              </div>
            </dd>
          </div>
          <div v-if="item.priority">
            <dt class="text-text-secondary">{{ $t('downloadDetail.priority') }}</dt>
            <dd class="text-text-primary">{{ item.priority }}</dd>
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
        <button v-if="historyItem?.status === 3" @click="handleRetry" class="px-3 py-1.5 text-sm bg-accent text-white rounded hover:bg-accent/80 transition-colors">
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
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useQueueStream } from '../composables/useQueueStream'
import { getHistory, deleteQueueItem, deleteHistoryItem, retryDownload, forceStartDownload } from '../api/downloads'
import type { QueueItem, HistoryItem } from '../api/downloads'

const { t } = useI18n()

const route = useRoute()
const router = useRouter()
const id = computed(() => route.params.id as string)

const { items: queueItems, release } = useQueueStream()
onUnmounted(release)

const historyItem = ref<HistoryItem | null>(null)

const queueItem = computed(() =>
  queueItems.value.find(i => i.downloadId === id.value) ?? null
)

const item = computed(() => {
  if (queueItem.value) return queueItem.value
  if (historyItem.value) return {
    downloadId: historyItem.value.downloadId,
    title: historyItem.value.title,
    status: historyItem.value.status,
    channel: '',
    category: historyItem.value.category,
    hasSubtitles: false,
    totalDuration: 0,
    totalBytes: historyItem.value.totalBytes,
    bytesDownloaded: historyItem.value.totalBytes,
    percentage: 100,
    phase: -1,
    speed: 0,
    eta: '',
    priority: 0,
  } as QueueItem
  return null
})

const isActive = computed(() => queueItem.value !== null)

const phaseNames = ['Download', 'Remux', 'Move'] as const
const statusLabel = computed(() => {
  if (isActive.value) {
    const p = queueItem.value?.phase ?? -1
    return p >= 0 && p < phaseNames.length ? phaseNames[p] : 'Queued'
  }
  if (historyItem.value?.status === 2) return 'Completed'
  if (historyItem.value?.status === 3) return 'Failed'
  return 'Unknown'
})

const statusClass = computed(() => {
  if (isActive.value) return 'bg-accent-dim/20 text-accent'
  if (historyItem.value?.status === 2) return 'bg-green-500/20 text-green-400'
  if (historyItem.value?.status === 3) return 'bg-red-500/20 text-red-400'
  return 'bg-surface-elevated text-text-secondary'
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

const currentPhase = computed(() => item.value?.phase ?? -1)

function isStepDone(key: number): boolean {
  return key < currentPhase.value
}

function isStepCurrent(key: number): boolean {
  return key === currentPhase.value
}

function stepClass(key: number): string {
  if (isStepDone(key)) return 'bg-green-500/20 text-green-400'
  if (isStepCurrent(key)) return 'bg-accent text-white'
  return 'bg-surface-elevated text-text-secondary'
}

function stepLabelClass(key: number): string {
  if (isStepDone(key)) return 'text-green-400'
  if (isStepCurrent(key)) return 'text-accent font-medium'
  return 'text-text-secondary'
}

onMounted(async () => {
  try {
    const res = await getHistory(0, 100)
    historyItem.value = res.items.find(i => i.downloadId === id.value) ?? null
  } catch { /* ignore */ }
})

function formatBytes(bytes: number): string {
  if (bytes === 0) return '0 B'
  const units = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(1024))
  return `${(bytes / Math.pow(1024, i)).toFixed(i > 1 ? 1 : 0)} ${units[i]}`
}

function formatSpeed(speed: number): string {
  return `${formatBytes(speed)}/s`
}

function formatDuration(seconds: number): string {
  const m = Math.floor(seconds / 60)
  const s = seconds % 60
  return `${m}m ${s}s`
}

async function handleDelete() {
  if (isActive.value) {
    await deleteQueueItem(id.value)
  } else {
    await deleteHistoryItem(id.value)
  }
  router.push('/activity')
}

async function handleRetry() {
  await retryDownload(id.value)
  router.push('/activity')
}

async function handleForceStart() {
  await forceStartDownload(id.value)
}
</script>
