<template>
  <div class="flex flex-col">
    <!-- Header -->
    <div class="flex items-center justify-between mb-3">
      <div class="flex items-center gap-2">
        <span class="text-xs font-medium" :class="mode === 'live' ? 'text-text-secondary' : 'text-accent'">
          {{ mode === 'live' ? $t('preview.livePreview') : $t('preview.fullTestResults') }}
        </span>
        <span v-if="mode === 'live' && total > 0" class="text-xs text-text-body">
          {{ $t('preview.matchedCount', { matched: matchedCount, total }) }}
        </span>
        <span v-if="mode === 'fullTest' && fullTestResults" class="text-xs text-text-body">
          {{ $t('preview.matchedCount', { matched: fullTestResults.filter(r => r.matched).length, total: fullTestResults.length }) }}
        </span>
      </div>
      <button
        v-if="topic.trim()"
        class="text-xs text-text-secondary hover:text-text-body transition-colors"
        :disabled="fetchLoading"
        @click="refresh"
      >{{ fetchLoading ? '...' : '↻ ' + $t('preview.refresh') }}</button>
    </div>

    <!-- Fetch loading -->
    <div v-if="fetchLoading" class="text-center py-8">
      <div class="text-text-muted text-xs">{{ $t('preview.fetchingCandidates') }}</div>
    </div>

    <!-- Fetch error -->
    <div v-else-if="fetchError" class="text-center py-4">
      <p class="text-status-fail text-xs mb-2">{{ fetchError }}</p>
      <button class="text-xs text-accent hover:text-accent/80 transition-colors" @click="refresh">{{ $t('preview.retry') }}</button>
    </div>

    <!-- No topic -->
    <div v-else-if="!topic.trim()" class="text-center py-8">
      <p class="text-text-muted text-xs">{{ $t('preview.enterTopic') }}</p>
    </div>

    <!-- No candidates -->
    <div v-else-if="candidates.length === 0 && !fetchLoading" class="text-center py-8">
      <p class="text-text-muted text-xs">{{ $t('preview.noCandidatesFound', { topic }) }}</p>
    </div>

    <!-- Live Preview Mode -->
    <template v-else-if="mode === 'live'">
      <div v-if="rules.length === 0" class="text-center py-4 mb-2">
        <p class="text-text-muted text-xs">{{ $t('preview.addRulesToSeeMatches') }}</p>
      </div>

      <div class="max-h-[55vh] overflow-y-auto space-y-1 mb-3">
        <div
          v-for="(item, idx) in sortedLiveResults"
          :key="idx"
          class="rounded-md border text-xs p-2"
          :class="item.matchedRuleId
            ? 'border-l-4 border-l-status-ok border-border-default bg-surface-raised'
            : 'border-l-4 border-l-border-default border-border-default bg-surface-raised'"
        >
          <div class="flex items-baseline gap-2 mb-0.5">
            <span class="font-medium text-text-primary truncate">{{ item.candidate.title }}</span>
            <span
              class="px-1.5 py-0.5 rounded text-[11px] shrink-0"
              :class="item.matchedRuleId ? 'bg-status-ok/10 text-status-ok' : 'bg-surface-elevated text-text-secondary'"
            >{{ item.matchedRuleId ? $t('preview.matched') : $t('preview.noMatch') }}</span>
          </div>
          <div class="text-text-secondary">
            {{ item.candidate.channel }} &middot; {{ item.candidate.topic }} &middot;
            {{ Math.floor(item.candidate.duration / 60) }}min
            <template v-if="item.matchedRuleId">
              &middot; <span class="text-status-ok font-medium">{{ item.matchedRuleId }}</span>
              <template v-if="item.extractions.season">
                &middot; S<span class="font-mono">{{ item.extractions.season }}</span>
              </template>
              <template v-if="item.extractions.episode">
                E<span class="font-mono">{{ item.extractions.episode }}</span>
              </template>
              <template v-if="item.extractions.title">
                &middot; "<span class="font-mono">{{ item.extractions.title }}</span>"
              </template>
              <template v-if="item.extractions.airdate">
                &middot; <span class="font-mono">{{ item.extractions.airdate }}</span>
              </template>
            </template>
          </div>
        </div>
      </div>

      <div class="text-[11px] text-text-muted mb-2">{{ $t('preview.previewDisclaimer') }}</div>

      <button
        class="w-full px-3 py-1.5 rounded-md text-sm transition-colors"
        :class="candidates.length > 0 && rules.length > 0
          ? 'bg-accent text-surface-base font-medium hover:bg-accent/90'
          : 'bg-surface-elevated text-text-muted cursor-not-allowed border border-border-default'"
        :disabled="candidates.length === 0 || rules.length === 0 || testing"
        @click="runFullTest"
      >{{ testing ? (testPhase === 'enriching' ? $t('preview.enriching') : $t('preview.scoring')) : $t('preview.fullTestButton', { count: candidates.length }) }}</button>
    </template>

    <!-- Full Test Results Mode -->
    <template v-else-if="mode === 'fullTest' && fullTestResults">
      <div class="max-h-[55vh] overflow-y-auto space-y-2 mb-3">
        <div
          v-for="(item, idx) in sortedFullTestResults"
          :key="idx"
          class="bg-surface-raised rounded-lg border border-border-default text-sm"
        >
          <div class="p-3 cursor-pointer" @click="toggleExpand(idx)">
            <div class="flex items-baseline gap-2 mb-0.5">
              <span class="font-semibold text-text-body truncate">{{ item.candidateTitle }}</span>
              <span
                class="text-xs px-1.5 py-0.5 rounded shrink-0"
                :class="item.matched ? 'bg-surface-elevated text-status-ok' : 'bg-surface-elevated text-text-secondary'"
              >{{ item.matched ? $t('preview.matched') : $t('preview.noMatch') }}</span>
            </div>
            <div class="text-xs text-text-secondary">
              {{ item.candidateChannel }} &middot; {{ item.candidateTopic }} &middot;
              {{ Math.floor(item.candidateDuration / 60) }}min
              <template v-if="item.matched">
                &middot; <span class="text-status-ok font-medium">{{ item.matchedRuleId }}</span>
                &middot; score {{ item.score.toFixed(2) }}
              </template>
            </div>
          </div>

          <div v-if="expanded[idx]" class="border-t border-border-default px-3 pb-3 pt-2">
            <div class="text-xs font-semibold text-text-secondary mb-2">{{ $t('preview.rulePipeline') }}</div>
            <div class="space-y-2">
              <div
                v-for="(rt, ri) in item.ruleTraces"
                :key="ri"
                class="pl-3 border-l-2"
                :class="outcomeBorderClass(rt.outcome, item.matched && ri > item.ruleTraces.findIndex(r => r.outcome === 'matched'))"
              >
                <div class="flex items-center gap-2 text-xs mb-1">
                  <span class="font-mono text-text-body">{{ rt.ruleId }}</span>
                  <span class="text-text-secondary">prio {{ rt.priority }}</span>
                  <span
                    class="px-1.5 py-0.5 rounded text-[11px] font-medium"
                    :class="outcomeBadgeClass(rt.outcome, item.matched && ri > item.ruleTraces.findIndex(r => r.outcome === 'matched'))"
                  >{{ isSkipped(rt, item, ri) ? $t('preview.skipped') : outcomeLabel(rt.outcome) }}</span>
                </div>

                <div v-if="rt.filterTrace && !isSkipped(rt, item, ri)" class="ml-2 mb-1">
                  <FilterGroupTraceView :group="rt.filterTrace" />
                </div>

                <div v-if="rt.identificationTrace && !isSkipped(rt, item, ri)" class="ml-2 text-xs">
                  <div class="flex items-center gap-2">
                    <span class="text-text-secondary">{{ $t('preview.identification') }}:</span>
                    <span class="font-mono text-text-secondary">{{ strategyLabel(rt.identificationTrace.strategy ?? '', t) }}</span>
                  </div>
                  <div v-if="!rt.identificationTrace.attempted" class="text-text-secondary ml-4">{{ $t('preview.notAttempted') }}</div>
                  <div v-else-if="rt.identificationTrace.detail" class="text-status-fail ml-4">{{ rt.identificationTrace.detail }}</div>
                  <div v-else class="ml-4 text-status-ok">
                    <template v-if="item.identification">
                      <span v-if="item.identification.season" class="mr-2">{{ $t('detail.season') }}: <span class="font-mono">{{ item.identification.season }}</span></span>
                      <span v-if="item.identification.episode" class="mr-2">{{ $t('detail.episode') }}: <span class="font-mono">{{ item.identification.episode }}</span></span>
                      <span v-if="item.identification.title">Title: <span class="font-mono">{{ item.identification.title }}</span></span>
                    </template>
                    <span v-else>{{ $t('preview.ok') }}</span>
                  </div>
                </div>
              </div>
            </div>

            <!-- Enrichment Trace -->
            <div v-if="item.enrichmentTrace" class="mt-3 pt-2 border-t border-border-default">
              <div class="text-xs font-semibold text-text-secondary mb-1.5">{{ $t('preview.enrichmentResult') }}</div>
              <div
                class="pl-3 border-l-2 text-xs"
                :class="item.enrichmentTrace.enriched ? 'border-status-ok' : 'border-amber-500'"
              >
                <div class="flex items-center gap-2 mb-0.5">
                  <span
                    class="px-1.5 py-0.5 rounded text-[11px] font-medium"
                    :class="item.enrichmentTrace.enriched
                      ? (item.enrichmentTrace.method === 'RegexExtracted' ? 'bg-blue-500/10 text-blue-500' : 'bg-status-ok/10 text-status-ok')
                      : 'bg-amber-500/10 text-amber-500'"
                  >{{ item.enrichmentTrace.enriched
                      ? (item.enrichmentTrace.method === 'RegexExtracted' ? $t('preview.confirmedViaTvdb') : enrichmentMethodLabel(item.enrichmentTrace.method))
                      : $t('preview.notEnriched') }}</span>
                  <span v-if="item.enrichmentTrace.enriched" class="text-text-secondary">
                    {{ (item.enrichmentTrace.confidence * 100).toFixed(0) }}%
                  </span>
                </div>
                <div v-if="item.enrichmentTrace.enriched" class="text-status-ok">
                  <span v-if="item.enrichmentTrace.resolvedSeason" class="mr-2">S<span class="font-mono">{{ item.enrichmentTrace.resolvedSeason }}</span></span>
                  <span v-if="item.enrichmentTrace.resolvedEpisode" class="mr-2">E<span class="font-mono">{{ item.enrichmentTrace.resolvedEpisode }}</span></span>
                  <span v-if="item.enrichmentTrace.resolvedTitle" class="font-mono">"{{ item.enrichmentTrace.resolvedTitle }}"</span>
                  <span v-if="item.enrichmentTrace.resolvedYear" class="font-mono ml-2">({{ item.enrichmentTrace.resolvedYear }})</span>
                  <span v-if="item.enrichmentTrace.daysDiff != null" class="text-text-secondary ml-2">{{ $t('preview.daysDifference', { days: item.enrichmentTrace.daysDiff }) }}</span>
                </div>
                <div v-else-if="item.enrichmentTrace.detail" class="text-amber-500">
                  {{ item.enrichmentTrace.detail }}
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <button
        class="w-full px-3 py-1.5 rounded-md text-sm transition-colors bg-surface-elevated text-text-body border border-border-default hover:bg-surface-overlay"
        @click="mode = 'live'"
      >{{ $t('preview.backToLivePreview') }}</button>
    </template>

    <div v-if="testError" class="text-status-fail text-xs mt-2">{{ testError }}</div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch, toRef } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  testRuleSet,
  type TestCandidate,
  type ItemTrace,
  type TestEnrichmentParams,
} from '../api/rulesets'
import { useMediathekAutoFetch } from '../composables/useMediathekAutoFetch'
import { useRulesetMatcher } from '../composables/useRulesetMatcher'
import { strategyLabel } from '../utils/strategy'
import FilterGroupTraceView from './FilterGroupTraceView.vue'

