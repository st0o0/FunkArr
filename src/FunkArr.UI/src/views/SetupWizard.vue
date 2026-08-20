<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { api, apiPost, apiPut, setApiKey } from '@/api/client'

const router = useRouter()

// --- Step tracking ---
const currentStep = ref(1)
const mode = ref<'with-prowlarr' | 'without-prowlarr' | null>(null)

const steps = computed(() => {
  const base = [
    { num: 1, label: 'API Key' },
    { num: 2, label: 'Mode' },
  ]
  let n = 3
  if (mode.value === 'with-prowlarr') {
    base.push({ num: n++, label: 'Prowlarr' })
  }
  base.push({ num: n++, label: 'Arr Instances' })
  base.push({ num: n++, label: 'Paths' })
  base.push({ num: n++, label: 'Verify' })
  return base
})

const totalSteps = computed(() => steps.value.length)

const isLastStep = computed(() => currentStep.value === totalSteps.value)

function stepIndex(step: number): number {
  return steps.value.findIndex((s) => s.num === step)
}

// Logical step number for current position
const logicalStep = computed(() => steps.value[currentStep.value - 1]?.num ?? 1)

// --- Step 1: API Key ---
const apiKey = ref('')
const apiKeyCopied = ref(false)

function generateApiKey() {
  apiKey.value = crypto.randomUUID().replace(/-/g, '')
  persistApiKey()
}

function persistApiKey() {
  if (apiKey.value.trim()) {
    setApiKey(apiKey.value.trim())
  }
}

async function copyApiKey() {
  await navigator.clipboard.writeText(apiKey.value)
  apiKeyCopied.value = true
  setTimeout(() => (apiKeyCopied.value = false), 2000)
}

// --- Step 3: Prowlarr ---
const prowlarr = ref({ url: '', apiKey: '' })
const prowlarrTest = ref<{ loading: boolean; result: { success: boolean; error?: string } | null }>({
  loading: false,
  result: null,
})
const prowlarrUrlCopied = ref(false)
const prowlarrKeyCopied = ref(false)

async function testProwlarr() {
  prowlarrTest.value = { loading: true, result: null }
  try {
    const res = await apiPost<{ success: boolean; error?: string }>('/api/setup/test-prowlarr', {
      url: prowlarr.value.url,
      apiKey: prowlarr.value.apiKey,
    })
    prowlarrTest.value = { loading: false, result: res }
  } catch (e: any) {
    prowlarrTest.value = { loading: false, result: { success: false, error: e.message } }
  }
}

async function copyProwlarrUrl() {
  await navigator.clipboard.writeText('http://funkarr:5000/api')
  prowlarrUrlCopied.value = true
  setTimeout(() => (prowlarrUrlCopied.value = false), 2000)
}

async function copyProwlarrKey() {
  await navigator.clipboard.writeText(apiKey.value)
  prowlarrKeyCopied.value = true
  setTimeout(() => (prowlarrKeyCopied.value = false), 2000)
}

// --- Step 4: Arr Instances ---
interface ArrInstance {
  name: string
  type: 'Sonarr' | 'Radarr'
  url: string
  apiKey: string
  testResult: { success: boolean; version?: string; error?: string } | null
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
    const res = await apiPost<{ success: boolean; version?: string; error?: string }>(
      '/api/setup/test-arr',
      {
        url: inst.url,
        apiKey: inst.apiKey,
        type: inst.type,
      },
    )
    inst.testResult = res
  } catch (e: any) {
    inst.testResult = { success: false, error: e.message }
  } finally {
    inst.testing = false
  }
}

// --- Step 5: Paths ---
const downloadPath = ref('/downloads')
const tempPath = ref('/tmp/funkarr')
const concurrentDownloads = ref(3)
const pathMapping = ref('')
const pathsTest = ref<{
  loading: boolean
  result: { downloadPath: { ok: boolean; error?: string }; tempPath: { ok: boolean; error?: string } } | null
}>({ loading: false, result: null })

async function testPaths() {
  pathsTest.value = { loading: true, result: null }
  try {
    const res = await apiPost<{
      downloadPath: { ok: boolean; error?: string }
      tempPath: { ok: boolean; error?: string }
    }>('/api/setup/test-paths', {
      downloadPath: downloadPath.value,
      tempPath: tempPath.value,
    })
    pathsTest.value = { loading: false, result: res }
  } catch (e: any) {
    pathsTest.value = {
      loading: false,
      result: {
        downloadPath: { ok: false, error: e.message },
        tempPath: { ok: false, error: e.message },
      },
    }
  }
}

