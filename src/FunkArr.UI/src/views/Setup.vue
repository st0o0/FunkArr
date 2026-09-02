<template>
  <div>
    <h1 class="text-2xl font-bold mb-6">Setup Guide</h1>

    <!-- Step indicators -->
    <div class="flex items-center gap-2 mb-8">
      <div
        v-for="(label, i) in stepLabels"
        :key="i"
        class="flex items-center gap-2"
      >
        <div
          class="w-7 h-7 rounded-full flex items-center justify-center text-xs font-semibold"
          :class="i < currentStep
            ? 'bg-green-500 text-white'
            : i === currentStep
              ? 'bg-gray-900 text-white'
              : 'bg-gray-200 text-gray-500'"
        >
          {{ i < currentStep ? '✓' : i + 1 }}
        </div>
        <span class="text-sm text-gray-600 hidden sm:inline">{{ label }}</span>
        <span v-if="i < stepLabels.length - 1" class="w-6 border-t border-gray-300" />
      </div>
    </div>

    <!-- Step 1: Self-Check -->
    <div v-if="currentStep === 0">
      <h2 class="text-lg font-semibold mb-4">System Health Check</h2>

      <div v-if="loading" class="text-gray-500">Running checks...</div>
      <div v-else-if="error" class="text-red-600 mb-4">{{ error }}</div>

      <div v-if="health" class="space-y-3 mb-6">
        <div
          v-for="(result, name) in health.checks"
          :key="name"
          class="flex items-start gap-3 p-3 rounded border"
          :class="{
            'border-green-200 bg-green-50': result.status === 'ok',
            'border-yellow-200 bg-yellow-50': result.status === 'warn',
            'border-red-200 bg-red-50': result.status === 'fail',
          }"
        >
          <span
            class="w-3 h-3 rounded-full flex-shrink-0 mt-0.5"
            :class="{
              'bg-green-500': result.status === 'ok',
              'bg-yellow-500': result.status === 'warn',
              'bg-red-500': result.status === 'fail',
            }"
          />
          <div>
            <div class="font-medium text-sm">{{ checkLabels[name] ?? name }}</div>
            <div v-if="result.message" class="text-xs text-gray-600 mt-0.5">{{ result.message }}</div>
            <div v-if="result.path" class="text-xs text-gray-500 mt-0.5 font-mono">{{ result.path }}</div>
            <div v-if="result.version" class="text-xs text-gray-500 mt-0.5">Version: {{ result.version }}</div>
            <div v-if="result.status === 'fail'" class="text-xs text-red-700 mt-1">
              {{ fixHints[name] ?? 'Check your configuration and try again.' }}
            </div>
          </div>
        </div>
      </div>

      <div class="flex gap-3">
        <button
          @click="runHealthCheck"
          class="px-4 py-2 text-sm border border-gray-300 rounded hover:bg-gray-50"
        >
          Re-check
        </button>
        <button
          @click="currentStep++"
          :disabled="hasFailures"
          class="px-4 py-2 text-sm bg-gray-900 text-white rounded hover:bg-gray-700 disabled:opacity-40 disabled:cursor-not-allowed"
        >
          Next
        </button>
      </div>
    </div>

    <!-- Step 2: Service Selection -->
    <div v-else-if="currentStep === 1">
      <h2 class="text-lg font-semibold mb-4">Select Services to Configure</h2>
      <p class="text-gray-600 text-sm mb-6">Choose which *arr applications you want to set up with FunkArr.</p>

      <div class="space-y-3 mb-6">
        <label class="flex items-start gap-3 p-4 rounded border border-gray-200 hover:border-gray-400 cursor-pointer transition-colors">
          <input type="checkbox" v-model="selectedServices" value="prowlarr" class="mt-0.5" />
          <div>
            <div class="font-semibold text-sm">Prowlarr</div>
            <div class="text-xs text-gray-500">Indexer Manager — adds FunkArr as a Newznab indexer source</div>
          </div>
        </label>
        <label class="flex items-start gap-3 p-4 rounded border border-gray-200 hover:border-gray-400 cursor-pointer transition-colors">
          <input type="checkbox" v-model="selectedServices" value="sonarr" class="mt-0.5" />
          <div>
            <div class="font-semibold text-sm">Sonarr</div>
            <div class="text-xs text-gray-500">TV Series — adds FunkArr as a SABnzbd download client</div>
          </div>
        </label>
        <label class="flex items-start gap-3 p-4 rounded border border-gray-200 hover:border-gray-400 cursor-pointer transition-colors">
          <input type="checkbox" v-model="selectedServices" value="radarr" class="mt-0.5" />
          <div>
            <div class="font-semibold text-sm">Radarr</div>
            <div class="text-xs text-gray-500">Movies — adds FunkArr as a SABnzbd download client</div>
          </div>
        </label>
      </div>

      <div class="flex gap-3">
        <button @click="currentStep--" class="px-4 py-2 text-sm border border-gray-300 rounded hover:bg-gray-50">Back</button>
        <button
          @click="currentStep++"
          :disabled="selectedServices.length === 0"
          class="px-4 py-2 text-sm bg-gray-900 text-white rounded hover:bg-gray-700 disabled:opacity-40 disabled:cursor-not-allowed"
        >
          Next
        </button>
      </div>
    </div>

    <!-- Dynamic service steps -->
    <div v-else-if="currentServiceConfig">
      <h2 class="text-lg font-semibold mb-1">Configure {{ currentServiceConfig.title }}</h2>
      <p class="text-gray-600 text-sm mb-6">{{ currentServiceConfig.description }}</p>

      <div class="bg-white rounded border border-gray-200 overflow-hidden mb-6">
        <table class="w-full text-sm">
          <tbody>
            <tr
              v-for="field in currentServiceConfig.fields"
              :key="field.label"
              class="border-b border-gray-100 last:border-b-0"
            >
              <td class="px-4 py-3 font-medium text-gray-700 bg-gray-50 w-36">{{ field.label }}</td>
              <td class="px-4 py-3 font-mono text-sm">
                <div class="flex items-center gap-2">
                  <span>{{ field.value }}</span>
                  <button
                    v-if="field.copyable"
                    @click="copyToClipboard(field.value)"
                    class="text-xs px-2 py-0.5 border border-gray-300 rounded hover:bg-gray-50 text-gray-500"
                    :title="'Copy ' + field.label"
                  >
                    {{ justCopied === field.value ? 'Copied!' : 'Copy' }}
                  </button>
                </div>
                <div v-if="field.note" class="text-xs text-gray-400 mt-1 font-sans">{{ field.note }}</div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="p-4 bg-blue-50 border border-blue-200 rounded text-sm text-blue-800 mb-6">
        After entering these values, use the <strong>Test</strong> button in {{ currentServiceConfig.title }} to verify the connection works.
      </div>

      <div class="flex gap-3">
        <button @click="currentStep--" class="px-4 py-2 text-sm border border-gray-300 rounded hover:bg-gray-50">Back</button>
        <button
          v-if="isLastStep"
          @click="$router.push('/')"
          class="px-4 py-2 text-sm bg-green-600 text-white rounded hover:bg-green-700"
        >
          Done
        </button>
        <button
          v-else
          @click="currentStep++"
          class="px-4 py-2 text-sm bg-gray-900 text-white rounded hover:bg-gray-700"
        >
          Next
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { getSetupHealth, type SetupHealthCheck } from '../api/setup'

