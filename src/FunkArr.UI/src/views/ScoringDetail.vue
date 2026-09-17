<template>
  <div class="max-w-5xl mx-auto">
    <AppBreadcrumb :items="[
      { label: $t('rulesets.title'), to: '/rulesets' },
      { label: id, to: `/rulesets/${id}` },
      { label: $t('scoring.historyTitle'), to: `/rulesets/${id}/history` },
      { label: requestId.substring(0, 8) + '...' }
    ]" />

    <div v-if="loading" class="space-y-3">
      <SkeletonCard v-for="i in 3" :key="i" />
    </div>
    <div v-else-if="error" class="text-status-fail text-sm">{{ error }}</div>
    <div v-else-if="detail">
      <h1 class="text-xl font-semibold text-text-primary tracking-tight mb-2">{{ $t('scoring.detailTitle') }}</h1>
      <div class="text-sm text-text-secondary mb-6 flex items-center gap-3">
        <span>{{ $t('scoring.sourceColumn') }}: {{ detail.source }}</span>
        <span class="text-text-muted">|</span>
        <span>{{ $t('scoring.queryColumn') }}: {{ detail.query }}</span>
        <span class="text-text-muted">|</span>
        <span>{{ new Date(detail.timestamp).toLocaleString() }}</span>
      </div>

      <div class="flex items-center gap-2 mb-4">
        <button
          v-for="opt in filterOptions"
          :key="opt.value"
          @click="matchFilter = opt.value"
          class="px-2.5 py-1 rounded-md text-xs transition-colors"
          :class="matchFilter === opt.value
            ? 'bg-surface-elevated text-text-primary'
            : 'text-text-secondary hover:text-text-body'"
        >
          {{ opt.label }} ({{ opt.count }})
        </button>
      </div>

      <div class="grid gap-2.5">
        <div
          v-for="(item, idx) in filteredTraces"
          :key="idx"
          class="bg-surface-raised rounded-lg border border-border-default p-4 text-sm"
        >
          <div class="flex items-baseline gap-3 mb-1">
            <span class="font-semibold" :class="item.matched ? 'text-status-ok' : 'text-text-body'">
              {{ item.candidateTitle }}
            </span>
            <span
              class="text-xs px-1.5 py-0.5 rounded text-xs"
              :class="item.matched ? 'bg-surface-elevated text-status-ok' : 'bg-surface-elevated text-text-secondary'"
            >
              {{ item.matched ? $t('preview.matched') : $t('preview.noMatch') }}
            </span>
            <span class="text-text-secondary text-xs tabular-nums">score {{ item.score.toFixed(2) }}</span>
          </div>

          <div class="text-xs text-text-secondary mb-2">
            {{ item.candidateChannel }} &middot; {{ item.candidateTopic }} &middot;
            {{ Math.floor(item.candidateDuration / 60) }}min &middot;
            {{ item.candidateQuality }}p
            <span v-if="item.matchedRuleId" class="ml-2 text-status-ok font-medium">rule: {{ item.matchedRuleId }}</span>
          </div>

          <details class="mt-2 group">
            <summary class="text-xs text-text-secondary cursor-pointer hover:text-text-body transition-colors select-none">
              {{ $t('scoring.ruleTraces', { count: item.ruleTraces.length }) }}
            </summary>
            <div class="mt-2 space-y-2">
              <div
                v-for="(rt, ri) in item.ruleTraces"
                :key="ri"
                class="pl-3 border-l-2 text-xs"
                :class="{
                  'border-status-ok': rt.outcome === 'matched',
                  'border-status-fail': rt.outcome === 'filterFailed',
                  'border-border-default': rt.outcome !== 'matched' && rt.outcome !== 'filterFailed'
                }"
              >
                <div class="flex gap-2 items-center">
                  <span class="font-mono text-text-body">{{ rt.ruleId }}</span>
                  <span class="text-text-secondary">prio {{ rt.priority }}</span>
                  <span
                    class="font-medium"
                    :class="{
                      'text-status-ok': rt.outcome === 'matched',
                      'text-status-fail': rt.outcome === 'filterFailed',
                      'text-text-secondary': rt.outcome !== 'matched' && rt.outcome !== 'filterFailed'
                    }"
                  >{{ outcomeLabel(rt.outcome) }}</span>
                </div>
                <FilterGroupTraceView v-if="rt.filterTrace" :group="rt.filterTrace" class="mt-1" />
                <div v-if="rt.identificationTrace" class="text-xs text-text-secondary mt-1 bg-surface-elevated/50 rounded p-2 space-y-0.5">
                  <div><span class="text-text-secondary">{{ $t('preview.identification') }}:</span> {{ strategyLabel(rt.identificationTrace.strategy ?? '', t) }}</div>
                  <div><span class="text-text-secondary">Attempted:</span> {{ rt.identificationTrace.attempted }}</div>
                  <div v-if="rt.identificationTrace.detail"><span class="text-text-secondary">Detail:</span> {{ rt.identificationTrace.detail }}</div>
                </div>
              </div>
            </div>
          </details>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { getScoringDetail, type ScoringDetail } from '../api/rulesets'
import SkeletonCard from '../components/SkeletonCard.vue'
import AppBreadcrumb from '../components/AppBreadcrumb.vue'
import { strategyLabel } from '../utils/strategy'
import FilterGroupTraceView from '../components/FilterGroupTraceView.vue'

const { t } = useI18n()

const route = useRoute()
const id = route.params.id as string
const requestId = route.params.requestId as string

const detail = ref<ScoringDetail | null>(null)
const loading = ref(true)
const error = ref<string | null>(null)
const matchFilter = ref<'all' | 'matched' | 'unmatched'>('all')

const filteredTraces = computed(() => {
  if (!detail.value) return []
  if (matchFilter.value === 'all') return detail.value.itemTraces
  const wantMatched = matchFilter.value === 'matched'
  return detail.value.itemTraces.filter(t => t.matched === wantMatched)
})

const filterOptions = computed(() => {
  const traces = detail.value?.itemTraces ?? []
  const matchedCount = traces.filter(t => t.matched).length
  return [
    { label: t('scoring.allFilter'), value: 'all' as const, count: traces.length },
    { label: t('scoring.matchedFilter'), value: 'matched' as const, count: matchedCount },
    { label: t('scoring.unmatchedFilter'), value: 'unmatched' as const, count: traces.length - matchedCount },
  ]
})

function outcomeLabel(outcome: string): string {
  switch (outcome) {
    case 'matched': return t('preview.matched')
    case 'filterFailed': return t('preview.filterFailed')
    case 'identificationFailed': return t('preview.idFailed')
    default: return outcome
  }
}

onMounted(async () => {
  try {
    detail.value = await getScoringDetail(id, requestId)
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to load scoring detail'
  } finally {
    loading.value = false
  }
})
</script>
