<script setup lang="ts">
import { ref, onMounted, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { api, apiPut, apiPost, getApiKey, setApiKey, API_BASE } from '@/api/client'

// --- Types ---

interface ArrInstance {
  name: string
  type: string
  url: string
  apiKey: string
}

interface ConfigResponse {
  apiKey: string
  downloadPath: string
  tempPath: string
  concurrentDownloads: number
  pathMapping?: string
  prowlarr?: { url: string; apiKey: string }
  arrInstances: ArrInstance[]
}

interface ArrInstanceStatus {
  name: string
  connected: boolean
  error?: string
}

interface StatusResponse {
  configured: boolean
  ffmpeg: { found: boolean; version?: string }
  paths: { downloadOk: boolean; tempOk: boolean }
  mediathek: { reachable: boolean }
  prowlarr?: { connected: boolean; error?: string }
  arrInstances: ArrInstanceStatus[]
}

// --- State ---

const router = useRouter()
const loading = ref(true)
const error = ref<string | null>(null)
const saving = ref(false)
const saveMessage = ref<string | null>(null)

const config = reactive<ConfigResponse>({
  apiKey: '',
  downloadPath: '',
  tempPath: '',
  concurrentDownloads: 2,
  pathMapping: undefined,
  prowlarr: undefined,
  arrInstances: [],
})

const status = reactive<StatusResponse>({
  configured: false,
  ffmpeg: { found: false },
  paths: { downloadOk: false, tempOk: false },
  mediathek: { reachable: false },
  prowlarr: undefined,
  arrInstances: [],
})

const testing = ref<Record<string, boolean>>({})

// --- Methods ---

async function loadData() {
  loading.value = true
  error.value = null
  try {
    const [configData, statusData] = await Promise.all([
      api<ConfigResponse>(`${API_BASE}/config`),
      api<StatusResponse>(`${API_BASE}/setup/status`),
    ])
    Object.assign(config, configData)
    Object.assign(status, statusData)
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e)
  } finally {
    loading.value = false
  }
}

function maskedApiKey(key: string): string {
  if (key.length <= 4) return key
  return '●'.repeat(8) + key.slice(-4)
}

async function copyApiKey() {
  try {
    await navigator.clipboard.writeText(config.apiKey)
  } catch {
    // clipboard API may fail in non-secure contexts
  }
}

async function regenerateApiKey() {
  try {
    const result = await apiPost<{ apiKey: string }>(`${API_BASE}/config/regenerate-key`, {})
    config.apiKey = result.apiKey
    setApiKey(result.apiKey)
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e)
  }
}

async function testConnection(name: string, endpoint: string) {
  testing.value = { ...testing.value, [name]: true }
  try {
    const result = await api<{ connected: boolean; error?: string }>(endpoint)
    // Update status for the tested connection
    if (name === 'prowlarr' && status.prowlarr) {
      status.prowlarr.connected = result.connected
      status.prowlarr.error = result.error
    } else if (name === 'mediathek') {
      status.mediathek.reachable = result.connected
    } else {
      const inst = status.arrInstances.find((a) => a.name === name)
      if (inst) {
        inst.connected = result.connected
        inst.error = result.error
      }
    }
  } catch (e) {
    // Connection test failed
  } finally {
    testing.value = { ...testing.value, [name]: false }
  }
}

async function save() {
  saving.value = true
  saveMessage.value = null
  try {
    await apiPut(`${API_BASE}/config`, {
      downloadPath: config.downloadPath,
      tempPath: config.tempPath,
      concurrentDownloads: config.concurrentDownloads,
      pathMapping: config.pathMapping || undefined,
    })
    saveMessage.value = 'Saved'
    setTimeout(() => (saveMessage.value = null), 2000)
  } catch (e) {
    saveMessage.value = e instanceof Error ? e.message : String(e)
  } finally {
    saving.value = false
  }
}

function goToSetup() {
  router.push('/setup')
}

onMounted(loadData)
</script>

