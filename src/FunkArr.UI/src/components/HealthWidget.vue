<template>
  <div class="bg-surface-raised rounded-lg border border-border-default">
    <div class="flex items-center justify-between px-4 py-3 border-b border-border-subtle">
      <h2 class="text-sm font-medium text-text-primary">{{ $t('health.systemHealth') }}</h2>
      <router-link to="/setup" class="text-xs text-accent hover:text-accent/80 transition-colors">{{ $t('nav.setup') }}</router-link>
    </div>

    <div class="p-4">
      <div v-if="loading && !health" class="space-y-2.5">
        <SkeletonLine width="1/2" />
        <SkeletonLine width="3/4" />
        <SkeletonLine width="1/2" />
        <SkeletonLine width="3/4" />
      </div>
      <div v-else-if="error" class="text-status-fail text-sm">{{ error }}</div>

      <div v-else-if="health" class="space-y-3">
        <div class="grid grid-cols-2 gap-2">
          <div
            v-for="(result, name) in health.checks"
            :key="name"
            class="flex items-center gap-2 px-2.5 py-1.5 rounded-md"
          >
            <span
              class="w-1.5 h-1.5 rounded-full shrink-0"
              :class="{
                'bg-status-ok': result.status === CheckStatus.Ok,
                'bg-status-warn': result.status === CheckStatus.Warn,
                'bg-status-fail': result.status === CheckStatus.Fail,
              }"
            />
            <span class="text-xs text-text-secondary">{{ labels[name] ?? name }}</span>
          </div>
        </div>

        <div
          v-if="failedChecks.length > 0 || warnChecks.length > 0"
          class="space-y-1.5 pt-1 border-t border-border-subtle"
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
import { useI18n } from 'vue-i18n'
import SkeletonLine from './SkeletonLine.vue'
import { getSetupHealth, type SetupHealthCheck, type CheckResult } from '../api/setup'
import { CheckStatus } from '../api/enums'

const { t } = useI18n()

const health = ref<SetupHealthCheck | null>(null)
const loading = ref(true)
const error = ref<string | null>(null)
let interval: ReturnType<typeof setInterval> | null = null

const labelKeys: Record<string, string> = {
  apiKey: 'health.apiKey',
  mediathekViewWeb: 'health.mediathekViewWeb',
  dataDirectory: 'health.dataDirectory',
  completeDirectory: 'health.completeDirectory',
  incompleteDirectory: 'health.incompleteDirectory',
  indexerApi: 'health.indexerApi',
  downloadApi: 'health.downloadApi',
  ffmpeg: 'health.ffmpeg',
}

const labels = computed<Record<string, string>>(() => {
  const result: Record<string, string> = {}
  for (const [key, i18nKey] of Object.entries(labelKeys)) {
    result[key] = t(i18nKey)
  }
  return result
})

interface NamedCheck {
  name: string
  result: CheckResult
}

const failedChecks = computed<NamedCheck[]>(() => {
  if (!health.value) return []
  return Object.entries(health.value.checks)
    .filter(([, r]) => r.status === CheckStatus.Fail)
    .map(([name, result]) => ({ name, result }))
})

const warnChecks = computed<NamedCheck[]>(() => {
  if (!health.value) return []
  return Object.entries(health.value.checks)
    .filter(([, r]) => r.status === CheckStatus.Warn)
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
