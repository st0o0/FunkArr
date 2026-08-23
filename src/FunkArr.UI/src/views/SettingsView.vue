<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { api, API_BASE } from '@/api/client'

interface StatusResponse {
  configured: boolean
  apiKey: string
  ffmpeg: { found: boolean; version?: string }
  paths: { downloadOk: boolean; tempOk: boolean }
  mediathek: { reachable: boolean }
}

const loading = ref(true)
const error = ref<string | null>(null)
const status = ref<StatusResponse | null>(null)
const apiKeyCopied = ref(false)

async function loadStatus() {
  loading.value = true
  error.value = null
  try {
    status.value = await api<StatusResponse>(`${API_BASE}/setup/status`)
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e)
  } finally {
    loading.value = false
  }
}

async function copyApiKey() {
  if (!status.value) return
  await navigator.clipboard.writeText(status.value.apiKey)
  apiKeyCopied.value = true
  setTimeout(() => (apiKeyCopied.value = false), 2000)
}

onMounted(loadStatus)
</script>

<template>
  <div>
    <p v-if="loading" class="text-center text-neutral-500 py-12">Loading...</p>
    <p v-else-if="error" class="text-center text-red-600 dark:text-red-400 py-12">{{ error }}</p>

    <div v-else-if="status" class="space-y-8">
      <!-- API Key -->
      <section>
        <p class="text-sm font-medium text-neutral-500 uppercase tracking-wide mb-2">API Key</p>
        <div class="border border-neutral-200 dark:border-neutral-700 rounded p-4">
          <p class="text-xs text-neutral-500 mb-2">Copy this key into Sonarr/Radarr/Prowlarr when adding FunkArr as an indexer or download client.</p>
          <div class="flex items-center gap-3">
            <code class="font-mono text-sm flex-1 truncate">{{ status.apiKey }}</code>
            <button
              class="text-xs px-2 py-1 border border-neutral-300 dark:border-neutral-600 rounded hover:bg-neutral-100 dark:hover:bg-neutral-800"
              @click="copyApiKey"
            >
              {{ apiKeyCopied ? 'Copied' : 'Copy' }}
            </button>
          </div>
        </div>
      </section>

      <!-- System Status -->
      <section>
        <p class="text-sm font-medium text-neutral-500 uppercase tracking-wide mb-2">System Status</p>
        <div class="border border-neutral-200 dark:border-neutral-700 rounded p-4 space-y-2 text-sm">
          <div class="flex items-center gap-2">
            <span class="w-2 h-2 rounded-full shrink-0" :class="status.ffmpeg.found ? 'bg-green-500' : 'bg-red-500'" />
            <span class="text-neutral-500 w-32 shrink-0">FFmpeg</span>
            <span v-if="status.ffmpeg.found" class="font-mono">v{{ status.ffmpeg.version }}</span>
            <span v-else class="text-red-500">Not found</span>
          </div>
          <div class="flex items-center gap-2">
            <span class="w-2 h-2 rounded-full shrink-0" :class="status.paths.downloadOk ? 'bg-green-500' : 'bg-red-500'" />
            <span class="text-neutral-500 w-32 shrink-0">Download Path</span>
            <span class="text-xs">{{ status.paths.downloadOk ? 'Writable' : 'Not writable' }}</span>
          </div>
          <div class="flex items-center gap-2">
            <span class="w-2 h-2 rounded-full shrink-0" :class="status.paths.tempOk ? 'bg-green-500' : 'bg-red-500'" />
            <span class="text-neutral-500 w-32 shrink-0">Temp Path</span>
            <span class="text-xs">{{ status.paths.tempOk ? 'Writable' : 'Not writable' }}</span>
          </div>
          <div class="flex items-center gap-2">
            <span class="w-2 h-2 rounded-full shrink-0" :class="status.mediathek.reachable ? 'bg-green-500' : 'bg-red-500'" />
            <span class="text-neutral-500 w-32 shrink-0">MediathekViewWeb</span>
            <span class="text-xs">{{ status.mediathek.reachable ? 'Reachable' : 'Unreachable' }}</span>
          </div>
        </div>
      </section>

      <!-- Configuration Note -->
      <section>
        <p class="text-sm font-medium text-neutral-500 uppercase tracking-wide mb-2">Configuration</p>
        <div class="border border-neutral-200 dark:border-neutral-700 rounded p-4">
          <p class="text-sm text-neutral-600 dark:text-neutral-400">
            All settings are configured via environment variables in your <code class="font-mono text-xs">docker-compose.yml</code>.
            See the documentation for available options.
          </p>
        </div>
      </section>

      <!-- Setup Guide Link -->
      <div class="pt-4 border-t border-neutral-200 dark:border-neutral-700">
        <router-link
          to="/setup"
          class="text-sm text-neutral-500 hover:text-neutral-700 dark:hover:text-neutral-300"
        >
          Setup Guide
        </router-link>
      </div>
    </div>
  </div>
</template>
