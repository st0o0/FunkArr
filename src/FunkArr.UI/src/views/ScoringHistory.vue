<template>
  <div>
    <div class="mb-4 text-sm text-text-muted">
      <router-link to="/rulesets" class="hover:text-text-secondary">RuleSets</router-link>
      <span class="mx-1">&gt;</span>
      <router-link :to="`/rulesets/${id}`" class="hover:text-text-secondary">{{ id }}</router-link>
      <span class="mx-1">&gt;</span>
      <span class="text-text-secondary">History</span>
    </div>

    <h1 class="text-xl font-bold text-text-primary mb-4">Scoring History</h1>

    <div v-if="loading" class="text-text-muted">Loading...</div>
    <div v-else-if="error" class="text-status-fail">{{ error }}</div>
    <div v-else-if="history && history.snapshots.length === 0" class="text-text-muted">No scoring history.</div>

    <div v-else-if="history">
      <div class="text-sm text-text-muted mb-3">{{ history.totalCount }} total scoring runs</div>

      <div class="overflow-x-auto">
        <table class="w-full text-sm bg-surface-raised border border-border-default rounded-lg">
          <thead>
            <tr class="bg-surface-elevated">
              <th class="text-left px-3 py-2 font-medium text-text-secondary text-xs uppercase tracking-wider">Request</th>
              <th class="text-left px-3 py-2 font-medium text-text-secondary text-xs uppercase tracking-wider">Source</th>
              <th class="text-left px-3 py-2 font-medium text-text-secondary text-xs uppercase tracking-wider">Query</th>
              <th class="text-left px-3 py-2 font-medium text-text-secondary text-xs uppercase tracking-wider">When</th>
              <th class="text-right px-3 py-2 font-medium text-text-secondary text-xs uppercase tracking-wider">Candidates</th>
              <th class="text-right px-3 py-2 font-medium text-text-secondary text-xs uppercase tracking-wider">Matched</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="s in history.snapshots"
              :key="s.requestId"
              class="border-t border-border-subtle hover:bg-surface-elevated cursor-pointer transition-colors"
              @click="$router.push(`/rulesets/${id}/history/${s.requestId}`)"
            >
              <td class="px-3 py-2 font-mono text-xs text-brand-400">{{ s.requestId.substring(0, 8) }}...</td>
              <td class="px-3 py-2 text-text-body">{{ s.source }}</td>
              <td class="px-3 py-2 text-text-body">{{ s.query }}</td>
              <td class="px-3 py-2 text-text-muted">{{ formatTime(s.timestamp) }}</td>
              <td class="px-3 py-2 text-right text-text-body">{{ s.candidateCount }}</td>
              <td class="px-3 py-2 text-right text-text-body">{{ s.matchedCount }}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="flex gap-3 mt-4">
        <button
          v-if="offset > 0"
          class="px-3 py-1 text-sm bg-surface-elevated border border-border-default rounded-md hover:border-brand-500 text-text-body"
          @click="navigate(offset - pageSize)"
        >
          Previous
        </button>
        <button
          v-if="history.snapshots.length === pageSize && offset + pageSize < history.totalCount"
          class="px-3 py-1 text-sm bg-surface-elevated border border-border-default rounded-md hover:border-brand-500 text-text-body"
          @click="navigate(offset + pageSize)"
        >
          Next
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { getScoringHistory, type ScoringHistoryResult } from '../api/rulesets'

const route = useRoute()
const id = route.params.id as string
const pageSize = 20

const history = ref<ScoringHistoryResult | null>(null)
const loading = ref(true)
const error = ref<string | null>(null)
const offset = ref(0)

function formatTime(ts: string): string {
  return new Date(ts).toLocaleString()
}

async function load() {
  loading.value = true
  error.value = null
  try {
    history.value = await getScoringHistory(id, offset.value, pageSize)
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to load history'
  } finally {
    loading.value = false
  }
}

function navigate(newOffset: number) {
  offset.value = Math.max(0, newOffset)
  load()
}

onMounted(load)
</script>
