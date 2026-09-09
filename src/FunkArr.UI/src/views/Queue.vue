<template>
  <div class="max-w-4xl mx-auto">
    <div class="flex items-center justify-between mb-5">
      <h1 class="text-lg font-medium text-text-primary">Downloads</h1>
      <div v-if="groups.length > 0" class="flex items-center gap-3 text-xs">
        <span class="text-text-muted tabular-nums">{{ formatSpeed(totalSpeed) }}</span>
        <button
          @click="toggleAll"
          class="text-text-secondary hover:text-text-body transition-colors"
        >
          {{ allExpanded ? 'Collapse All' : 'Expand All' }}
        </button>
      </div>
    </div>

    <EmptyState
      v-if="items.length === 0"
      icon='<path d="M8 2v8M5 7l3 3 3-3"/><path d="M2 12h12"/>'
      title="No active downloads"
      description="Downloads appear here when Sonarr or Radarr trigger a search."
    />

    <div v-else class="space-y-2">
      <div class="bg-surface-raised rounded-lg border border-border-default px-4 py-2.5 flex items-center gap-4">
        <div class="flex-1">
          <div class="h-1 bg-surface-elevated rounded-full overflow-hidden">
            <div class="h-full bg-brand-500 rounded-full transition-all duration-700" :style="{ width: `${overallProgress}%` }" />
          </div>
        </div>
        <span class="text-xs text-text-muted tabular-nums shrink-0">{{ activeCount }} active &middot; {{ queuedCount }} queued</span>
      </div>

      <template v-for="group in groups" :key="group.series">
        <div v-if="group.items.length === 1" class="rounded-lg border border-border-default overflow-hidden px-4 py-2.5 bg-surface-raised">
          <QueueCard :item="group.items[0]" @cancel="handleCancel" />
        </div>
        <QueueGroupCard
          v-else
          :group="group"
          :default-expanded="allExpanded || undefined"
          @cancel="handleCancel"
        />
      </template>

      <div class="text-xs text-text-muted pt-1 tabular-nums">
        {{ items.length }} {{ items.length === 1 ? 'item' : 'items' }}
        &middot; {{ queuedCount }} queued
        &middot; {{ activeCount }} downloading
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onUnmounted } from 'vue'
import QueueGroupCard from '../components/QueueGroupCard.vue'
import QueueCard from '../components/QueueCard.vue'
import EmptyState from '../components/EmptyState.vue'
import { useGroupedQueue } from '../composables/useGroupedQueue'
import { deleteQueueItem } from '../api/downloads'
import { useToast } from '../composables/useToast'
import { formatSpeed } from '../utils/format'

const { toast } = useToast()
const { groups, items, activeCount, queuedCount, totalSpeed, release } = useGroupedQueue()

const allExpanded = ref(false)

const overallProgress = computed(() => {
  if (items.value.length === 0) return 0
  const total = items.value.reduce((sum, i) => sum + i.percentage, 0)
  return Math.round(total / items.value.length)
})

function toggleAll() {
  allExpanded.value = !allExpanded.value
}

async function handleCancel(id: string) {
  try {
    await deleteQueueItem(id)
    toast('Download cancelled')
  } catch {
    toast('Failed to cancel download', 'error')
  }
}

onUnmounted(release)
</script>
