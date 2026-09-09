<template>
  <div class="max-w-5xl mx-auto">
    <AppBreadcrumb :items="[
      { label: 'RuleSets', to: '/rulesets' },
      { label: id, to: `/rulesets/${id}` },
      { label: 'History', to: `/rulesets/${id}/history` },
      { label: requestId.substring(0, 8) + '...' }
    ]" />

    <div v-if="loading" class="space-y-3">
      <SkeletonCard v-for="i in 3" :key="i" />
    </div>
    <div v-else-if="error" class="text-status-fail text-sm">{{ error }}</div>
    <div v-else-if="detail">
      <h1 class="text-lg font-medium text-text-primary mb-2">Scoring Detail</h1>
      <div class="text-sm text-text-secondary mb-6 flex items-center gap-3">
        <span>Source: {{ detail.source }}</span>
        <span class="text-text-muted">|</span>
        <span>Query: {{ detail.query }}</span>
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
              :class="item.matched ? 'bg-surface-elevated text-status-ok' : 'bg-surface-elevated text-text-muted'"
            >
              {{ item.matched ? 'matched' : 'no match' }}
            </span>
            <span class="text-text-muted text-xs tabular-nums">score {{ item.score.toFixed(2) }}</span>
          </div>

          <div class="text-xs text-text-muted mb-2">
            {{ item.candidateChannel }} &middot; {{ item.candidateTopic }} &middot;
            {{ Math.floor(item.candidateDuration / 60) }}min &middot;
            {{ item.candidateQuality }}p
            <span v-if="item.matchedRuleId" class="ml-2 text-status-ok font-medium">rule: {{ item.matchedRuleId }}</span>
          </div>

          <details class="mt-2 group">
            <summary class="text-xs text-text-muted cursor-pointer hover:text-text-secondary transition-colors select-none">
              {{ item.ruleTraces.length }} rule trace(s)
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
                  <span class="text-text-muted">prio {{ rt.priority }}</span>
                  <span
                    class="font-medium"
                    :class="{
                      'text-status-ok': rt.outcome === 'matched',
                      'text-status-fail': rt.outcome === 'filterFailed',
                      'text-text-muted': rt.outcome !== 'matched' && rt.outcome !== 'filterFailed'
                    }"
                  >{{ rt.outcome }}</span>
                </div>
                <FilterGroupTraceView v-if="rt.filterTrace" :group="rt.filterTrace" class="mt-1" />
                <div v-if="rt.identificationTrace" class="text-xs text-text-muted mt-1 bg-surface-elevated/50 rounded p-2 space-y-0.5">
                  <div><span class="text-text-secondary">Strategy:</span> {{ rt.identificationTrace.strategy }}</div>
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
import { getScoringDetail, type ScoringDetail } from '../api/rulesets'
import SkeletonCard from '../components/SkeletonCard.vue'
import AppBreadcrumb from '../components/AppBreadcrumb.vue'
import FilterGroupTraceView from '../components/FilterGroupTraceView.vue'

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
    { label: 'All', value: 'all' as const, count: traces.length },
    { label: 'Matched', value: 'matched' as const, count: matchedCount },
    { label: 'Unmatched', value: 'unmatched' as const, count: traces.length - matchedCount },
  ]
})

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
