<template>
  <div class="bg-white rounded border border-gray-200 p-4">
    <div class="flex items-center justify-between mb-3">
      <h2 class="text-lg font-semibold">System Health</h2>
      <router-link to="/setup" class="text-sm text-blue-600 hover:text-blue-800">Setup Guide</router-link>
    </div>

    <div v-if="loading && !health" class="text-gray-500 text-sm">Checking...</div>
    <div v-else-if="error" class="text-red-600 text-sm">{{ error }}</div>

    <div v-else-if="health" class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-3">
      <div
        v-for="(result, name) in health.checks"
        :key="name"
        class="flex items-center gap-2 text-sm"
      >
        <span
          class="w-2.5 h-2.5 rounded-full flex-shrink-0"
          :class="{
            'bg-green-500': result.status === 'ok',
            'bg-yellow-500': result.status === 'warn',
            'bg-red-500': result.status === 'fail',
          }"
        />
        <span class="text-gray-700">{{ labels[name] ?? name }}</span>
      </div>
    </div>

    <div
      v-if="health && failedChecks.length > 0"
      class="mt-3 pt-3 border-t border-gray-100 space-y-1"
    >
      <div v-for="check in failedChecks" :key="check.name" class="text-xs text-red-600">
        {{ labels[check.name] ?? check.name }}: {{ check.result.message }}
      </div>
    </div>

    <div
      v-if="health && warnChecks.length > 0"
      class="mt-3 pt-3 border-t border-gray-100 space-y-1"
    >
      <div v-for="check in warnChecks" :key="check.name" class="text-xs text-yellow-600">
        {{ labels[check.name] ?? check.name }}: {{ check.result.message }}
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { getSetupHealth, type SetupHealthCheck, type CheckResult } from '../api/setup'

const health = ref<SetupHealthCheck | null>(null)
const loading = ref(true)
const error = ref<string | null>(null)
let interval: ReturnType<typeof setInterval> | null = null

const labels: Record<string, string> = {
  apiKey: 'API Key',
  mediathekViewWeb: 'MediathekViewWeb',
  dataDirectory: 'Data Directory',
  downloadDirectory: 'Download Directory',
  indexerApi: 'Indexer API',
  downloadApi: 'Download API',
  ffmpeg: 'FFmpeg',
}

interface NamedCheck {
  name: string
  result: CheckResult
}

const failedChecks = computed<NamedCheck[]>(() => {
  if (!health.value) return []
  return Object.entries(health.value.checks)
    .filter(([, r]) => r.status === 'fail')
    .map(([name, result]) => ({ name, result }))
})

const warnChecks = computed<NamedCheck[]>(() => {
  if (!health.value) return []
  return Object.entries(health.value.checks)
    .filter(([, r]) => r.status === 'warn')
    .map(([name, result]) => ({ name, result }))
})

async function refresh() {
  loading.value = true
  try {
    health.value = await getSetupHealth()
    error.value = null
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to load health status'
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  refresh()
  interval = setInterval(refresh, 30000)
})

onUnmounted(() => {
  if (interval) clearInterval(interval)
})
</script>
