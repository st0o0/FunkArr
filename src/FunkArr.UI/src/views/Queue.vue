<template>
  <div>
    <h1 class="text-2xl font-bold text-text-primary tracking-tight mb-6">Downloads</h1>

    <div v-if="items.length === 0" class="bg-surface-raised rounded-xl border border-border-default p-10 text-center">
      <p class="text-text-secondary text-sm">No active or queued downloads.</p>
    </div>

    <div v-else class="space-y-3">
      <QueueCard
        v-for="item in items"
        :key="item.downloadId"
        :item="item"
        @cancel="handleCancel"
      />

      <div class="text-xs text-text-muted pt-2 tabular-nums">
        {{ items.length }} {{ items.length === 1 ? 'item' : 'items' }}
        &middot; {{ queuedCount }} queued
        &middot; {{ activeCount }} downloading
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onUnmounted } from 'vue'
import QueueCard from '../components/QueueCard.vue'
import { useQueueStream } from '../composables/useQueueStream'
import { deleteQueueItem } from '../api/downloads'

const { items, release } = useQueueStream()

const activeCount = computed(() => items.value.filter(i => i.status === 'Processing').length)
const queuedCount = computed(() => items.value.filter(i => i.status === 'Queued').length)

async function handleCancel(id: string) {
  await deleteQueueItem(id)
}

onUnmounted(release)
</script>
