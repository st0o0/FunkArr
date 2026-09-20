<template>
  <div class="max-w-3xl mx-auto">
    <h1 class="text-xl font-semibold text-text-primary tracking-tight mb-5">{{ $t('setup.title') }}</h1>

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
      <h2 class="text-sm font-medium text-text-primary mb-3">{{ $t('setup.systemHealthCheck') }}</h2>

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
              'bg-status-ok': result.status === CheckStatus.Ok,
              'bg-status-warn': result.status === CheckStatus.Warn,
              'bg-status-fail': result.status === CheckStatus.Fail,
            }"
          />
          <div class="min-w-0">
            <div class="text-sm text-text-body">{{ getCheckLabel(name as string) }}</div>
            <div v-if="result.message" class="text-xs text-text-secondary mt-0.5">{{ result.message }}</div>
            <div v-if="result.path" class="text-xs text-text-secondary mt-0.5 font-mono truncate">{{ result.path }}</div>
            <div v-if="result.version" class="text-xs text-text-secondary mt-0.5">{{ $t('setup.version') }}: {{ result.version }}</div>
            <div v-if="result.status === CheckStatus.Fail" class="text-xs text-status-fail mt-1">
              {{ getFixHint(name as string) }}
            </div>
          </div>
        </div>
      </div>

      <div class="flex gap-2">
        <button
          @click="runHealthCheck"
          class="px-3 py-1.5 text-sm bg-surface-elevated border border-border-default rounded-md hover:bg-surface-overlay text-text-body transition-colors"
        >
          {{ $t('setup.recheck') }}
        </button>
        <button
          @click="currentStep++"
          :disabled="hasFailures"
          class="px-3 py-1.5 text-sm bg-accent text-black font-medium rounded-md hover:bg-accent-dim disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
        >
          {{ $t('setup.next') }}
        </button>
      </div>
    </div>

    <div v-else-if="currentStep === 1">
      <h2 class="text-sm font-medium text-text-primary mb-3">{{ $t('setup.selectServices') }}</h2>
      <p class="text-text-secondary text-sm mb-4">{{ $t('setup.selectServicesHint') }}</p>

      <div class="space-y-2 mb-5">
        <label
          v-for="svc in services"
          :key="svc.value"
          class="flex items-start gap-3 p-3 rounded-lg bg-surface-raised border cursor-pointer transition-colors"
          :class="selectedServices.includes(svc.value) ? 'border-border-focus' : 'border-border-default hover:bg-surface-elevated'"
        >
          <input type="checkbox" v-model="selectedServices" :value="svc.value" class="mt-0.5 accent-accent" />
          <div>
            <div class="text-sm text-text-body">{{ svc.title }}</div>
            <div class="text-xs text-text-secondary">{{ svc.description }}</div>
          </div>
        </label>
      </div>

      <div class="flex gap-2">
        <button @click="currentStep--" class="px-3 py-1.5 text-sm bg-surface-elevated border border-border-default rounded-md hover:bg-surface-overlay text-text-body transition-colors">{{ $t('setup.back') }}</button>
        <button
          @click="currentStep++"
          :disabled="selectedServices.length === 0"
          class="px-3 py-1.5 text-sm bg-accent text-black font-medium rounded-md hover:bg-accent-dim disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
        >
          {{ $t('setup.next') }}
        </button>
      </div>
    </div>

    <div v-else-if="currentServiceConfig">
      <h2 class="text-sm font-medium text-text-primary mb-1">{{ $t('setup.configureService', { service: currentServiceConfig.title }) }}</h2>
      <p class="text-text-secondary text-sm mb-4">{{ currentServiceConfig.description }}</p>

      <div class="p-3 bg-amber-500/10 border border-amber-500/30 rounded-lg text-xs text-text-body mb-4" v-html="$t('setup.advancedSettingsHint', { service: currentServiceConfig.title })">
      </div>

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
                    @click="handleCopy(field.value)"
                    class="text-xs px-1.5 py-0.5 border border-border-default rounded hover:bg-surface-elevated text-text-secondary transition-colors"
                    :title="`${$t('setup.copy')} ${field.label}`"
                  >
                    {{ hasCopied(field.value) ? $t('setup.copied') : $t('setup.copy') }}
                  </button>
                </div>
                <div v-if="field.note" class="text-xs text-text-secondary mt-0.5 font-sans">{{ field.note }}</div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="p-3 bg-surface-elevated border border-border-default rounded-lg text-xs text-text-secondary mb-4" v-html="$t('setup.testConnectionHint', { service: currentServiceConfig.title })">
      </div>

      <div v-if="currentServiceConfig?.title === 'Sonarr'" class="p-3 bg-surface-elevated border border-border-default rounded-lg text-xs text-text-secondary mb-4" v-html="$t('setup.sonarrDailyTip')">
      </div>

      <div class="flex gap-2">
        <button @click="currentStep--" class="px-3 py-1.5 text-sm bg-surface-elevated border border-border-default rounded-md hover:bg-surface-overlay text-text-body transition-colors">{{ $t('setup.back') }}</button>
        <button
          v-if="isLastStep"
          @click="$router.push('/')"
          class="px-3 py-1.5 text-sm bg-accent text-black font-medium rounded-md hover:bg-accent-dim transition-colors"
        >
          {{ $t('setup.done') }}
        </button>
        <button
          v-else
          @click="currentStep++"
          class="px-3 py-1.5 text-sm bg-accent text-black font-medium rounded-md hover:bg-accent-dim transition-colors"
        >
          {{ $t('setup.next') }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { getSetupHealth, type SetupHealthCheck } from '../api/setup'
import { CheckStatus } from '../api/enums'
import { useToast } from '../composables/useToast'
import { useClipboard } from '../composables/useClipboard'

const { t } = useI18n()
const { toast } = useToast()
const { copy, hasCopied } = useClipboard()
import SkeletonCard from '../components/SkeletonCard.vue'

const health = ref<SetupHealthCheck | null>(null)
const loading = ref(true)
const error = ref<string | null>(null)
const currentStep = ref(0)
const selectedServices = ref<string[]>([])

const healthLabelKeys: Record<string, string> = {
  apiKey: 'health.apiKey',
  mediathekViewWeb: 'health.mediathekViewWeb',
  dataDirectory: 'health.dataDirectory',
  completeDirectory: 'health.completeDirectory',
  incompleteDirectory: 'health.incompleteDirectory',
  indexerApi: 'health.indexerApi',
  downloadApi: 'health.downloadApi',
  ffmpeg: 'health.ffmpeg',
}

function getCheckLabel(name: string): string {
  const key = healthLabelKeys[name]
  return key ? t(key) : name
}

const fixHintKeys: Record<string, string> = {
  apiKey: 'setup.fixApiKey',
  mediathekViewWeb: 'setup.fixMediathekViewWeb',
  dataDirectory: 'setup.fixDataDirectory',
  completeDirectory: 'setup.fixCompleteDirectory',
  incompleteDirectory: 'setup.fixIncompleteDirectory',
  indexerApi: 'setup.fixIndexerApi',
  downloadApi: 'setup.fixDownloadApi',
}

function getFixHint(name: string): string {
  const key = fixHintKeys[name]
  return key ? t(key) : t('setup.fixDefault')
}

const services = computed(() => [
  { value: 'prowlarr', title: 'Prowlarr', description: t('setup.prowlarrDescription') },
  { value: 'sonarr', title: 'Sonarr', description: t('setup.sonarrDescription') },
  { value: 'radarr', title: 'Radarr', description: t('setup.radarrDescription') },
])

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
    description: t('setup.prowlarrConfigDescription'),
    fields: [
      { label: 'Name', value: 'FunkArr', copyable: true },
      { label: 'URL', value: `http://${actualHost.value}:${actualPort.value}`, copyable: true, note: t('setup.adjustProxy') },
      { label: 'API Path', value: '/index/api', copyable: true },
      { label: 'API Key', value: apiKey.value, copyable: true },
      { label: 'Categories', value: '5000 (TV), 2000 (Movies)', copyable: false },
    ],
  }
}