const { t } = useI18n()

const props = defineProps<{
  topic: string
  builderState: {
    confidence: number
    rules: {
      id: string
      priority: number
      confidence: number | null
      strategy: string
      seasonRegex: string
      episodeRegex: string
      captureGroup: number | null
      filters: {
        all: { field: string; op: string; value: string }[]
        any: { field: string; op: string; value: string }[]
        not: { field: string; op: string; value: string }[]
      }
      titleRules: { type: string; field: string; pattern: string; captureGroup: number | null; value: string }[]
    }[]
    enrichment?: {
      enabled: boolean
      methods: string[]
      titleThreshold: number
      airdateTolerance: number
      runtimeTolerance: number
      runtimeMode: string
      yearTolerance: number
    }
    tvdbId?: number | null
    tmdbId?: number | null
    imdbId?: string
    mediaType?: string
  }
}>()

const topicRef = toRef(props, 'topic')
const rulesRef = toRef(props.builderState, 'rules')

const { candidates, loading: fetchLoading, error: fetchError, refresh } = useMediathekAutoFetch(topicRef)
const { results: liveResults, matchedCount, total } = useRulesetMatcher(candidates, rulesRef)

const mode = ref<'live' | 'fullTest'>('live')
const testing = ref(false)
const testPhase = ref<'scoring' | 'enriching'>('scoring')
const testError = ref<string | null>(null)
const fullTestResults = ref<ItemTrace[] | null>(null)
const expanded = reactive<Record<number, boolean>>({})
const rules = toRef(props.builderState, 'rules')

