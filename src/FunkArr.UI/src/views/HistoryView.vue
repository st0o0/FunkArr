<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { api, API_BASE } from '@/api/client'
import DownloadCard from '@/components/DownloadCard.vue'

interface HistoryItem {
  nzoId: string
  title: string
  status: string
  outputPath: string | null
  errorMessage: string | null
  enqueuedAt: string
  completedAt: string | null
}

const history = ref<HistoryItem[]>([])
const loading = ref(true)
const error = ref<string | null>(null)

onMounted(async () => {
  try {
    history.value = await api<HistoryItem[]>(`${API_BASE}/history`)
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e)
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div>
    <p v-if="loading" class="text-center text-neutral-500 py-12">Loading...</p>

    <p v-else-if="error" class="text-center text-red-600 dark:text-red-400 py-12">{{ error }}</p>

    <p v-else-if="history.length === 0" class="text-center text-neutral-500 py-12">
      No download history
    </p>

    <div v-else class="flex flex-col gap-3">
      <DownloadCard
        v-for="item in history"
        :key="item.nzoId"
        :title="item.title"
        :status="item.status"
        :error-message="item.errorMessage"
        :completed-at="item.completedAt"
      />
    </div>
  </div>
</template>
