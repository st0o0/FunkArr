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
              <path d="M6 15h4" />
              <path d="M8 12v3" />
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
              <circle cx="8" cy="8" r="6" />
              <path d="M8 5v3l2 2" />
            </svg>
            {{ formatDuration(item.totalDuration) }}
          </span>
          <span
            v-if="item.hasSubtitles"
            class="inline-flex items-center gap-1 text-[11px] px-1.5 py-0.5 rounded bg-accent/15 text-accent font-medium"
          >SUB</span>
        </div>
      </div>
      <div class="flex items-center gap-2 shrink-0">
        <span
          class="inline-flex items-center gap-1.5 text-xs px-2 py-0.5 rounded-md"
          :class="item.status === QueueStatus.Processing
            ? 'bg-surface-elevated text-text-body'
            : 'bg-surface-elevated text-text-secondary'"
        >
          <span
            class="w-1.5 h-1.5 rounded-full"
            :class="item.status === QueueStatus.Processing ? 'bg-accent' : 'bg-text-muted'"
          />
          {{ item.status === QueueStatus.Processing ? $t('queue.downloading') : $t('queue.queuedStatus') }}
        </span>
        <button
          @click="$emit('cancel', item.downloadId)"
          class="p-1 text-text-secondary hover:text-status-fail transition-colors rounded"
          :title="$t('queue.cancel')"
        >
          <svg class="w-3.5 h-3.5" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round">
            <path d="M4 4l8 8M12 4l-8 8" />
          </svg>
        </button>
      </div>
    </div>

    <template v-if="item.status === QueueStatus.Processing">
      <div class="mt-3">
        <div class="h-1.5 bg-surface-elevated rounded-full overflow-hidden">
          <div
            class="h-full bg-accent rounded-full transition-all duration-700 ease-out"
            :style="{ width: `${item.percentage}%` }"
          />
        </div>
        <div class="flex justify-between text-xs text-text-secondary mt-1.5">
          <span class="tabular-nums">{{ item.percentage }}% · {{ formatSize(item.bytesDownloaded) }} / {{ formatSize(item.totalBytes) }}</span>
          <span class="tabular-nums">{{ formatSpeed(item.speed) }} · ETA {{ item.eta }}</span>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import type { QueueItem } from '../api/downloads'
import { QueueStatus } from '../api/enums'
import { formatSize, formatSpeed, formatDuration } from '../utils/format'
import ReleaseTitle from './ReleaseTitle.vue'

defineProps<{ item: QueueItem }>()
defineEmits<{ cancel: [id: string] }>()
</script>