watch(rules, () => {
  if (mode.value === 'fullTest') {
    mode.value = 'live'
    fullTestResults.value = null
    Object.keys(expanded).forEach(k => delete expanded[Number(k)])
  }
}, { deep: true })

const sortedLiveResults = computed(() =>
  [...liveResults.value].sort((a, b) => {
    if (a.matchedRuleId && !b.matchedRuleId) return -1
    if (!a.matchedRuleId && b.matchedRuleId) return 1
    return 0
  })
)

const sortedFullTestResults = computed(() => {
  if (!fullTestResults.value) return []
  return [...fullTestResults.value].sort((a, b) => {
    if (a.matched && !b.matched) return -1
    if (!a.matched && b.matched) return 1
    return 0
  })
})

function hasFilters(filters: { all: unknown[]; any: unknown[]; not: unknown[] }): boolean {
  return filters.all.length > 0 || filters.any.length > 0 || filters.not.length > 0
}

async function runFullTest() {
  testing.value = true
  testPhase.value = 'scoring'
  testError.value = null
  Object.keys(expanded).forEach(k => delete expanded[Number(k)])

  const testCandidates: TestCandidate[] = candidates.value.map(item => ({
    title: item.title,
    topic: item.topic,
    channel: item.channel,
    duration: item.duration,
    quality: item.quality,
    description: item.description,
    timestamp: item.timestamp,
  }))

  const config = {
    defaultConfidence: props.builderState.confidence,
    rules: props.builderState.rules.map(r => ({
      id: r.id,
      priority: r.priority,
      confidence: r.confidence ?? undefined,
      strategy: r.strategy,
      seasonRegex: r.seasonRegex || undefined,
      episodeRegex: r.episodeRegex || undefined,
      captureGroup: r.captureGroup ?? undefined,
      filters: hasFilters(r.filters) ? {
        all: r.filters.all.length > 0 ? r.filters.all : undefined,
        any: r.filters.any.length > 0 ? r.filters.any : undefined,
        not: r.filters.not.length > 0 ? r.filters.not : undefined,
      } : undefined,
      titleRules: r.titleRules.length > 0 ? r.titleRules.map(tr => ({
        ...tr,
        captureGroup: tr.captureGroup ?? undefined,
      })) : undefined,
    })),
  }

  const enrichment = props.builderState.enrichment
  let enrichmentParams: TestEnrichmentParams | undefined
  if (enrichment?.enabled) {
    testPhase.value = 'enriching'
    enrichmentParams = {
      enrichment: {
        enabled: enrichment.enabled,
        methods: enrichment.methods,
        title: { threshold: enrichment.titleThreshold },
        airdate: { tolerance: enrichment.airdateTolerance },
        runtime: { tolerance: enrichment.runtimeTolerance, mode: enrichment.runtimeMode },
        year: { tolerance: enrichment.yearTolerance },
      },
      tvdbId: props.builderState.tvdbId ?? undefined,
      tmdbId: props.builderState.tmdbId ?? undefined,
      imdbId: props.builderState.imdbId || undefined,
      mediaType: props.builderState.mediaType,
    }
  }

  try {
    const response = await testRuleSet(config, testCandidates, enrichmentParams)
    fullTestResults.value = response.itemTraces
    mode.value = 'fullTest'
  } catch (e) {
    testError.value = e instanceof Error ? e.message : 'Test failed'
  } finally {
    testing.value = false
  }
}