<template>
  <div>
    <p v-if="loading" class="text-center text-neutral-500 py-12">Loading...</p>
    <p v-else-if="error" class="text-center text-red-600 dark:text-red-400 py-12">{{ error }}</p>

    <div v-else class="space-y-8">
      <!-- Connections -->
      <section>
        <p class="text-sm font-medium text-neutral-500 uppercase tracking-wide mb-2">Connections</p>
        <div class="space-y-2">
          <!-- Prowlarr -->
          <div
            v-if="config.prowlarr"
            class="border border-neutral-200 dark:border-neutral-700 rounded p-4 flex items-center justify-between"
          >
            <div class="flex items-center gap-3 min-w-0">
              <span
                class="w-2 h-2 rounded-full inline-block shrink-0"
                :class="status.prowlarr?.connected ? 'bg-green-500' : 'bg-red-500'"
              />
              <div class="min-w-0">
                <span class="text-sm font-medium">Prowlarr</span>
                <span class="text-xs text-neutral-400 ml-2 font-mono truncate">{{ config.prowlarr.url }}</span>
                <span v-if="status.prowlarr?.error" class="text-xs text-red-500 ml-2">{{ status.prowlarr.error }}</span>
              </div>
            </div>
            <button
              class="text-xs px-2 py-1 border border-neutral-300 dark:border-neutral-600 rounded hover:bg-neutral-100 dark:hover:bg-neutral-800"
              :disabled="testing['prowlarr']"
              @click="testConnection('prowlarr', `${API_BASE}/setup/test/prowlarr`)"
            >
              {{ testing['prowlarr'] ? 'Testing...' : 'Test' }}
            </button>
          </div>

          <!-- Arr instances -->
          <div
            v-for="inst in config.arrInstances"
            :key="inst.name"
            class="border border-neutral-200 dark:border-neutral-700 rounded p-4 flex items-center justify-between"
          >
            <div class="flex items-center gap-3 min-w-0">
              <span
                class="w-2 h-2 rounded-full inline-block shrink-0"
                :class="status.arrInstances.find(a => a.name === inst.name)?.connected ? 'bg-green-500' : 'bg-red-500'"
              />
              <div class="min-w-0">
                <span class="text-sm font-medium">{{ inst.name }}</span>
                <span class="text-xs text-neutral-400 ml-1">({{ inst.type }})</span>
                <span class="text-xs text-neutral-400 ml-2 font-mono truncate">{{ inst.url }}</span>
                <span
                  v-if="status.arrInstances.find(a => a.name === inst.name)?.error"
                  class="text-xs text-red-500 ml-2"
                >
                  {{ status.arrInstances.find(a => a.name === inst.name)?.error }}
                </span>
              </div>
            </div>
            <button
              class="text-xs px-2 py-1 border border-neutral-300 dark:border-neutral-600 rounded hover:bg-neutral-100 dark:hover:bg-neutral-800"
              :disabled="testing[inst.name]"
              @click="testConnection(inst.name, `${API_BASE}/setup/test/arr/${encodeURIComponent(inst.name)}`)"
            >
              {{ testing[inst.name] ? 'Testing...' : 'Test' }}
            </button>
          </div>

          <!-- Mediathek -->
          <div
            class="border border-neutral-200 dark:border-neutral-700 rounded p-4 flex items-center justify-between"
          >
            <div class="flex items-center gap-3">
              <span
                class="w-2 h-2 rounded-full inline-block shrink-0"
                :class="status.mediathek.reachable ? 'bg-green-500' : 'bg-red-500'"
              />
              <span class="text-sm font-medium">MediathekViewWeb</span>
            </div>
            <button
              class="text-xs px-2 py-1 border border-neutral-300 dark:border-neutral-600 rounded hover:bg-neutral-100 dark:hover:bg-neutral-800"
              :disabled="testing['mediathek']"
              @click="testConnection('mediathek', `${API_BASE}/setup/test/mediathek`)"
            >
              {{ testing['mediathek'] ? 'Testing...' : 'Test' }}
            </button>
          </div>
        </div>
      </section>

      <!-- API Key -->
      <section>
        <p class="text-sm font-medium text-neutral-500 uppercase tracking-wide mb-2">API Key</p>
        <div class="border border-neutral-200 dark:border-neutral-700 rounded p-4">
          <div class="flex items-center gap-3">
            <span class="font-mono text-sm flex-1 truncate">{{ maskedApiKey(config.apiKey) }}</span>
            <button
              class="text-xs px-2 py-1 border border-neutral-300 dark:border-neutral-600 rounded hover:bg-neutral-100 dark:hover:bg-neutral-800"
              @click="copyApiKey"
            >
              Copy
            </button>
            <button
              class="text-xs px-2 py-1 border border-red-300 dark:border-red-700 text-red-600 dark:text-red-400 rounded hover:bg-red-50 dark:hover:bg-red-950/30"
              @click="regenerateApiKey"
            >
              Regenerate
            </button>
          </div>
        </div>
      </section>

      <!-- Paths & Downloads -->
      <section>
        <p class="text-sm font-medium text-neutral-500 uppercase tracking-wide mb-2">Paths & Downloads</p>
        <div class="border border-neutral-200 dark:border-neutral-700 rounded p-4 space-y-3">
          <div>
            <label class="text-xs text-neutral-500 block mb-1">Download Path</label>
            <input
              v-model="config.downloadPath"
              type="text"
              class="border border-neutral-300 dark:border-neutral-600 dark:bg-neutral-800 rounded px-2 py-1.5 text-sm w-full font-mono"
            />
          </div>
          <div>
            <label class="text-xs text-neutral-500 block mb-1">Temp Path</label>
            <input
              v-model="config.tempPath"
              type="text"
              class="border border-neutral-300 dark:border-neutral-600 dark:bg-neutral-800 rounded px-2 py-1.5 text-sm w-full font-mono"
            />
          </div>
          <div>
            <label class="text-xs text-neutral-500 block mb-1">Path Mapping (optional)</label>
            <input
              v-model="config.pathMapping"
              type="text"
              placeholder="e.g. /downloads=/media/downloads"
              class="border border-neutral-300 dark:border-neutral-600 dark:bg-neutral-800 rounded px-2 py-1.5 text-sm w-full font-mono"
            />
          </div>
          <div>
            <label class="text-xs text-neutral-500 block mb-1">Concurrent Downloads</label>
            <select
              v-model.number="config.concurrentDownloads"
              class="border border-neutral-300 dark:border-neutral-600 dark:bg-neutral-800 rounded px-2 py-1.5 text-sm"
            >
              <option v-for="n in 10" :key="n" :value="n">{{ n }}</option>
            </select>
          </div>
        </div>
      </section>

      <!-- System Info -->
      <section>
        <p class="text-sm font-medium text-neutral-500 uppercase tracking-wide mb-2">System Info</p>
        <div class="border border-neutral-200 dark:border-neutral-700 rounded p-4 space-y-2 text-sm">
          <div class="flex items-center gap-2">
            <span class="text-neutral-500 w-24 shrink-0">FFmpeg</span>
            <span v-if="status.ffmpeg.found" class="font-mono">{{ status.ffmpeg.version }}</span>
            <span v-else class="text-red-500">not found</span>
          </div>
          <div class="flex items-center gap-2">
            <span class="text-neutral-500 w-24 shrink-0">Downloads</span>
            <span
              class="w-2 h-2 rounded-full inline-block"
              :class="status.paths.downloadOk ? 'bg-green-500' : 'bg-red-500'"
            />
            <span class="text-xs text-neutral-400">{{ status.paths.downloadOk ? 'writable' : 'not writable' }}</span>
          </div>
          <div class="flex items-center gap-2">
            <span class="text-neutral-500 w-24 shrink-0">Temp</span>
            <span
              class="w-2 h-2 rounded-full inline-block"
              :class="status.paths.tempOk ? 'bg-green-500' : 'bg-red-500'"
            />
            <span class="text-xs text-neutral-400">{{ status.paths.tempOk ? 'writable' : 'not writable' }}</span>
          </div>
        </div>
      </section>

      <!-- Save -->
      <div class="flex items-center gap-3">
        <button
          class="px-4 py-1.5 text-sm bg-neutral-800 text-white dark:bg-neutral-200 dark:text-neutral-900 rounded hover:bg-neutral-700 dark:hover:bg-neutral-300 disabled:opacity-50"
          :disabled="saving"
          @click="save"
        >
          {{ saving ? 'Saving...' : 'Save' }}
        </button>
        <span v-if="saveMessage" class="text-xs" :class="saveMessage === 'Saved' ? 'text-green-600' : 'text-red-500'">
          {{ saveMessage }}
        </span>
      </div>

      <!-- Re-run Setup -->
      <div class="pt-4 border-t border-neutral-200 dark:border-neutral-700">
        <button
          class="text-sm text-neutral-500 hover:text-neutral-700 dark:hover:text-neutral-300"
          @click="goToSetup"
        >
          Re-run Setup Wizard
        </button>
      </div>
    </div>
  </div>
</template>