const health = ref<SetupHealthCheck | null>(null)
const loading = ref(true)
const error = ref<string | null>(null)
const currentStep = ref(0)
const selectedServices = ref<string[]>([])
const justCopied = ref<string | null>(null)

const checkLabels: Record<string, string> = {
  apiKey: 'API Key',
  mediathekViewWeb: 'MediathekViewWeb',
  dataDirectory: 'Data Directory',
  downloadDirectory: 'Download Directory',
  indexerApi: 'Indexer API',
  downloadApi: 'Download API',
  ffmpeg: 'FFmpeg',
}

const fixHints: Record<string, string> = {
  apiKey: 'Set FunkArr__ApiKey environment variable or update appsettings.json.',
  mediathekViewWeb: 'Check your internet connection. MediathekViewWeb must be reachable.',
  dataDirectory: 'Ensure the data directory exists and is writable. Set FunkArr__DataPath if needed.',
  downloadDirectory: 'Ensure the download directory exists and is writable.',
  indexerApi: 'The Newznab indexer endpoint is not responding. Check application logs.',
  downloadApi: 'The SABnzbd download endpoint is not responding. Check application logs.',
}

interface ConfigField {
  label: string
  value: string
  copyable: boolean
  note?: string
}

interface ServiceConfig {
  title: string
  description: string
  fields: ConfigField[]
}

