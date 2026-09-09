<template>
  <div class="max-w-2xl mx-auto">
    <h1 class="text-lg font-medium text-text-primary mb-5">Setup Guide</h1>

    <div class="flex items-center gap-1 mb-6">
      <div
        v-for="(label, i) in stepLabels"
        :key="i"
        class="flex items-center gap-1"
      >
        <button
          @click="i < currentStep ? currentStep = i : null"
          class="flex items-center gap-1.5 px-2.5 py-1 rounded-md text-xs transition-colors"
          :class="i < currentStep
            ? 'text-status-ok cursor-pointer hover:bg-surface-elevated'
            : i === currentStep
              ? 'bg-surface-elevated text-text-primary'
              : 'text-text-muted cursor-default'"
        >
          <span class="w-4 h-4 rounded-full flex items-center justify-center text-[9px] font-medium" :class="i < currentStep ? 'bg-status-ok/20 text-status-ok' : i === currentStep ? 'bg-surface-overlay text-text-body' : 'bg-surface-elevated text-text-muted'">
            {{ i < currentStep ? '&#x2713;' : i + 1 }}
          </span>
          <span class="hidden sm:inline">{{ label }}</span>
        </button>
        <svg v-if="i < stepLabels.length - 1" class="w-3.5 h-3.5 text-text-muted" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M6 4l4 4-4 4"/></svg>
      </div>
    </div>

    <div v-if="currentStep === 0">
      <h2 class="text-sm font-medium text-text-primary mb-3">System Health Check</h2>

      <div v-if="loading" class="space-y-2">
        <SkeletonCard v-for="i in 4" :key="i" />
      </div>
      <div v-else-if="error" class="text-status-fail mb-4">{{ error }}</div>

      <div v-if="health" class="space-y-2 mb-5">
        <div
          v-for="(result, name) in health.checks"
          :key="name"
          class="flex items-start gap-3 p-3 rounded-lg bg-surface-raised border border-border-default"
        >
          <span
            class="w-1.5 h-1.5 rounded-full shrink-0 mt-1.5"
            :class="{
              'bg-status-ok': result.status === 'ok',
              'bg-status-warn': result.status === 'warn',
              'bg-status-fail': result.status === 'fail',
            }"
          />
          <div class="min-w-0">
            <div class="text-sm text-text-body">{{ checkLabels[name] ?? name }}</div>
            <div v-if="result.message" class="text-xs text-text-secondary mt-0.5">{{ result.message }}</div>
            <div v-if="result.path" class="text-xs text-text-secondary mt-0.5 font-mono truncate">{{ result.path }}</div>
            <div v-if="result.version" class="text-xs text-text-secondary mt-0.5">Version: {{ result.version }}</div>
            <div v-if="result.status === 'fail'" class="text-xs text-status-fail mt-1">
              {{ fixHints[name] ?? 'Check your configuration and try again.' }}
            </div>
          </div>
        </div>
      </div>

      <div class="flex gap-2">
        <button
          @click="runHealthCheck"
          class="px-3 py-1.5 text-sm bg-surface-elevated border border-border-default rounded-md hover:bg-surface-overlay text-text-secondary transition-colors"
        >
          Re-check
        </button>
        <button
          @click="currentStep++"
          :disabled="hasFailures"
          class="px-3 py-1.5 text-sm bg-surface-elevated border border-border-default rounded-md text-text-body hover:bg-surface-overlay disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
        >
          Next
        </button>
      </div>
    </div>

    <div v-else-if="currentStep === 1">
      <h2 class="text-sm font-medium text-text-primary mb-3">Select Services to Configure</h2>
      <p class="text-text-secondary text-sm mb-4">Choose which *arr applications you want to set up with FunkArr.</p>

      <div class="space-y-2 mb-5">
        <label
          v-for="svc in services"
          :key="svc.value"
          class="flex items-start gap-3 p-3 rounded-lg bg-surface-raised border cursor-pointer transition-colors"
          :class="selectedServices.includes(svc.value) ? 'border-border-focus' : 'border-border-default hover:bg-surface-elevated'"
        >
          <input type="checkbox" v-model="selectedServices" :value="svc.value" class="mt-0.5 accent-brand-500" />
          <div>
            <div class="text-sm text-text-body">{{ svc.title }}</div>
            <div class="text-xs text-text-secondary">{{ svc.description }}</div>
          </div>
        </label>
      </div>

      <div class="flex gap-2">
        <button @click="currentStep--" class="px-3 py-1.5 text-sm bg-surface-elevated border border-border-default rounded-md hover:bg-surface-overlay text-text-secondary transition-colors">Back</button>
        <button
          @click="currentStep++"
          :disabled="selectedServices.length === 0"
          class="px-3 py-1.5 text-sm bg-surface-elevated border border-border-default rounded-md text-text-body hover:bg-surface-overlay disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
        >
          Next
        </button>
      </div>
    </div>

    <div v-else-if="currentServiceConfig">
      <h2 class="text-sm font-medium text-text-primary mb-1">Configure {{ currentServiceConfig.title }}</h2>
      <p class="text-text-secondary text-sm mb-4">{{ currentServiceConfig.description }}</p>

      <div class="bg-surface-raised rounded-lg border border-border-default overflow-hidden mb-4">
        <table class="w-full text-sm">
          <tbody>
            <tr
              v-for="field in currentServiceConfig.fields"
              :key="field.label"
              class="border-b border-border-subtle last:border-b-0"
            >
              <td class="px-4 py-2.5 text-text-secondary bg-surface-elevated/30 w-32 text-xs">{{ field.label }}</td>
              <td class="px-4 py-2.5 font-mono text-sm text-text-body">
                <div class="flex items-center gap-2">
                  <span>{{ field.value }}</span>
                  <button
                    v-if="field.copyable"
                    @click="copyToClipboard(field.value)"
                    class="text-[11px] px-1.5 py-0.5 border border-border-default rounded hover:bg-surface-elevated text-text-secondary transition-colors"
                    :title="'Copy ' + field.label"
                  >
                    {{ justCopied === field.value ? 'Copied' : 'Copy' }}
                  </button>
                </div>
                <div v-if="field.note" class="text-xs text-text-secondary mt-0.5 font-sans">{{ field.note }}</div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="p-3 bg-surface-elevated border border-border-default rounded-lg text-xs text-text-secondary mb-4">
        After entering these values, use the <strong class="text-text-body">Test</strong> button in {{ currentServiceConfig.title }} to verify the connection works.
      </div>

      <div v-if="currentServiceConfig?.title === 'Sonarr'" class="p-3 bg-surface-elevated border border-border-default rounded-lg text-xs text-text-secondary mb-4">
        <strong class="text-text-body">Tip:</strong> Shows identified by air date (e.g., Die Sendung mit der Maus, Bibi Blocksberg) need <strong class="text-text-body">Series Type: Daily</strong> in Sonarr for automatic import.
      </div>

      <div class="flex gap-2">
        <button @click="currentStep--" class="px-3 py-1.5 text-sm bg-surface-elevated border border-border-default rounded-md hover:bg-surface-overlay text-text-secondary transition-colors">Back</button>
        <button
          v-if="isLastStep"
          @click="$router.push('/')"
          class="px-3 py-1.5 text-sm bg-surface-elevated border border-border-default rounded-md text-text-body hover:bg-surface-overlay transition-colors"
        >
          Done
        </button>
        <button
          v-else
          @click="currentStep++"
          class="px-3 py-1.5 text-sm bg-surface-elevated border border-border-default rounded-md text-text-body hover:bg-surface-overlay transition-colors"
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
import { useToast } from '../composables/useToast'