function sonarrConfig(): ServiceConfig {
  return {
    title: 'Sonarr',
    description: t('setup.sonarrConfigDescription'),
    fields: [
      { label: 'Name', value: 'FunkArr', copyable: true },
      { label: 'Host', value: actualHost.value, copyable: true, note: t('setup.adjustProxy') },
      { label: 'Port', value: actualPort.value, copyable: true, note: t('setup.adjustProxy') },
      { label: 'URL Base', value: '/download', copyable: true },
      { label: 'API Key', value: apiKey.value, copyable: true },
      { label: 'Category', value: 'tv', copyable: true },
    ],
  }
}

function radarrConfig(): ServiceConfig {
  return {
    title: 'Radarr',
    description: t('setup.radarrConfigDescription'),
    fields: [
      { label: 'Name', value: 'FunkArr', copyable: true },
      { label: 'Host', value: actualHost.value, copyable: true, note: t('setup.adjustProxy') },
      { label: 'Port', value: actualPort.value, copyable: true, note: t('setup.adjustProxy') },
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
  const labels = [t('setup.healthCheck'), t('setup.services')]
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
  return Object.values(health.value.checks).some((c) => c.status === CheckStatus.Fail)
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

async function handleCopy(text: string) {
  const ok = await copy(text)
  toast(ok ? t('setup.copiedToClipboard') : t('setup.copy'))
}

onMounted(runHealthCheck)
</script>
