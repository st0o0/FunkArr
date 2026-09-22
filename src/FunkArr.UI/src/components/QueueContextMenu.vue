<template>
  <Teleport to="body">
    <div v-if="visible" class="fixed inset-0 z-40" @click="close" />
    <div
      v-if="visible"
      class="fixed z-50 bg-surface-overlay border border-border-default rounded-lg shadow-xl py-1 min-w-[180px]"
      :style="{ top: `${position.y}px`, left: `${position.x}px` }"
    >
      <template v-if="!isActive">
        <div class="px-3 py-1.5 text-[11px] text-text-muted uppercase tracking-wider">{{ $t('queue.priority') }}</div>
        <button
          v-for="p in priorities"
          :key="p.value"
          @click="handlePriority(p.value)"
          class="w-full flex items-center gap-2 px-3 py-1.5 text-sm text-text-body hover:bg-surface-elevated transition-colors"
        >
          <svg v-if="currentPriority === p.value" class="w-3.5 h-3.5 text-accent" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M3 8l3 3 7-7" />
          </svg>
          <span v-else class="w-3.5" />
          <span :class="p.color">{{ p.label }}</span>
        </button>
        <div class="h-px bg-border-default my-1" />
        <button
          @click="handleForceStart"
          class="w-full flex items-center gap-2 px-3 py-1.5 text-sm text-text-body hover:bg-surface-elevated transition-colors"
        >
          <svg class="w-3.5 h-3.5" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
            <path d="M4 2l10 6-10 6V2z" />
          </svg>
          {{ $t('queue.forceStart') }}
        </button>
        <div class="h-px bg-border-default my-1" />
      </template>
      <button
        @click="handleDelete"
        class="w-full flex items-center gap-2 px-3 py-1.5 text-sm text-status-fail hover:bg-surface-elevated transition-colors"
      >
        <svg class="w-3.5 h-3.5" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round">
          <path d="M4 4l8 8M12 4l-8 8" />
        </svg>
        {{ $t('queue.delete') }}
      </button>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import type { QueueItem } from '../api/downloads'
import { QueueStatus } from '../api/enums'

const { t } = useI18n()

const visible = ref(false)
const position = ref({ x: 0, y: 0 })
const currentItem = ref<QueueItem | null>(null)

const isActive = computed(() => currentItem.value?.status === QueueStatus.Processing)
const currentPriority = computed(() => currentItem.value?.priority ?? 'Normal')

const priorities = computed(() => [
  { value: 'High', label: t('queue.priorityHigh'), color: 'text-status-warn' },
  { value: 'Normal', label: t('queue.priorityNormal'), color: 'text-text-body' },
  { value: 'Low', label: t('queue.priorityLow'), color: 'text-status-info' },
])

const emit = defineEmits<{
  priority: [id: string, priority: string]
  forceStart: [id: string]
  delete: [id: string]
}>()

function open(event: MouseEvent, item: QueueItem) {
  currentItem.value = item
  const x = Math.min(event.clientX, window.innerWidth - 200)
  const y = Math.min(event.clientY, window.innerHeight - 250)
  position.value = { x, y }
  visible.value = true
}

function close() {
  visible.value = false
  currentItem.value = null
}

function handlePriority(priority: string) {
  if (currentItem.value && priority !== currentPriority.value) {
    emit('priority', currentItem.value.downloadId, priority)
  }
  close()
}

function handleForceStart() {
  if (currentItem.value) emit('forceStart', currentItem.value.downloadId)
  close()
}

function handleDelete() {
  if (currentItem.value) emit('delete', currentItem.value.downloadId)
  close()
}

defineExpose({ open })
</script>
