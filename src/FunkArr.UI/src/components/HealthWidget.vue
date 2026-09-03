<template>
  <div class="bg-surface-raised rounded-xl border border-border-default overflow-hidden">
    <div class="flex items-center justify-between px-5 py-3.5 border-b border-border-subtle">
      <h2 class="text-sm font-semibold text-text-primary">System Health</h2>
      <router-link to="/setup" class="text-xs text-brand-400 hover:text-brand-300 transition-colors">Setup Guide</router-link>
    </div>

    <div class="p-5">
      <div v-if="loading && !health" class="text-text-muted text-sm">Checking...</div>
      <div v-else-if="error" class="text-status-fail text-sm">{{ error }}</div>

      <div v-else-if="health" class="space-y-4">
        <div class="grid grid-cols-2 gap-2.5">
          <div
            v-for="(result, name) in health.checks"
            :key="name"
            class="flex items-center gap-2.5 px-3 py-2 rounded-lg bg-surface-elevated/50"
          >
            <span
              class="w-2 h-2 rounded-full shrink-0"
              :class="{
                'bg-status-ok shadow-[0_0_6px_rgba(72,187,120,0.4)]': result.status === 'ok',
                'bg-status-warn shadow-[0_0_6px_rgba(236,201,75,0.4)]': result.status === 'warn',
                'bg-status-fail shadow-[0_0_6px_rgba(252,129,129,0.4)]': result.status === 'fail',
              }"
            />
            <span class="text-xs text-text-body">{{ labels[name] ?? name }}</span>
          </div>
        </div>

        <div
          v-if="failedChecks.length > 0 || warnChecks.length > 0"
          class="space-y-1.5 pt-1"
        >
          <div v-for="check in failedChecks" :key="check.name" class="flex items-start gap-2 text-xs">
            <span class="text-status-fail shrink-0 mt-px">&#x2717;</span>
            <span class="text-text-secondary"><span class="text-text-body">{{ labels[check.name] ?? check.name }}:</span> {{ check.result.message }}</span>
          </div>
          <div v-for="check in warnChecks" :key="check.name" class="flex items-start gap-2 text-xs">
            <span class="text-status-warn shrink-0 mt-px">&#x26A0;</span>
            <span class="text-text-secondary"><span class="text-text-body">{{ labels[check.name] ?? check.name }}:</span> {{ check.result.message }}</span>
          </div>
        </div>
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
