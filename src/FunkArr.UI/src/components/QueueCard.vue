<template>
  <div class="flex items-start gap-3">
    <div v-if="draggable" class="pt-1 cursor-grab active:cursor-grabbing text-text-muted hover:text-text-secondary drag-handle shrink-0">
      <svg class="w-4 h-4" viewBox="0 0 16 16" fill="currentColor">
        <circle cx="5" cy="3" r="1.2" /><circle cx="11" cy="3" r="1.2" />
        <circle cx="5" cy="8" r="1.2" /><circle cx="11" cy="8" r="1.2" />
        <circle cx="5" cy="13" r="1.2" /><circle cx="11" cy="13" r="1.2" />
      </svg>
    </div>
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
          <svg class="w-3 h-3 shrink-0 opacity-60" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
            <path d="M13 2H3a1 1 0 0 0-1 1v10a1 1 0 0 0 1 1h10a1 1 0 0 0 1-1V3a1 1 0 0 0-1-1Z" />
            <path d="M2 6h12" />
          </svg>
          {{ formatSize(item.totalBytes) }}
        </span>
        <span
          v-if="item.totalDuration > 0"
          class="inline-flex items-center gap-1 text-[11px] px-1.5 py-0.5 rounded bg-surface-elevated text-text-secondary tabular-nums"
        >
          <svg class="w-3 h-3 shrink-0 opacity-60" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
            <circle cx="8" cy="8" r="6" /><path d="M8 5v3l2 2" />
          </svg>
          {{ formatDuration(item.totalDuration) }}
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
</template>

<script setup lang="ts">
import type { QueueItem } from '../api/downloads'
import { formatSize, formatDuration } from '../utils/format'
import ReleaseTitle from './ReleaseTitle.vue'

withDefaults(defineProps<{ item: QueueItem; draggable?: boolean }>(), { draggable: false })
defineEmits<{ menu: [event: MouseEvent, item: QueueItem] }>()
</script>
