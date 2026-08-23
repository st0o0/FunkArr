<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { api, apiPost, API_BASE } from '@/api/client'

const router = useRouter()

const currentStep = ref(1)
const totalSteps = 4

interface StatusResponse {
  configured: boolean
  apiKey: string
  ffmpeg: { found: boolean; version?: string }
  paths: { downloadOk: boolean; tempOk: boolean }
  mediathek: { reachable: boolean }
}

const status = ref<StatusResponse | null>(null)
const statusLoading = ref(false)
const apiKeyCopied = ref(false)

async function loadStatus() {
  statusLoading.value = true
  try {
    status.value = await api<StatusResponse>(`${API_BASE}/setup/status`)
  } catch {
    status.value = null
  } finally {
    statusLoading.value = false
  }
}

onMounted(loadStatus)

async function copyText(text: string) {
  await navigator.clipboard.writeText(text)
  apiKeyCopied.value = true
  setTimeout(() => (apiKeyCopied.value = false), 2000)
}

// --- Prowlarr test ---
const prowlarr = ref({ url: '', apiKey: '' })
const prowlarrTest = ref<{ loading: boolean; result: { success: boolean; error?: string } | null }>({
  loading: false,
  result: null,
})

async function testProwlarr() {
  prowlarrTest.value = { loading: true, result: null }
  try {
    const res = await apiPost<{ success: boolean; error?: string }>(`${API_BASE}/setup/test-prowlarr`, {
      url: prowlarr.value.url,
      apiKey: prowlarr.value.apiKey,
    })
    prowlarrTest.value = { loading: false, result: res }
  } catch (e: any) {
    prowlarrTest.value = { loading: false, result: { success: false, error: e.message } }
  }
}

// --- Arr instance test ---
interface ArrInstance {
  name: string
  type: 'Sonarr' | 'Radarr'
  url: string
  apiKey: string
  testResult: { success: boolean; error?: string } | null
  testing: boolean
}

const arrInstances = ref<ArrInstance[]>([])

function addArrInstance() {
  arrInstances.value.push({
    name: '',
    type: 'Sonarr',
    url: '',
    apiKey: '',
    testResult: null,
    testing: false,
  })
}

function removeArrInstance(index: number) {
  arrInstances.value.splice(index, 1)
}

async function testArrInstance(index: number) {
  const inst = arrInstances.value[index]
  inst.testing = true
  inst.testResult = null
  try {
    const res = await apiPost<{ success: boolean; error?: string }>(`${API_BASE}/setup/test-arr`, {
      url: inst.url,
      apiKey: inst.apiKey,
      type: inst.type,
    })
    inst.testResult = res
  } catch (e: any) {
    inst.testResult = { success: false, error: e.message }
  } finally {
    inst.testing = false
  }
}

// --- Navigation ---
function next() {
  if (currentStep.value < totalSteps) currentStep.value++
}

function back() {
  if (currentStep.value > 1) currentStep.value--
}

function finish() {
  router.push('/')
}
</script>

