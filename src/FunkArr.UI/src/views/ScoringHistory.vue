<template>
  <div class="max-w-3xl mx-auto">
    <AppBreadcrumb :items="[{ label: $t('rulesets.title'), to: '/rulesets' }, { label: id, to: `/rulesets/${id}` }, { label: $t('scoring.historyTitle') }]" />

    <h1 class="text-xl font-semibold text-text-primary tracking-tight mb-4">{{ $t('scoring.historyTitle') }}</h1>

    <SkeletonTable v-if="loading" :rows="5" :columns="6" />
    <div v-else-if="error" class="text-status-fail text-sm">{{ error }}</div>
    <div v-else-if="history && history.snapshots.length === 0" class="text-text-secondary text-sm">{{ $t('scoring.noHistory') }}</div>

    <div v-else-if="history">
      <div class="text-sm text-text-secondary mb-3 tabular-nums">{{ $t('scoring.totalRuns', { count: history.totalCount }) }}</div>

      <div class="overflow-x-auto rounded-lg border border-border-default">
        <table class="w-full text-sm">
          <thead class="sticky top-0 z-10">
            <tr class="bg-surface-raised">
              <th class="text-left px-4 py-3 font-medium text-text-secondary text-xs">{{ $t('scoring.sourceColumn') }}</th>
              <th class="text-left px-4 py-3 font-medium text-text-secondary text-xs">{{ $t('scoring.queryColumn') }}</th>
              <th class="text-left px-4 py-3 font-medium text-text-secondary text-xs">{{ $t('scoring.whenColumn') }}</th>
              <th class="text-right px-4 py-3 font-medium text-text-secondary text-xs">{{ $t('scoring.candidatesColumn') }}</th>
              <th class="text-right px-4 py-3 font-medium text-text-secondary text-xs">{{ $t('scoring.matchedColumn') }}</th>
            </tr>
          </thead>
          <tbody class="bg-surface-raised/50">
            <tr
              v-for="s in history.snapshots"
              :key="s.requestId"
              class="border-t border-border-subtle hover:bg-surface-elevated/60 cursor-pointer transition-colors"
              @click="$router.push(`/rulesets/${id}/history/${s.requestId}`)"
            >
              <td class="px-4 py-2.5 text-text-body">{{ s.source }}</td>
              <td class="px-4 py-2.5 text-text-body">{{ s.query }}</td>
              <td class="px-4 py-2.5 text-text-secondary">{{ formatTime(s.timestamp) }}</td>
              <td class="px-4 py-2.5 text-right text-text-body tabular-nums">{{ s.candidateCount }}</td>
              <td class="px-4 py-2.5 text-right text-text-body tabular-nums">{{ s.matchedCount }}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="flex gap-3 mt-4">
        <button
          v-if="offset > 0"
          class="px-2.5 py-1 text-sm bg-surface-elevated border border-border-default rounded-md hover:bg-surface-overlay text-text-body transition-colors"
          @click="navigate(offset - pageSize)"
        >
          {{ $t('scoring.previous') }}
        </button>
        <button
          v-if="history.snapshots.length === pageSize && offset + pageSize < history.totalCount"
          class="px-2.5 py-1 text-sm bg-surface-elevated border border-border-default rounded-md hover:bg-surface-overlay text-text-body transition-colors"
          @click="navigate(offset + pageSize)"
        >
          {{ $t('scoring.nextPage') }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { getScoringHistory, type ScoringHistoryResult } from '../api/rulesets'
import SkeletonTable from '../components/SkeletonTable.vue'
import AppBreadcrumb from '../components/AppBreadcrumb.vue'

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