// --- Step 6: Verify ---
interface VerifyCheck {
  label: string
  status: 'pending' | 'loading' | 'success' | 'error'
  detail?: string
}

const verifyChecks = ref<VerifyCheck[]>([])
const verifyRan = ref(false)

async function runVerification() {
  verifyRan.value = true
  const checks: VerifyCheck[] = [
    { label: 'FFmpeg', status: 'loading' },
    { label: 'Paths', status: 'loading' },
    { label: 'MediathekViewWeb', status: 'loading' },
  ]
  if (mode.value === 'with-prowlarr') {
    checks.push({ label: 'Prowlarr', status: 'loading' })
  }
  for (const inst of arrInstances.value) {
    checks.push({ label: `${inst.type}: ${inst.name || inst.url}`, status: 'loading' })
  }
  verifyChecks.value = checks

  // FFmpeg
  runCheck(0, async () => {
    const res = await apiPost<{ found: boolean; version?: string }>('/api/setup/test-ffmpeg', {})
    if (!res.found) throw new Error('FFmpeg not found')
    return res.version ? `v${res.version}` : 'Found'
  })

  // Paths
  runCheck(1, async () => {
    const res = await apiPost<{
      downloadPath: { ok: boolean; error?: string }
      tempPath: { ok: boolean; error?: string }
    }>('/api/setup/test-paths', {
      downloadPath: downloadPath.value,
      tempPath: tempPath.value,
    })
    const errors: string[] = []
    if (!res.downloadPath.ok) errors.push(`Download: ${res.downloadPath.error}`)
    if (!res.tempPath.ok) errors.push(`Temp: ${res.tempPath.error}`)
    if (errors.length) throw new Error(errors.join('; '))
    return 'OK'
  })

  // Mediathek
  runCheck(2, async () => {
    const res = await apiPost<{ reachable: boolean; error?: string }>(
      '/api/setup/test-mediathek',
      {},
    )
    if (!res.reachable) throw new Error(res.error || 'Unreachable')
    return 'Reachable'
  })

  // Prowlarr
  let idx = 3
  if (mode.value === 'with-prowlarr') {
    runCheck(idx, async () => {
      const res = await apiPost<{ success: boolean; error?: string }>('/api/setup/test-prowlarr', {
        url: prowlarr.value.url,
        apiKey: prowlarr.value.apiKey,
      })
      if (!res.success) throw new Error(res.error || 'Connection failed')
      return 'Connected'
    })
    idx++
  }

  // Arr instances
  for (let i = 0; i < arrInstances.value.length; i++) {
    const inst = arrInstances.value[i]
    runCheck(idx + i, async () => {
      const res = await apiPost<{ success: boolean; version?: string; error?: string }>(
        '/api/setup/test-arr',
        {
          url: inst.url,
          apiKey: inst.apiKey,
          type: inst.type,
        },
      )
      if (!res.success) throw new Error(res.error || 'Connection failed')
      return res.version ? `v${res.version}` : 'Connected'
    })
  }
}

async function runCheck(index: number, fn: () => Promise<string>) {
  try {
    const detail = await fn()
    verifyChecks.value[index] = {
      ...verifyChecks.value[index],
      status: 'success',
      detail,
    }
  } catch (e: any) {
    verifyChecks.value[index] = {
      ...verifyChecks.value[index],
      status: 'error',
      detail: e.message,
    }
  }
}

// --- Navigation ---
function next() {
  if (currentStep.value === 1) {
    persistApiKey()
  }
  if (currentStep.value < totalSteps.value) {
    currentStep.value++
  }
}

function back() {
  if (currentStep.value > 1) {
    currentStep.value--
  }
}

function selectMode(m: 'with-prowlarr' | 'without-prowlarr') {
  mode.value = m
  // Reset step to 3 when changing mode (in case user goes back)
  next()
}

const finishing = ref(false)

