<template>
  <div>
    <div class="mb-4 text-sm text-text-muted flex items-center gap-1.5">
      <router-link to="/rulesets" class="hover:text-text-secondary transition-colors">RuleSets</router-link>
      <svg class="w-3.5 h-3.5" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M6 4l4 4-4 4"/></svg>
      <router-link :to="`/rulesets/${id}`" class="hover:text-text-secondary transition-colors">{{ id }}</router-link>
      <svg class="w-3.5 h-3.5" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M6 4l4 4-4 4"/></svg>
      <router-link :to="`/rulesets/${id}/history`" class="hover:text-text-secondary transition-colors">History</router-link>
      <svg class="w-3.5 h-3.5" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M6 4l4 4-4 4"/></svg>
      <span class="font-mono text-text-secondary">{{ requestId.substring(0, 8) }}...</span>
    </div>

    <div v-if="loading" class="text-text-muted text-sm">Loading...</div>
    <div v-else-if="error" class="text-status-fail text-sm">{{ error }}</div>
    <div v-else-if="detail">
      <h1 class="text-2xl font-bold text-text-primary tracking-tight mb-2">Scoring Detail</h1>
      <div class="text-sm text-text-secondary mb-6 flex items-center gap-3">
        <span>Source: {{ detail.source }}</span>
        <span class="text-text-muted">|</span>
        <span>Query: {{ detail.query }}</span>
        <span class="text-text-muted">|</span>
        <span>{{ new Date(detail.timestamp).toLocaleString() }}</span>
      </div>

      <div class="grid gap-2.5">
        <div
          v-for="(item, idx) in detail.itemTraces"
          :key="idx"
          class="bg-surface-raised rounded-xl border-l-2 border border-border-default p-4 text-sm"
          :class="item.matched ? 'border-l-status-ok' : 'border-l-surface-elevated'"
        >
          <div class="flex items-baseline gap-3 mb-1">
            <span class="font-semibold" :class="item.matched ? 'text-status-ok' : 'text-text-body'">
              {{ item.candidateTitle }}
            </span>
            <span
              class="text-xs px-2 py-0.5 rounded-full font-medium"
              :class="item.matched ? 'bg-status-ok/10 text-status-ok' : 'bg-surface-elevated text-text-muted'"
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
                  <span class="font-mono text-brand-400">{{ rt.ruleId }}</span>
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
                <pre v-if="rt.filterTrace" class="text-text-muted mt-1 whitespace-pre-wrap font-mono text-[11px] bg-surface-elevated/50 rounded p-2">{{ JSON.stringify(rt.filterTrace, null, 2) }}</pre>
                <pre v-if="rt.identificationTrace" class="text-text-muted mt-1 whitespace-pre-wrap font-mono text-[11px] bg-surface-elevated/50 rounded p-2">{{ JSON.stringify(rt.identificationTrace, null, 2) }}</pre>
              </div>
            </div>
          </details>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { getScoringDetail, type ScoringDetail } from '../api/rulesets'

const route = useRoute()
const id = route.params.id as string
const requestId = route.params.requestId as string

const detail = ref<ScoringDetail | null>(null)
const loading = ref(true)
const error = ref<string | null>(null)

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