const apiKey = computed(() => health.value?.checks.apiKey?.value ?? '<your-api-key>')

function prowlarrConfig(): ServiceConfig {
  return {
    title: 'Prowlarr',
    description: 'Add FunkArr as a Custom Newznab indexer in Prowlarr: Settings > Indexers > Add > Newznab.',
    fields: [
      { label: 'Name', value: 'FunkArr', copyable: true },
      { label: 'URL', value: 'http://<funkarr-host>:<port>', copyable: false, note: 'Replace with your FunkArr address' },
      { label: 'API Path', value: '/index/api', copyable: true },
      { label: 'API Key', value: apiKey.value, copyable: true },
      { label: 'Categories', value: '5000 (TV), 2000 (Movies)', copyable: false },
    ],
  }
}

function sonarrConfig(): ServiceConfig {
  return {
    title: 'Sonarr',
    description: 'Add FunkArr as a SABnzbd download client in Sonarr: Settings > Download Clients > Add > SABnzbd.',
    fields: [
      { label: 'Name', value: 'FunkArr', copyable: true },
      { label: 'Host', value: '<funkarr-host>', copyable: false, note: 'Replace with your FunkArr hostname or IP' },
      { label: 'Port', value: '<funkarr-port>', copyable: false, note: 'Replace with your FunkArr port (default: 5000)' },
      { label: 'URL Base', value: '/download/api', copyable: true },
      { label: 'API Key', value: apiKey.value, copyable: true },
      { label: 'Category', value: 'tv', copyable: true },
    ],
  }
}

function radarrConfig(): ServiceConfig {
  return {
    title: 'Radarr',
    description: 'Add FunkArr as a SABnzbd download client in Radarr: Settings > Download Clients > Add > SABnzbd.',
    fields: [
      { label: 'Name', value: 'FunkArr', copyable: true },
      { label: 'Host', value: '<funkarr-host>', copyable: false, note: 'Replace with your FunkArr hostname or IP' },
      { label: 'Port', value: '<funkarr-port>', copyable: false, note: 'Replace with your FunkArr port (default: 5000)' },
      { label: 'URL Base', value: '/download/api', copyable: true },
      { label: 'API Key', value: apiKey.value, copyable: true },
      { label: 'Category', value: 'movies', copyable: true },
    ],
  }
}

const serviceConfigs: Record<string, () => ServiceConfig> = {
  prowlarr: prowlarrConfig,
  sonarr: sonarrConfig,
  radarr: radarrConfig,
}

const activeServiceSteps = computed(() =>
  selectedServices.value
    .filter((s) => s in serviceConfigs)
    .map((s) => serviceConfigs[s]()),
)

const stepLabels = computed(() => {
  const labels = ['Health Check', 'Services']
  for (const config of activeServiceSteps.value) {
    labels.push(config.title)
  }
  return labels
})

const currentServiceConfig = computed<ServiceConfig | null>(() => {
  const serviceIndex = currentStep.value - 2
  return activeServiceSteps.value[serviceIndex] ?? null
})

const isLastStep = computed(() => currentStep.value === stepLabels.value.length - 1)

const hasFailures = computed(() => {
  if (!health.value) return true
  return Object.values(health.value.checks).some((c) => c.status === 'fail')
})

async function runHealthCheck() {
  loading.value = true
  error.value = null
  try {
    health.value = await getSetupHealth()
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to run health check'
  } finally {
    loading.value = false
  }
}

async function copyToClipboard(text: string) {
  try {
    await navigator.clipboard.writeText(text)
    justCopied.value = text
    setTimeout(() => { justCopied.value = null }, 2000)
  } catch {
    // Clipboard not available
  }
}

onMounted(runHealthCheck)
</script>