async function finishSetup() {
  finishing.value = true
  try {
    setApiKey(apiKey.value.trim())

    const config: Record<string, unknown> = {
      apiKey: apiKey.value.trim(),
      downloadPath: downloadPath.value,
      tempPath: tempPath.value,
      concurrentDownloads: concurrentDownloads.value,
    }

    if (pathMapping.value.trim()) {
      config.pathMapping = pathMapping.value.trim()
    }

    if (mode.value === 'with-prowlarr') {
      config.prowlarr = {
        url: prowlarr.value.url,
        apiKey: prowlarr.value.apiKey,
      }
    }

    if (arrInstances.value.length > 0) {
      config.arrInstances = arrInstances.value.map((inst) => ({
        name: inst.name,
        type: inst.type,
        url: inst.url,
        apiKey: inst.apiKey,
      }))
    }

    await apiPut('/api/config', config)
    router.push('/')
  } catch (e: any) {
    alert(`Failed to save configuration: ${e.message}`)
  } finally {
    finishing.value = false
  }
}

const canProceed = computed(() => {
  switch (currentStep.value) {
    case 1:
      return apiKey.value.trim().length > 0
    case 2:
      return mode.value !== null
    default:
      return true
  }
})
</script>

<template>
  <div class="mx-auto max-w-2xl py-8">
    <h1 class="mb-8 text-center text-2xl font-bold">FunkArr Setup</h1>

    <!-- Step indicator -->
    <div class="mb-8 flex items-center justify-center gap-2">
      <template v-for="(step, i) in steps" :key="step.num">
        <div
          class="flex h-8 w-8 items-center justify-center rounded-full text-sm font-medium"
          :class="
            currentStep === i + 1
              ? 'bg-blue-600 text-white'
              : currentStep > i + 1
                ? 'bg-neutral-200 text-neutral-600 dark:bg-neutral-700 dark:text-neutral-300'
                : 'bg-neutral-100 text-neutral-400 dark:bg-neutral-800 dark:text-neutral-500'
          "
        >
          {{ i + 1 }}
        </div>
        <div
          v-if="i < steps.length - 1"
          class="h-px w-6 bg-neutral-200 dark:bg-neutral-700"
        />
      </template>
    </div>

    <!-- Step 1: API Key -->
    <div v-if="currentStep === 1">
      <h2 class="mb-4 text-lg font-semibold">API Key</h2>
      <p class="mb-4 text-sm text-neutral-600 dark:text-neutral-400">
        Generate a new API key or enter an existing one. This key authenticates all API requests.
      </p>

      <div class="mb-4 flex gap-2">
        <input
          v-model="apiKey"
          type="text"
          placeholder="API key"
          class="flex-1 rounded border border-neutral-200 bg-white px-3 py-2 font-mono text-sm dark:border-neutral-700 dark:bg-neutral-800"
          @blur="persistApiKey"
        />
        <button
          class="rounded border border-neutral-200 px-3 py-2 text-sm hover:bg-neutral-50 dark:border-neutral-700 dark:hover:bg-neutral-800"
          @click="copyApiKey"
        >
          {{ apiKeyCopied ? 'Copied' : 'Copy' }}
        </button>
      </div>

      <button
        class="rounded bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700"
        @click="generateApiKey"
      >
        Generate Random Key
      </button>
    </div>

    <!-- Step 2: Mode Selection -->
    <div v-if="currentStep === 2">
      <h2 class="mb-4 text-lg font-semibold">Integration Mode</h2>
      <p class="mb-4 text-sm text-neutral-600 dark:text-neutral-400">
        Choose how FunkArr integrates with your media stack.
      </p>

      <div class="grid grid-cols-2 gap-4">
        <button
          class="rounded border p-4 text-left transition-colors"
          :class="
            mode === 'with-prowlarr'
              ? 'border-blue-600 bg-blue-50 dark:bg-blue-950'
              : 'border-neutral-200 hover:border-neutral-400 dark:border-neutral-700 dark:hover:border-neutral-500'
          "
          @click="selectMode('with-prowlarr')"
        >
          <div class="mb-1 font-medium">With Prowlarr</div>
          <div class="text-sm text-neutral-600 dark:text-neutral-400">
            Prowlarr manages the indexer. Add FunkArr as a download client in Sonarr/Radarr.
          </div>
        </button>

        <button
          class="rounded border p-4 text-left transition-colors"
          :class="
            mode === 'without-prowlarr'
              ? 'border-blue-600 bg-blue-50 dark:bg-blue-950'
              : 'border-neutral-200 hover:border-neutral-400 dark:border-neutral-700 dark:hover:border-neutral-500'
          "
          @click="selectMode('without-prowlarr')"
        >
          <div class="mb-1 font-medium">Without Prowlarr</div>
          <div class="text-sm text-neutral-600 dark:text-neutral-400">
            Add FunkArr directly as indexer and download client in Sonarr/Radarr.
          </div>
        </button>
      </div>
    </div>

    <!-- Step 3: Prowlarr (conditional) -->
    <div v-if="steps[currentStep - 1]?.label === 'Prowlarr'">
      <h2 class="mb-4 text-lg font-semibold">Prowlarr Connection</h2>

      <div class="mb-4 space-y-3">
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
            :disabled="prowlarrTest.loading"
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

      <div class="rounded bg-neutral-50 p-4 dark:bg-neutral-800">
        <p class="mb-2 text-sm font-medium">Add FunkArr as a Newznab indexer in Prowlarr:</p>
        <div class="space-y-2">
          <div class="flex items-center gap-2">
            <span class="text-sm text-neutral-600 dark:text-neutral-400">URL:</span>
            <code class="font-mono text-sm">http://funkarr:5000/api</code>
            <button
              class="text-sm text-blue-600 hover:text-blue-700 dark:text-blue-400"
              @click="copyProwlarrUrl"
            >
              {{ prowlarrUrlCopied ? 'Copied' : '[Copy]' }}
            </button>
          </div>
          <div class="flex items-center gap-2">
            <span class="text-sm text-neutral-600 dark:text-neutral-400">API Key:</span>
            <code class="font-mono text-sm">{{ apiKey }}</code>
            <button
              class="text-sm text-blue-600 hover:text-blue-700 dark:text-blue-400"
              @click="copyProwlarrKey"
            >
              {{ prowlarrKeyCopied ? 'Copied' : '[Copy]' }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Step 4: Arr Instances -->
    <div v-if="steps[currentStep - 1]?.label === 'Arr Instances'">
      <h2 class="mb-4 text-lg font-semibold">Arr Instances</h2>

      <div class="mb-4 rounded bg-neutral-50 p-4 dark:bg-neutral-800">
        <p class="text-sm font-medium">
          <template v-if="mode === 'with-prowlarr'">
            Add FunkArr as a <span class="font-mono">SABnzbd</span> download client in each instance.
          </template>
          <template v-else>
            Add FunkArr as both a <span class="font-mono">Newznab</span> indexer and a
            <span class="font-mono">SABnzbd</span> download client in each instance.
          </template>
        </p>
        <div class="mt-2 space-y-1 font-mono text-sm">
          <div class="flex items-center gap-2">
            <span class="text-neutral-600 dark:text-neutral-400">Host:</span>
            <code>funkarr</code>
          </div>
          <div class="flex items-center gap-2">
            <span class="text-neutral-600 dark:text-neutral-400">Port:</span>
            <code>5000</code>
          </div>
          <div class="flex items-center gap-2">
            <span class="text-neutral-600 dark:text-neutral-400">API Key:</span>
            <code>{{ apiKey }}</code>
          </div>
        </div>
      </div>

      <div class="space-y-4">
        <div
          v-for="(inst, i) in arrInstances"
          :key="i"
          class="rounded border border-neutral-200 p-4 dark:border-neutral-700"
        >
          <div class="mb-3 flex items-center justify-between">
            <span class="text-sm font-medium">Instance {{ i + 1 }}</span>
            <button
              class="text-sm text-red-600 hover:text-red-700"
              @click="removeArrInstance(i)"
            >
              Remove
            </button>
          </div>
          <div class="space-y-2">
            <div class="flex gap-2">
              <input
                v-model="inst.name"
                type="text"
                placeholder="Name (e.g. Sonarr)"
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
                :disabled="inst.testing"
                @click="testArrInstance(i)"
              >
                {{ inst.testing ? 'Testing...' : 'Test' }}
              </button>
              <span
                v-if="inst.testResult"
                class="text-sm"
                :class="inst.testResult.success ? 'text-green-600' : 'text-red-600'"
              >
                {{ inst.testResult.success ? (inst.testResult.version ? `Connected (v${inst.testResult.version})` : 'Connected') : inst.testResult.error }}
              </span>
            </div>
          </div>
        </div>
      </div>

      <button
        class="mt-4 rounded border border-neutral-200 px-4 py-2 text-sm hover:bg-neutral-50 dark:border-neutral-700 dark:hover:bg-neutral-800"
        @click="addArrInstance"
      >
        + Add Instance
      </button>
    </div>

    <!-- Step 5: Paths -->
    <div v-if="steps[currentStep - 1]?.label === 'Paths'">
      <h2 class="mb-4 text-lg font-semibold">Paths & Downloads</h2>

      <div class="space-y-4">
        <div>
          <label class="mb-1 block text-sm font-medium">Download Path</label>
          <input
            v-model="downloadPath"
            type="text"
            placeholder="/downloads"
            class="w-full rounded border border-neutral-200 bg-white px-3 py-2 font-mono text-sm dark:border-neutral-700 dark:bg-neutral-800"
          />
        </div>

        <div>
          <label class="mb-1 block text-sm font-medium">Temp Path</label>
          <input
            v-model="tempPath"
            type="text"
            placeholder="/tmp/funkarr"
            class="w-full rounded border border-neutral-200 bg-white px-3 py-2 font-mono text-sm dark:border-neutral-700 dark:bg-neutral-800"
          />
        </div>

        <div>
          <label class="mb-1 block text-sm font-medium">Concurrent Downloads</label>
          <input
            v-model.number="concurrentDownloads"
            type="number"
            min="1"
            max="10"
            class="w-24 rounded border border-neutral-200 bg-white px-3 py-2 text-sm dark:border-neutral-700 dark:bg-neutral-800"
          />
        </div>

        <div>
          <label class="mb-1 block text-sm font-medium">
            Path Mapping
            <span class="font-normal text-neutral-500">(optional)</span>
          </label>
          <input
            v-model="pathMapping"
            type="text"
            placeholder="/downloads:/media/downloads"
            class="w-full rounded border border-neutral-200 bg-white px-3 py-2 font-mono text-sm dark:border-neutral-700 dark:bg-neutral-800"
          />
          <p class="mt-1 text-xs text-neutral-500">
            Map container paths to host paths (container:host). Leave empty if paths match.
          </p>
        </div>

        <div class="flex items-center gap-3">
          <button
            class="rounded bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700"
            :disabled="pathsTest.loading"
            @click="testPaths"
          >
            {{ pathsTest.loading ? 'Testing...' : 'Test Paths' }}
          </button>
          <template v-if="pathsTest.result">
            <span
              class="text-sm"
              :class="pathsTest.result.downloadPath.ok ? 'text-green-600' : 'text-red-600'"
            >
              Download: {{ pathsTest.result.downloadPath.ok ? 'OK' : pathsTest.result.downloadPath.error }}
            </span>
            <span
              class="text-sm"
              :class="pathsTest.result.tempPath.ok ? 'text-green-600' : 'text-red-600'"
            >
              Temp: {{ pathsTest.result.tempPath.ok ? 'OK' : pathsTest.result.tempPath.error }}
            </span>
          </template>
        </div>
      </div>
    </div>

    <!-- Step 6: Verify -->
    <div v-if="steps[currentStep - 1]?.label === 'Verify'">
      <h2 class="mb-4 text-lg font-semibold">Verification</h2>
      <p class="mb-4 text-sm text-neutral-600 dark:text-neutral-400">
        Test all connections and dependencies before saving.
      </p>

      <button
        v-if="!verifyRan"
        class="mb-4 rounded bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700"
        @click="runVerification"
      >
        Run Checks
      </button>

      <div v-if="verifyRan" class="space-y-2">
        <div
          v-for="check in verifyChecks"
          :key="check.label"
          class="flex items-center gap-3 rounded border border-neutral-200 px-4 py-3 dark:border-neutral-700"
        >
          <span
            v-if="check.status === 'loading'"
            class="text-sm text-neutral-500"
          >...</span>
          <span
            v-else-if="check.status === 'success'"
            class="text-sm text-green-600"
          >OK</span>
          <span
            v-else-if="check.status === 'error'"
            class="text-sm text-red-600"
          >FAIL</span>
          <span
            v-else
            class="text-sm text-neutral-400"
          >--</span>

          <span class="text-sm font-medium">{{ check.label }}</span>
          <span
            v-if="check.detail"
            class="text-sm text-neutral-500"
          >{{ check.detail }}</span>
        </div>

        <button
          class="mt-2 text-sm text-blue-600 hover:text-blue-700 dark:text-blue-400"
          @click="runVerification"
        >
          Re-run Checks
        </button>
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
        v-if="isLastStep"
        class="rounded bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700"
        :disabled="finishing"
        @click="finishSetup"
      >
        {{ finishing ? 'Saving...' : 'Finish Setup' }}
      </button>
      <button
        v-else-if="currentStep !== 2"
        class="rounded bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700"
        :disabled="!canProceed"
        @click="next"
      >
        Next
      </button>
    </div>
  </div>
</template>