const { toast } = useToast()
import SkeletonCard from '../components/SkeletonCard.vue'

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
  completeDirectory: 'Complete Directory',
  incompleteDirectory: 'Incomplete Directory',
  indexerApi: 'Indexer API',
  downloadApi: 'Download API',
  ffmpeg: 'FFmpeg',
}

const fixHints: Record<string, string> = {
  apiKey: 'Set FunkArr__ApiKey environment variable or update appsettings.json.',
  mediathekViewWeb: 'Check your internet connection. MediathekViewWeb must be reachable.',
  dataDirectory: 'Ensure the data directory exists and is writable. Set FunkArr__DataPath if needed.',
  completeDirectory: 'Ensure the complete download directory exists and is writable.',
  incompleteDirectory: 'Ensure the incomplete download directory exists and is writable.',
  indexerApi: 'The Newznab indexer endpoint is not responding. Check application logs.',
  downloadApi: 'The SABnzbd download endpoint is not responding. Check application logs.',
}

const services = [
  { value: 'prowlarr', title: 'Prowlarr', description: 'Indexer Manager - adds FunkArr as a Newznab indexer source' },
  { value: 'sonarr', title: 'Sonarr', description: 'TV Series - adds FunkArr as a SABnzbd download client' },
  { value: 'radarr', title: 'Radarr', description: 'Movies - adds FunkArr as a SABnzbd download client' },
]

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
const defaultPort = computed(() => health.value?.setupConnectionInfo?.defaultPort ?? 6969)
const actualHost = computed(() => window.location.hostname || 'localhost')
const actualPort = computed(() => window.location.port || defaultPort.value.toString())

function prowlarrConfig(): ServiceConfig {
  return {
    title: 'Prowlarr',
    description: 'Add FunkArr as a Custom Newznab indexer in Prowlarr: Settings > Indexers > Add > Newznab.',
    fields: [
      { label: 'Name', value: 'FunkArr', copyable: true },
      { label: 'URL', value: `http://${actualHost.value}:${actualPort.value}`, copyable: true, note: 'Adjust if using a reverse proxy' },
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
      { label: 'Host', value: actualHost.value, copyable: true, note: 'Adjust if using a reverse proxy' },
      { label: 'Port', value: actualPort.value, copyable: true, note: 'Adjust if using a reverse proxy' },
      { label: 'URL Base', value: '/download', copyable: true },
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
      { label: 'Host', value: actualHost.value, copyable: true, note: 'Adjust if using a reverse proxy' },
      { label: 'Port', value: actualPort.value, copyable: true, note: 'Adjust if using a reverse proxy' },
      { label: 'URL Base', value: '/download', copyable: true },
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
    toast('Copied to clipboard')
    justCopied.value = text
    setTimeout(() => { justCopied.value = null }, 2000)
  } catch {
    // Clipboard not available
  }
}

onMounted(runHealthCheck)
</script>
