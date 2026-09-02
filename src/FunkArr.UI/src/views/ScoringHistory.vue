<template>
  <div>
    <div class="mb-4 text-sm text-gray-500">
      <router-link to="/rulesets" class="hover:text-gray-700">RuleSets</router-link>
      <span class="mx-1">&gt;</span>
      <router-link :to="`/rulesets/${id}`" class="hover:text-gray-700">{{ id }}</router-link>
      <span class="mx-1">&gt;</span>
      <span>History</span>
    </div>

    <h1 class="text-2xl font-bold mb-4">Scoring History</h1>

    <div v-if="loading" class="text-gray-500">Loading...</div>
    <div v-else-if="error" class="text-red-600">{{ error }}</div>
    <div v-else-if="history && history.snapshots.length === 0" class="text-gray-500">No scoring history.</div>

    <div v-else-if="history">
      <div class="text-sm text-gray-500 mb-3">{{ history.totalCount }} total scoring runs</div>

      <div class="overflow-x-auto">
        <table class="w-full text-sm bg-white border border-gray-200 rounded">
          <thead class="bg-gray-50">
            <tr>
              <th class="text-left px-3 py-2 font-medium text-gray-600">Request</th>
              <th class="text-left px-3 py-2 font-medium text-gray-600">Source</th>
              <th class="text-left px-3 py-2 font-medium text-gray-600">Query</th>
              <th class="text-left px-3 py-2 font-medium text-gray-600">When</th>
              <th class="text-right px-3 py-2 font-medium text-gray-600">Candidates</th>
              <th class="text-right px-3 py-2 font-medium text-gray-600">Matched</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="s in history.snapshots"
              :key="s.requestId"
              class="border-t border-gray-100 hover:bg-gray-50 cursor-pointer"
              @click="$router.push(`/rulesets/${id}/history/${s.requestId}`)"
            >
              <td class="px-3 py-2 font-mono text-xs">{{ s.requestId.substring(0, 8) }}...</td>
              <td class="px-3 py-2">{{ s.source }}</td>
              <td class="px-3 py-2">{{ s.query }}</td>
              <td class="px-3 py-2 text-gray-500">{{ formatTime(s.timestamp) }}</td>
              <td class="px-3 py-2 text-right">{{ s.candidateCount }}</td>
              <td class="px-3 py-2 text-right">{{ s.matchedCount }}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="flex gap-3 mt-4">
        <button
          v-if="offset > 0"
          class="px-3 py-1 text-sm border border-gray-300 rounded hover:bg-gray-100"
          @click="navigate(offset - pageSize)"
        >
          Previous
        </button>
        <button
          v-if="history.snapshots.length === pageSize && offset + pageSize < history.totalCount"
          class="px-3 py-1 text-sm border border-gray-300 rounded hover:bg-gray-100"
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
