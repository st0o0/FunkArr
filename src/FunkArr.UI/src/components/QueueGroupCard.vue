<template>
  <div class="rounded-xl border border-border-default overflow-hidden">
    <button
      @click="expanded = !expanded"
      class="w-full flex items-center justify-between px-4 py-3 bg-surface-raised hover:bg-surface-elevated transition-colors text-left"
    >
      <div class="flex items-center gap-3 min-w-0">
        <svg
          class="w-3.5 h-3.5 text-text-muted shrink-0 transition-transform duration-200"
          :class="expanded ? 'rotate-90' : ''"
          viewBox="0 0 16 16" fill="currentColor"
        >
          <path d="M6 3l5 5-5 5V3z"/>
        </svg>
        <span class="text-sm font-semibold text-text-primary truncate">{{ group.series }}</span>
      </div>
      <div class="flex items-center gap-3 shrink-0 text-xs">
        <span v-if="group.activeCount > 0" class="text-brand-400 font-medium">
          {{ group.activeCount }} downloading
        </span>
        <span v-if="group.queuedCount > 0" class="text-text-muted">
          {{ group.queuedCount }} queued
        </span>
      </div>
    </button>

    <div v-if="expanded" class="border-t border-border-subtle">
      <div class="space-y-0">
        <div
          v-for="item in visibleItems"
          :key="item.downloadId"
          class="px-4 py-2.5 border-b border-border-subtle last:border-b-0"
        >
          <QueueCard :item="item" @cancel="$emit('cancel', $event)" />
        </div>
      </div>
      <button
        v-if="hasMore"
        @click="showAll = !showAll"
        class="w-full px-4 py-2 text-xs text-brand-400 hover:text-brand-300 hover:bg-surface-elevated transition-colors"
      >
        {{ showAll ? 'Show less' : `+${group.items.length - collapsedLimit} more` }}
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import type { QueueGroup } from '../composables/useGroupedQueue'
import QueueCard from './QueueCard.vue'

const props = defineProps<{
  group: QueueGroup
  defaultExpanded?: boolean
}>()

defineEmits<{ cancel: [id: string] }>()

const collapsedLimit = 5
const expanded = ref(props.defaultExpanded ?? props.group.activeCount > 0)
const showAll = ref(false)

const hasMore = computed(() => props.group.items.length > collapsedLimit)
const visibleItems = computed(() => {
  if (showAll || !hasMore.value) return props.group.items
  return props.group.items.slice(0, collapsedLimit)
})
</script>