function toggleExpand(idx: number) {
  expanded[idx] = !expanded[idx]
}

function isSkipped(_rt: ItemTrace['ruleTraces'][0], item: ItemTrace, ri: number): boolean {
  if (!item.matched) return false
  const matchIdx = item.ruleTraces.findIndex(r => r.outcome === 'matched')
  return matchIdx >= 0 && ri > matchIdx
}

function outcomeLabel(outcome: string): string {
  switch (outcome) {
    case 'matched': return t('preview.matched')
    case 'filterFailed': return t('preview.filterFailed')
    case 'identificationFailed': return t('preview.idFailed')
    default: return outcome
  }
}

function outcomeBorderClass(outcome: string, skipped: boolean): string {
  if (skipped) return 'border-border-default'
  switch (outcome) {
    case 'matched': return 'border-status-ok'
    case 'filterFailed': return 'border-status-fail'
    case 'identificationFailed': return 'border-amber-500'
    default: return 'border-border-default'
  }
}

function enrichmentMethodLabel(method: string): string {
  switch (method) {
    case 'TitleMatch': return t('preview.titleMatch')
    case 'AirdateMatch': return t('preview.airdateMatch')
    case 'YearMatch': return t('preview.yearMatch')
    case 'RegexExtracted': return t('preview.regexExtracted')
    default: return method
  }
}

function outcomeBadgeClass(outcome: string, skipped: boolean): string {
  if (skipped) return 'bg-surface-elevated text-text-secondary'
  switch (outcome) {
    case 'matched': return 'bg-status-ok/10 text-status-ok'
    case 'filterFailed': return 'bg-status-fail/10 text-status-fail'
    case 'identificationFailed': return 'bg-amber-500/10 text-amber-500'
    default: return 'bg-surface-elevated text-text-secondary'
  }
}
</script>
