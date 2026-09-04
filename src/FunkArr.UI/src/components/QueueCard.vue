<template>
  <div class="bg-surface-raised rounded-lg p-4 hover:-translate-y-px hover:shadow-md transition-all">
    <div class="flex items-start justify-between gap-3">
      <div class="min-w-0 flex-1">
        <h3 class="text-sm font-semibold text-text-primary truncate">{{ item.title }}</h3>
        <div class="flex items-center gap-2 mt-1 text-xs text-text-secondary">
          <span>{{ item.category }}</span>
          <span class="text-text-muted">&middot;</span>
          <span>{{ formatSize(item.totalBytes) }}</span>
        </div>
      </div>
      <div class="flex items-center gap-2 shrink-0">
        <span
          class="inline-flex items-center gap-1.5 text-xs font-medium px-2.5 py-1 rounded-full"
          :class="item.status === 'Processing'
            ? 'bg-brand-900/30 text-brand-400'
            : 'bg-surface-elevated text-text-secondary'"
        >
          <span
            class="w-1.5 h-1.5 rounded-full"
            :class="item.status === 'Processing' ? 'bg-brand-500 animate-pulse' : 'bg-text-muted'"
          />
          {{ item.status === 'Processing' ? 'Downloading' : 'Queued' }}
        </span>
        <button
          @click="$emit('cancel', item.downloadId)"
          class="p-1.5 text-status-fail/60 hover:text-status-fail hover:bg-status-fail/10 transition-colors rounded-md"
          title="Cancel"
        >
          <svg class="w-3.5 h-3.5" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round">
            <path d="M4 4l8 8M12 4l-8 8" />
          </svg>
        </button>
      </div>
    </div>

    <template v-if="item.status === 'Processing'">
      <div class="mt-3">
        <div class="flex justify-between text-xs text-text-secondary mb-1.5">
          <span class="tabular-nums">{{ item.percentage }}%</span>
          <span class="tabular-nums">{{ formatSpeed(item.speed) }}</span>
        </div>
        <div class="h-1.5 bg-surface-elevated rounded-full overflow-hidden">
          <div
            class="h-full bg-brand-500 rounded-full transition-all duration-700 ease-out"
            :style="{ width: `${item.percentage}%` }"
          />
        </div>
        <div class="flex justify-between text-xs text-text-muted mt-1.5">
          <span class="tabular-nums">{{ formatSize(item.bytesDownloaded) }} / {{ formatSize(item.totalBytes) }}</span>
          <span>ETA {{ item.eta }}</span>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import type { QueueItem } from '../api/downloads'
import { formatSize, formatSpeed } from '../utils/format'

defineProps<{ item: QueueItem }>()
defineEmits<{ cancel: [id: string] }>()
</script>
