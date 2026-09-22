<template>
  <div>
    <div class="flex items-start justify-between gap-3">
      <div class="min-w-0 flex-1">
        <ReleaseTitle :title="item.title" />
        <div class="flex items-center gap-1.5 mt-1.5 flex-wrap">
          <span
            v-if="item.channel"
            class="inline-flex items-center gap-1 text-[11px] px-1.5 py-0.5 rounded bg-surface-elevated text-text-secondary"
          >
            <svg class="w-3 h-3 shrink-0 opacity-60" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
              <rect x="2" y="3" width="12" height="9" rx="1" />
              <path d="M6 15h4" /><path d="M8 12v3" />
            </svg>
            {{ item.channel }}
          </span>
          <span class="inline-flex items-center gap-1 text-[11px] px-1.5 py-0.5 rounded bg-surface-elevated text-text-secondary tabular-nums">
            {{ formatSize(item.totalBytes) }}
          </span>
          <span v-if="item.hasSubtitles" class="inline-flex items-center gap-1 text-[11px] px-1.5 py-0.5 rounded bg-accent/15 text-accent font-medium">SUB</span>
        </div>
      </div>
      <button
        @click="$emit('menu', $event, item)"
        class="p-1.5 text-text-muted hover:text-text-secondary transition-colors rounded shrink-0"
      >
        <svg class="w-4 h-4" viewBox="0 0 16 16" fill="currentColor">
          <circle cx="8" cy="3" r="1.5" /><circle cx="8" cy="8" r="1.5" /><circle cx="8" cy="13" r="1.5" />
        </svg>
      </button>
    </div>
    <div class="mt-3">
      <div class="h-1.5 bg-surface-elevated rounded-full overflow-hidden">
        <div class="h-full bg-accent rounded-full transition-all duration-700 ease-out" :style="{ width: `${item.percentage}%` }" />
      </div>
      <div class="flex justify-between text-xs text-text-secondary mt-1.5">
        <span class="tabular-nums">{{ item.percentage }}% · {{ formatSize(item.bytesDownloaded) }} / {{ formatSize(item.totalBytes) }}</span>
        <span class="tabular-nums">{{ formatSpeed(item.speed) }} · ETA {{ item.eta }}</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { QueueItem } from '../api/downloads'
import { formatSize, formatSpeed } from '../utils/format'
import ReleaseTitle from './ReleaseTitle.vue'

defineProps<{ item: QueueItem }>()
defineEmits<{ menu: [event: MouseEvent, item: QueueItem] }>()
</script>