<template>
  <div class="mx-auto max-w-2xl py-8">
    <h1 class="mb-8 text-center text-2xl font-bold">FunkArr Setup Guide</h1>

    <!-- Step indicator -->
    <div class="mb-8 flex items-center justify-center gap-2">
      <template v-for="step in totalSteps" :key="step">
        <div
          class="flex h-8 w-8 items-center justify-center rounded-full text-sm font-medium"
          :class="
            currentStep === step
              ? 'bg-blue-600 text-white'
              : currentStep > step
                ? 'bg-neutral-200 text-neutral-600 dark:bg-neutral-700 dark:text-neutral-300'
                : 'bg-neutral-100 text-neutral-400 dark:bg-neutral-800 dark:text-neutral-500'
          "
        >
          {{ step }}
        </div>
        <div
          v-if="step < totalSteps"
          class="h-px w-6 bg-neutral-200 dark:bg-neutral-700"
        />
      </template>
    </div>

    <!-- Step 1: Self-Check -->
    <div v-if="currentStep === 1">
      <h2 class="mb-4 text-lg font-semibold">System Check</h2>
      <p class="mb-4 text-sm text-neutral-600 dark:text-neutral-400">
        Verify that FunkArr is configured correctly. Configuration is done via environment variables in your docker-compose.yml.
      </p>

      <div v-if="statusLoading" class="text-center text-neutral-500 py-8">Loading...</div>

      <div v-else-if="status" class="space-y-2">
        <div class="flex items-center gap-3 rounded border border-neutral-200 px-4 py-3 dark:border-neutral-700">
          <span class="w-2 h-2 rounded-full shrink-0" :class="status.apiKey ? 'bg-green-500' : 'bg-red-500'" />
          <span class="text-sm font-medium flex-1">API Key</span>
          <span v-if="status.apiKey" class="text-xs text-green-600">Configured</span>
          <span v-else class="text-xs text-red-600">Set FunkArr__ApiKey</span>
        </div>

        <div class="flex items-center gap-3 rounded border border-neutral-200 px-4 py-3 dark:border-neutral-700">
          <span class="w-2 h-2 rounded-full shrink-0" :class="status.ffmpeg.found ? 'bg-green-500' : 'bg-red-500'" />
          <span class="text-sm font-medium flex-1">FFmpeg</span>
          <span v-if="status.ffmpeg.found" class="text-xs text-green-600">v{{ status.ffmpeg.version }}</span>
          <span v-else class="text-xs text-red-600">Not found — install FFmpeg</span>
        </div>

        <div class="flex items-center gap-3 rounded border border-neutral-200 px-4 py-3 dark:border-neutral-700">
          <span class="w-2 h-2 rounded-full shrink-0" :class="status.paths.downloadOk ? 'bg-green-500' : 'bg-red-500'" />
          <span class="text-sm font-medium flex-1">Download Path</span>
          <span v-if="status.paths.downloadOk" class="text-xs text-green-600">Writable</span>
          <span v-else class="text-xs text-red-600">Set FunkArr__Download__DownloadPath</span>
        </div>

        <div class="flex items-center gap-3 rounded border border-neutral-200 px-4 py-3 dark:border-neutral-700">
          <span class="w-2 h-2 rounded-full shrink-0" :class="status.paths.tempOk ? 'bg-green-500' : 'bg-red-500'" />
          <span class="text-sm font-medium flex-1">Temp Path</span>
          <span v-if="status.paths.tempOk" class="text-xs text-green-600">Writable</span>
          <span v-else class="text-xs text-red-600">Set FunkArr__Download__TempPath</span>
        </div>

        <div class="flex items-center gap-3 rounded border border-neutral-200 px-4 py-3 dark:border-neutral-700">
          <span class="w-2 h-2 rounded-full shrink-0" :class="status.mediathek.reachable ? 'bg-green-500' : 'bg-red-500'" />
          <span class="text-sm font-medium flex-1">MediathekViewWeb</span>
          <span v-if="status.mediathek.reachable" class="text-xs text-green-600">Reachable</span>
          <span v-else class="text-xs text-red-600">Unreachable</span>
        </div>

        <button
          class="mt-2 text-sm text-blue-600 hover:text-blue-700 dark:text-blue-400"
          @click="loadStatus"
        >
          Re-check
        </button>
      </div>
    </div>

    <!-- Step 2: Prowlarr Integration -->
    <div v-if="currentStep === 2">
      <h2 class="mb-4 text-lg font-semibold">Add FunkArr to Prowlarr</h2>
      <p class="mb-4 text-sm text-neutral-600 dark:text-neutral-400">
        Add FunkArr as a Newznab indexer in Prowlarr. Skip this step if you don't use Prowlarr.
      </p>

      <div class="rounded bg-neutral-50 p-4 dark:bg-neutral-800 mb-6">
        <p class="mb-3 text-sm font-medium">In Prowlarr: Indexers → + → Generic Newznab</p>
        <div class="space-y-2">
          <div class="flex items-center gap-2">
            <span class="text-sm text-neutral-600 dark:text-neutral-400 w-16">URL:</span>
            <code class="font-mono text-sm flex-1">http://funkarr:6969/index</code>
            <button
              class="text-sm text-blue-600 hover:text-blue-700 dark:text-blue-400"
              @click="copyText('http://funkarr:6969/index')"
            >
              Copy
            </button>
          </div>
          <div class="flex items-center gap-2">
            <span class="text-sm text-neutral-600 dark:text-neutral-400 w-16">API Key:</span>
            <code class="font-mono text-sm flex-1">{{ status?.apiKey ?? '...' }}</code>
            <button
              class="text-sm text-blue-600 hover:text-blue-700 dark:text-blue-400"
              @click="copyText(status?.apiKey ?? '')"
            >
              {{ apiKeyCopied ? 'Copied' : 'Copy' }}
            </button>
          </div>
        </div>
      </div>

      <div class="rounded border border-neutral-200 p-4 dark:border-neutral-700">
        <p class="mb-3 text-sm font-medium text-neutral-600 dark:text-neutral-400">Optional: Verify connection</p>
        <div class="space-y-2">
          <input
            v-model="prowlarr.url"
            type="text"
            placeholder="Prowlarr URL (e.g. http://prowlarr:9696)"
            class="w-full rounded border border-neutral-200 bg-white px-3 py-2 text-sm dark:border-neutral-700 dark:bg-neutral-800"
          />
          <input
            v-model="prowlarr.apiKey"
            type="text"
            placeholder="Prowlarr API key"
            class="w-full rounded border border-neutral-200 bg-white px-3 py-2 font-mono text-sm dark:border-neutral-700 dark:bg-neutral-800"
          />
          <div class="flex items-center gap-3">
            <button
              class="rounded bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700"
              :disabled="prowlarrTest.loading || !prowlarr.url || !prowlarr.apiKey"
              @click="testProwlarr"
            >
              {{ prowlarrTest.loading ? 'Testing...' : 'Test Connection' }}
            </button>
            <span
              v-if="prowlarrTest.result"
              class="text-sm"
              :class="prowlarrTest.result.success ? 'text-green-600' : 'text-red-600'"
            >
              {{ prowlarrTest.result.success ? 'Connected' : prowlarrTest.result.error }}
            </span>
          </div>
        </div>
      </div>
    </div>

    <!-- Step 3: Arr Instances -->
    <div v-if="currentStep === 3">
      <h2 class="mb-4 text-lg font-semibold">Add FunkArr to Sonarr / Radarr</h2>
      <p class="mb-4 text-sm text-neutral-600 dark:text-neutral-400">
        Add FunkArr as a SABnzbd download client (and optionally a Newznab indexer if not using Prowlarr) in each Sonarr/Radarr instance.
      </p>

      <div class="rounded bg-neutral-50 p-4 dark:bg-neutral-800 mb-6">
        <p class="mb-3 text-sm font-medium">In Sonarr/Radarr: Settings → Download Clients → + → SABnzbd</p>
        <div class="space-y-1 font-mono text-sm">
          <div class="flex items-center gap-2">
            <span class="text-neutral-600 dark:text-neutral-400 w-20">Host:</span>
            <code>funkarr</code>
          </div>
          <div class="flex items-center gap-2">
            <span class="text-neutral-600 dark:text-neutral-400 w-20">Port:</span>
            <code>6969</code>
          </div>
          <div class="flex items-center gap-2">
            <span class="text-neutral-600 dark:text-neutral-400 w-20">URL Base:</span>
            <code>download</code>
          </div>
          <div class="flex items-center gap-2">
            <span class="text-neutral-600 dark:text-neutral-400 w-20">Category:</span>
            <code>tv</code>
            <span class="text-xs text-neutral-500">(Sonarr) /</span>
            <code>movies</code>
            <span class="text-xs text-neutral-500">(Radarr)</span>
          </div>
          <div class="flex items-center gap-2">
            <span class="text-neutral-600 dark:text-neutral-400 w-20">API Key:</span>
            <code>{{ status?.apiKey ?? '...' }}</code>
            <button
              class="text-sm text-blue-600 hover:text-blue-700 dark:text-blue-400"
              @click="copyText(status?.apiKey ?? '')"
            >
              {{ apiKeyCopied ? 'Copied' : 'Copy' }}
            </button>
          </div>
        </div>
      </div>

      <div class="rounded border border-neutral-200 p-4 dark:border-neutral-700">
        <p class="mb-3 text-sm font-medium text-neutral-600 dark:text-neutral-400">Optional: Verify connections</p>

        <div class="space-y-4">
          <div
            v-for="(inst, i) in arrInstances"
            :key="i"
            class="rounded border border-neutral-200 p-3 dark:border-neutral-700"
          >
            <div class="mb-2 flex items-center justify-between">
              <span class="text-sm font-medium">Instance {{ i + 1 }}</span>
              <button class="text-sm text-red-600 hover:text-red-700" @click="removeArrInstance(i)">Remove</button>
            </div>
            <div class="space-y-2">
              <div class="flex gap-2">
                <input
                  v-model="inst.name"
                  type="text"
                  placeholder="Name"
                  class="flex-1 rounded border border-neutral-200 bg-white px-3 py-2 text-sm dark:border-neutral-700 dark:bg-neutral-800"
                />
                <select
                  v-model="inst.type"
                  class="rounded border border-neutral-200 bg-white px-3 py-2 text-sm dark:border-neutral-700 dark:bg-neutral-800"
                >
                  <option>Sonarr</option>
                  <option>Radarr</option>
                </select>
              </div>
              <input
                v-model="inst.url"
                type="text"
                placeholder="URL (e.g. http://sonarr:8989)"
                class="w-full rounded border border-neutral-200 bg-white px-3 py-2 text-sm dark:border-neutral-700 dark:bg-neutral-800"
              />
              <input
                v-model="inst.apiKey"
                type="text"
                placeholder="API key"
                class="w-full rounded border border-neutral-200 bg-white px-3 py-2 font-mono text-sm dark:border-neutral-700 dark:bg-neutral-800"
              />
              <div class="flex items-center gap-3">
                <button
                  class="rounded bg-blue-600 px-3 py-1.5 text-sm text-white hover:bg-blue-700"
                  :disabled="inst.testing || !inst.url || !inst.apiKey"
                  @click="testArrInstance(i)"
                >
                  {{ inst.testing ? 'Testing...' : 'Test' }}
                </button>
                <span
                  v-if="inst.testResult"
                  class="text-sm"
                  :class="inst.testResult.success ? 'text-green-600' : 'text-red-600'"
                >
                  {{ inst.testResult.success ? 'Connected' : inst.testResult.error }}
                </span>
              </div>
            </div>
          </div>
        </div>

        <button
          class="mt-3 rounded border border-neutral-200 px-3 py-1.5 text-sm hover:bg-neutral-50 dark:border-neutral-700 dark:hover:bg-neutral-800"
          @click="addArrInstance"
        >
          + Add Instance
        </button>
      </div>
    </div>

    <!-- Step 4: Done -->
    <div v-if="currentStep === 4">
      <h2 class="mb-4 text-lg font-semibold">Setup Complete</h2>
      <p class="mb-6 text-sm text-neutral-600 dark:text-neutral-400">
        FunkArr is ready to use. Sonarr and Radarr can now search and download from German public broadcaster media libraries.
      </p>

      <div v-if="status" class="rounded bg-neutral-50 p-4 dark:bg-neutral-800 mb-6">
        <p class="mb-2 text-sm font-medium">System Status</p>
        <div class="space-y-1 text-sm">
          <div class="flex items-center gap-2">
            <span class="w-2 h-2 rounded-full" :class="status.configured ? 'bg-green-500' : 'bg-yellow-500'" />
            <span>{{ status.configured ? 'All systems ready' : 'Some checks need attention — see Step 1' }}</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Navigation -->
    <div class="mt-8 flex items-center justify-between border-t border-neutral-200 pt-4 dark:border-neutral-700">
      <button
        v-if="currentStep > 1"
        class="rounded border border-neutral-200 px-4 py-2 text-sm hover:bg-neutral-50 dark:border-neutral-700 dark:hover:bg-neutral-800"
        @click="back"
      >
        Back
      </button>
      <div v-else />

      <button
        v-if="currentStep === totalSteps"
        class="rounded bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700"
        @click="finish"
      >
        Go to Dashboard
      </button>
      <button
        v-else
        class="rounded bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700"
        @click="next"
      >
        {{ currentStep === 2 || currentStep === 3 ? 'Next (or Skip)' : 'Next' }}
      </button>
    </div>
  </div>
</template>
