<template>
  <div>
    <div class="mb-4 text-sm text-gray-500">
      <router-link to="/rulesets" class="hover:text-gray-700">RuleSets</router-link>
      <span class="mx-1">&gt;</span>
      <router-link :to="`/rulesets/${id}`" class="hover:text-gray-700">{{ id }}</router-link>
      <span class="mx-1">&gt;</span>
      <router-link :to="`/rulesets/${id}/history`" class="hover:text-gray-700">History</router-link>
      <span class="mx-1">&gt;</span>
      <span class="font-mono">{{ requestId.substring(0, 8) }}...</span>
    </div>

    <div v-if="loading" class="text-gray-500">Loading...</div>
    <div v-else-if="error" class="text-red-600">{{ error }}</div>
    <div v-else-if="detail">
      <h1 class="text-2xl font-bold mb-2">Scoring Detail</h1>
      <div class="text-sm text-gray-500 mb-6">
        <span>Source: {{ detail.source }}</span>
        <span class="mx-2">|</span>
        <span>Query: {{ detail.query }}</span>
        <span class="mx-2">|</span>
        <span>{{ new Date(detail.timestamp).toLocaleString() }}</span>
      </div>

      <div class="grid gap-3">
        <div
          v-for="(item, idx) in detail.itemTraces"
          :key="idx"
          class="bg-white rounded border p-4 text-sm"
          :class="item.matched ? 'border-green-300' : 'border-gray-200'"
        >
          <div class="flex items-baseline gap-3 mb-1">
            <span class="font-semibold" :class="item.matched ? 'text-green-700' : 'text-gray-900'">
              {{ item.candidateTitle }}
            </span>
            <span class="text-xs px-2 py-0.5 rounded" :class="item.matched ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'">
              {{ item.matched ? 'matched' : 'no match' }}
            </span>
            <span class="text-gray-500 text-xs">score {{ item.score.toFixed(2) }}</span>
          </div>

          <div class="text-xs text-gray-500 mb-2">
            {{ item.candidateChannel }} &middot; {{ item.candidateTopic }} &middot;
            {{ Math.floor(item.candidateDuration / 60) }}min &middot;
            {{ item.candidateQuality }}p
            <span v-if="item.matchedRuleId" class="ml-2 text-green-600">rule: {{ item.matchedRuleId }}</span>
          </div>

          <!-- Expandable rule traces -->
          <details class="mt-2">
            <summary class="text-xs text-gray-400 cursor-pointer hover:text-gray-600">
              {{ item.ruleTraces.length }} rule trace(s)
            </summary>
            <div class="mt-2 space-y-2">
              <div
                v-for="(rt, ri) in item.ruleTraces"
                :key="ri"
                class="pl-3 border-l-2 text-xs"
                :class="{
                  'border-green-400': rt.outcome === 'matched',
                  'border-red-300': rt.outcome === 'filterFailed',
                  'border-gray-300': rt.outcome !== 'matched' && rt.outcome !== 'filterFailed'
                }"
              >
                <div class="flex gap-2">
                  <span class="font-mono">{{ rt.ruleId }}</span>
                  <span class="text-gray-500">prio {{ rt.priority }}</span>
                  <span
                    :class="{
                      'text-green-600': rt.outcome === 'matched',
                      'text-red-500': rt.outcome === 'filterFailed',
                      'text-gray-400': rt.outcome !== 'matched' && rt.outcome !== 'filterFailed'
                    }"
                  >{{ rt.outcome }}</span>
                </div>
                <pre v-if="rt.filterTrace" class="text-gray-500 mt-1 whitespace-pre-wrap">{{ JSON.stringify(rt.filterTrace, null, 2) }}</pre>
                <pre v-if="rt.identificationTrace" class="text-gray-500 mt-1 whitespace-pre-wrap">{{ JSON.stringify(rt.identificationTrace, null, 2) }}</pre>
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
